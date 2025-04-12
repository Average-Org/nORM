using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using nORM.Attributes;
using nORM.Builders.Queries;
using nORM.Connections;
using nORM.Models.Properties;

namespace nORM.Models.Context;

public class CollectionContext<T> : ICollectionContext<T> where T : NormEntity
{
    private readonly INormConnection _normConnection;
    
    private CollectionContext(INormConnection normConnection)
    {
        _normConnection = normConnection;
    }

    public bool Truncate()
    {
        var deleteQuery = SqliteQueryBuilder.Instance
            .GetTruncateQuery<T>();
        
        using var reader = _normConnection.ExecuteQuery(deleteQuery);
        return reader.Read();
    }

    public IDbTransaction BeginTransaction()
    {
        return _normConnection.BeginTransaction();
    }

    public bool Remove(T entity)
    {
        var deleteQuery = SqliteQueryBuilder.Instance
            .GetDeleteQuery(entity);
        
        using var reader = _normConnection.ExecuteQuery(deleteQuery);
        return reader.Read();
    }
    
    public T Insert(T entity)
    {
        var addQuery = SqliteQueryBuilder.Instance
            .GetInsertQuery(entity);
        
        entity.SetId(_normConnection.ExecuteScalar(addQuery));
        
        return entity;
    }
    
    public T? FindOne(Expression<Func<T, bool>> predicate)
    {
        var selectQuery = SqliteQueryBuilder.Instance
            .GetSelectQuery(predicate);

        using var reader = _normConnection.ExecuteQuery(selectQuery);

        if (!reader.Read())
        {
            return default;
        }

        var entity = Activator.CreateInstance<T>();

        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var columnName = property.GetCustomAttribute<ColumnAttribute>();
            if (columnName == null)
                continue;

            var value = reader[columnName.Name];
            if (value is DBNull)
                continue;

            try
            {
                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                object? convertedValue;

                if (targetType == typeof(DateTime))
                {
                    // try parsing manually using system culture fallback
                    var raw = value.ToString()!;
                    Console.WriteLine(raw);
                    var parsed = DateTime.Parse(raw, null, DateTimeStyles.RoundtripKind);
                    convertedValue = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                }
                else
                {
                    convertedValue = Convert.ChangeType(value, targetType);
                }

                property.SetValue(entity, convertedValue);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error setting property '{property.Name}' with value '{value}' (type {value?.GetType()?.Name})",
                    ex
                );
            }
        }


        return entity;
    }


    public static ICollectionContext<T> Create(INormConnection normConnection)
    {
        var sqlBuilder = SqliteQueryBuilder.Instance;
        
        var addQuery = sqlBuilder
            .GetCreateCollectionQuery<T>();

        normConnection.ExecuteNonQuery(addQuery);

        var tableInfoQuery = sqlBuilder
            .GetTableInfoQuery<T>();

        var tableInfoResults = normConnection.Query(tableInfoQuery);

        List<string> columnNames = [];
        string[] classPropertyColumns = [];
        var currentColumnName = string.Empty;

        foreach (var row in tableInfoResults)
        {
            if (row.TryGetValue("name", out var columnNameObj))
            {
                if (columnNameObj is not string columnNameString)
                {
                    continue;
                }

                currentColumnName = columnNameString;
                
                classPropertyColumns =
                    typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Select(x => x.GetCustomAttribute<ColumnAttribute>())
                        .Where(x => x != null || !string.IsNullOrWhiteSpace(x?.Name))
                        .Select(x => x.Name)
                        .ToArray();

                if (classPropertyColumns.Length != 0)
                {
                    columnNames.Add(currentColumnName);
                }
                else
                {
                    // Column is no longer in use, drop it
                    var alterQuery = $"ALTER TABLE {addQuery.CollectionContext} DROP COLUMN {currentColumnName};";
                    normConnection.ExecuteNonQuery(new SqlExecutionProperties(alterQuery));
                }
            }

            if (row.TryGetValue("type", out var columnTypeObj))
            {
                if (columnTypeObj is not string columnTypeValue)
                {
                    continue;
                }

                var columnName = classPropertyColumns.FirstOrDefault(x => x == currentColumnName);

                var property = typeof(T).GetProperties().FirstOrDefault(x =>
                    x.GetCustomAttribute<ColumnAttribute>()?.Name == currentColumnName);

                if (property == null)
                {
                    continue;
                }

                var columnType = SqliteQueryBuilder.GetSqliteType(property.PropertyType);

                // Check if the column type matches the expected type
                if (columnType.Equals(columnTypeValue, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                // Column type does not match, alter the column
                // SQLite does not support altering column types directly, so we need to create a new table
                
                // DELETE the column first, then create a new one
                // with the correct type
                var deleteColumnQuery =
                    $"ALTER TABLE {addQuery.CollectionContext} DROP COLUMN {currentColumnName};";
                normConnection.ExecuteNonQuery(new SqlExecutionProperties(deleteColumnQuery));
                
                var alterQuery =
                    $"ALTER TABLE {addQuery.CollectionContext} ADD COLUMN {currentColumnName} {columnType};";
                normConnection.ExecuteNonQuery(new SqlExecutionProperties(alterQuery));
            }
        }

        // Check for missing columns
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var columnName = property.GetCustomAttribute<ColumnAttribute>();

            if (columnName == null)
            {
                continue;
            }

            if (columnNames.Contains(columnName.Name))
            {
                continue;
            }

            // Column is missing, add it
            var columnType = SqliteQueryBuilder.GetSqliteType(property.PropertyType);

            var addColumnQuery =
                $"ALTER TABLE {addQuery.CollectionContext} ADD COLUMN {columnName.Name} {columnType};";
            normConnection.ExecuteNonQuery(new SqlExecutionProperties(addColumnQuery));
        }

        return new CollectionContext<T>(normConnection);
    }
}