using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using nORM.Attributes;
using nORM.Builders.Queries;
using nORM.Connections;
using nORM.Models.Data;
using nORM.Models.Properties;

namespace nORM.Models.Context;

public class SqlCollectionContext<T> : ICollectionContext<T> where T : NormEntity
{
    private readonly INormConnection _normConnection;
    private readonly SqlQueryBuilder _queryBuilder;

    private SqlCollectionContext(INormConnection normConnection)
    {
        _normConnection = normConnection;
        _queryBuilder = QueryBuilder.GetQueryBuilderForProvider(normConnection.DatabaseProviderType)
            as SqlQueryBuilder ?? throw new InvalidOperationException("QueryBuilder is not SqlQueryBuilder");
    }

    public bool Truncate()
    {
        var deleteQuery = _queryBuilder.GetTruncateQuery<T>();

        using var reader = _normConnection.ExecuteQuery(deleteQuery);
        return reader.Read();
    }

    public IDbTransaction BeginTransaction()
    {
        return _normConnection.BeginTransaction();
    }

    public bool Remove(T entity)
    {
        var deleteQuery = _queryBuilder
            .GetDeleteQuery(entity);

        using var reader = _normConnection.ExecuteQuery(deleteQuery);
        return reader.Read();
    }

    public T Insert(T entity)
    {
        return Insert(entity, null);
    }

    public T Insert(T entity, IDbTransaction? transaction)
    {
        var addQuery = _queryBuilder
            .GetInsertQuery(entity);

        // Execute the insert query
        entity.SetId(_normConnection.ExecuteScalar(addQuery, transaction));

        return entity;
    }

    public IEnumerable<T> InsertMany(IEnumerable<T> entities)
    {
        var insertedEntities = new List<T>();

        var transaction = _normConnection.BeginTransaction();
        foreach (var entity in entities)
        {
            Insert(entity);
            insertedEntities.Add(entity);
        }

        transaction.Commit();

        return insertedEntities;
    }

    public T? FindOne(Expression<Func<T, bool>> predicate)
    {
        var selectQuery = _queryBuilder
            .GetSelectQuery(predicate);

        using var reader = _normConnection.ExecuteQuery(selectQuery);

        if (!reader.Read())
        {
            return null;
        }

        var entity = Activator.CreateInstance<T>();
        var properties = NormEntity.GetPropertiesSpan(typeof(T));

        ParseAllProperties(properties, reader, entity);
        return entity;
    }

    private void ParseAllProperties(Span<PropertyInfo> properties, IDataReader reader, T entity)
    {
        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<ColumnAttribute>() is not { } columnName)
            {
                continue;
            }

            if (reader[columnName.Name] is not { } value
                || value is DBNull)
            {
                continue;
            }

