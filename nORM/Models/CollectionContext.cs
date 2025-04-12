using System.Reflection;
using nORM.Attributes;
using nORM.Builders.Queries;
using nORM.Connections;
using nORM.Models.Properties;

namespace nORM.Models;

public class CollectionContext<T> : ICollectionContext<T> where T : NormEntity
{
    private readonly INormConnection _normConnection;
    
    private CollectionContext(INormConnection normConnection)
    {
        _normConnection = normConnection;
    }
    
    public T Insert(T entity)
    {
        var sqlBuilder = new SqliteQueryBuilder();
        var addQuery = sqlBuilder
            .GetInsertQuery(entity)
            .AppendRawText("; SELECT last_insert_rowid();");
        
        using var reader = _normConnection.ExecuteQuery(addQuery);

        int lastInsertId = 0;
        if (reader.Read())
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                lastInsertId = reader.GetInt32(i);
                break;
            }
        }    
        
        entity.SetId(lastInsertId);
        
        return entity;
    }

    public static ICollectionContext<T> Create(INormConnection normConnection)
    {
        var sqlBuilder = new SqliteQueryBuilder();
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