            ParseObjectFromReader(property, value, entity);
        }
    }

    private void ParseObjectFromReader(PropertyInfo property, object value, T entity)
    {
        try
        {
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var convertedValue = targetType == typeof(DateTime)
                ? ParseDateTime(value)
                : Convert.ChangeType(value, targetType);

            property.SetValue(entity, convertedValue);
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Error setting property '{property.Name}' with value '{value}' (type {value.GetType().Name})",
                ex
            );
        }
    }

    private object ParseDateTime(object value)
    {
        object? convertedValue;

        // try parsing manually using system culture fallback
        var raw = value.ToString()!;

        switch (_normConnection.DatabaseProviderType)
        {
            case DatabaseProviderType.Sqlite:
            {
                var parsed = DateTime.Parse(raw, null, DateTimeStyles.RoundtripKind);
                convertedValue = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                break;
            }
            case DatabaseProviderType.MySql:
            {
                var parsed = DateTime.Parse(raw, null);
                convertedValue = new DateTime(parsed.Year, parsed.Month, parsed.Day,
                    parsed.Hour, parsed.Minute, parsed.Second);
                break;
            }
            default:
            {
                throw new NotImplementedException("Database provider not supported for date parsing");
            }
        }

        return convertedValue;
    }

    public static ICollectionContext<T> Create(INormConnection normConnection)
    {
        var sqlBuilder = QueryBuilder.GetQueryBuilderForProvider(normConnection.DatabaseProviderType)
            as SqlQueryBuilder ?? throw new InvalidOperationException("QueryBuilder is not SqlQueryBuilder");

        EnsureCollectionSchema(normConnection, sqlBuilder);

        return new SqlCollectionContext<T>(normConnection);
    }

    private static void EnsureCollectionSchema(INormConnection normConnection, SqlQueryBuilder sqlBuilder)
    {
        // Add the collection table if it doesn't exist
        var addQuery = sqlBuilder
            .GetCreateCollectionQuery<T>();
        normConnection.ExecuteNonQuery(addQuery);

        // Get the table info to check for missing columns
        var tableInfoQuery = sqlBuilder
            .GetTableInfoQuery<T>();
        var tableInfoResults = normConnection.Query(tableInfoQuery);

        var properties = NormEntity.GetPropertiesSpan(typeof(T)).ToArray();
        List<DatabaseColumn> columns = ExtractColumnNames(tableInfoResults, properties.ToArray());
        var classPropertyColumns = GetClassPropertyColumns(properties);

        foreach (var column in columns)
        {
            // Get the property that matches the column name
            var property = properties.FirstOrDefault(x =>
                x.GetCustomAttribute<ColumnAttribute>()?.Name == column.Name);

            if (property == null)
            {
                DropColumn(normConnection, addQuery.CollectionContext, column.Name);
                continue;
            }

            var columnType = SqlQueryBuilder.GetSqlType(property.PropertyType,
                normConnection.DatabaseProviderType);

            // If the column type matches the expected type, skip
            if (column.Type.Equals(columnType, StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            // Column type does not match, alter the column
            // SQLite does not support altering column types directly, so we need to create a new table

            // DELETE the column first, then create a new one
            // with the correct type
            if (normConnection.DatabaseProviderType == DatabaseProviderType.Sqlite)
            {
                DropColumn(normConnection, addQuery.CollectionContext, column.Name);
                AddColumn(normConnection, addQuery.CollectionContext, column.Name, columnType);
            }
            else
            {
                AlterColumn(normConnection, addQuery.CollectionContext, column.Name, columnType, sqlBuilder);
            }
        }

        AddAnyMissingColumns(normConnection, properties, columns.Select(x => x.Name).ToList(), addQuery);
    }

    private static void AlterColumn(INormConnection normConnection,
        string? collectionName, string columnName, string columnType, SqlQueryBuilder builder)
    {
        if (collectionName == null)
        {
            return;
        }

        var alterQuery = builder
            .GetAlterTableTypeQuery(collectionName, columnName, columnType);

        normConnection.ExecuteNonQuery(alterQuery);
    }

    private static List<DatabaseColumn> ExtractColumnNames(IEnumerable<IDictionary<string, object>> tableInfoResults,
        PropertyInfo[] properties)
    {
        var columns = new List<DatabaseColumn>();
        DatabaseColumn currentColumn = new DatabaseColumn();

        foreach (var row in tableInfoResults)
        {
            if (row.TryGetValue("name", out var columnNameObj) ||
                row.TryGetValue("COLUMN_NAME", out columnNameObj))
            {
                if (columnNameObj is not string columnNameString)
                {
                    continue;
                }

                currentColumn.Name = columnNameString;
            }

            if (!row.TryGetValue("type", out var columnTypeObj)
                && !row.TryGetValue("COLUMN_TYPE", out columnTypeObj))
            {
                continue;
            }

            if (columnTypeObj is not string columnTypeValue)
            {
                continue;
            }

            currentColumn.Type = columnTypeValue;

            columns.Add(currentColumn);
            currentColumn = new DatabaseColumn();
        }

        return columns;
    }

    private static void AddAnyMissingColumns(INormConnection normConnection, Span<PropertyInfo> properties,
        List<string> columnNames,
        IExecutionProperties addQuery)
    {
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
            var columnType = SqlQueryBuilder.GetSqlType(property.PropertyType,
                normConnection.DatabaseProviderType);

            var addColumnQuery =
                $"ALTER TABLE {addQuery.CollectionContext} ADD COLUMN {columnName.Name} {columnType};";
            normConnection.ExecuteNonQuery(new SqlExecutionProperties(addColumnQuery));
        }
    }

    private static void AddColumn(INormConnection normConnection, string? collectionName, string currentColumnName,
        string columnType)
    {
        var alterQuery =
            $"ALTER TABLE {collectionName} ADD COLUMN {currentColumnName} {columnType};";
        normConnection.ExecuteNonQuery(new SqlExecutionProperties(alterQuery));
    }

    private static string[] GetClassPropertyColumns(Span<PropertyInfo> properties)
    {
        var columnNames = new List<string>();

        foreach (var property in properties)
        {
            var attr = property.GetCustomAttribute<ColumnAttribute>();
            if (!string.IsNullOrWhiteSpace(attr?.Name))
            {
                columnNames.Add(attr.Name);
            }
        }

        return columnNames.ToArray();
    }

    private static void DropColumn(INormConnection normConnection, string? collectionName, string currentColumnName)
    {
        // Column is no longer in use, drop it
        var alterQuery = $"ALTER TABLE {collectionName} DROP COLUMN {currentColumnName};";
        normConnection.ExecuteNonQuery(new SqlExecutionProperties(alterQuery));
    }
}