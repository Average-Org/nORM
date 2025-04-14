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

/// <summary>
/// Implementation of the collection context for SQL-based database providers.
/// </summary>
/// <typeparam name="T">The entity type that this collection manages.</typeparam>
/// <remarks>
/// This class provides CRUD operations for entities in SQL databases,
/// handling SQL-specific functionality like schema management and query execution.
/// </remarks>
public class SqlCollectionContext<T> : ICollectionContext<T> where T : NormEntity
{
    private readonly INormConnection _normConnection;
    private readonly SqlQueryBuilder _queryBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlCollectionContext{T}"/> class.
    /// </summary>
    /// <param name="normConnection">The database connection to use.</param>
    private SqlCollectionContext(INormConnection normConnection)
    {
        _normConnection = normConnection;
        _queryBuilder = QueryBuilder.GetQueryBuilderForProvider(normConnection.DatabaseProviderType)
            as SqlQueryBuilder ?? throw new InvalidOperationException("QueryBuilder is not SqlQueryBuilder");
    }

    /// <summary>
    /// Removes all entities from the collection.
    /// </summary>
    /// <returns>true if the operation was successful; otherwise, false.</returns>
    public bool Truncate()
    {
        var deleteQuery = _queryBuilder.GetTruncateQuery<T>();

        using var reader = _normConnection.ExecuteQuery(deleteQuery);
        return reader.Read();
    }

    /// <summary>
    /// Begins a new transaction for operations on this collection.
    /// </summary>
    /// <returns>A new database transaction.</returns>
    public IDbTransaction BeginTransaction()
    {
        return _normConnection.BeginTransaction();
    }

    /// <summary>
    /// Removes an entity from the collection.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <returns>true if the entity was successfully removed; otherwise, false.</returns>
    public bool Remove(T entity)
    {
        var deleteQuery = _queryBuilder
            .GetDeleteQuery(entity);

        using var reader = _normConnection.ExecuteQuery(deleteQuery);
        return reader.Read();
    }

    /// <summary>
    /// Inserts a new entity into the collection.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <returns>The inserted entity with any database-generated values populated.</returns>
    public T Insert(T entity)
    {
        return Insert(entity, null);
    }

    /// <summary>
    /// Inserts a new entity into the collection using the specified transaction.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <param name="transaction">The transaction in which to perform the insert.</param>
    /// <returns>The inserted entity with any database-generated values populated.</returns>
    public T Insert(T entity, IDbTransaction? transaction)
    {
        var addQuery = _queryBuilder
            .GetInsertQuery(entity);

        // Execute the insert query
        entity.SetId(_normConnection.ExecuteScalar(addQuery, transaction));

        return entity;
    }

    /// <summary>
    /// Inserts multiple entities into the collection as a batch operation.
    /// </summary>
    /// <param name="entities">The collection of entities to insert.</param>
    /// <returns>The collection of inserted entities with any database-generated values populated.</returns>
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

    /// <summary>
    /// Finds a single entity in the collection that matches the specified predicate.
    /// </summary>
    /// <param name="predicate">A lambda expression that defines the conditions to match.</param>
    /// <returns>The matching entity, or null if no match is found.</returns>
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

    /// <summary>
    /// Parses all properties from a data reader into an entity instance.
    /// </summary>
    /// <param name="properties">The properties to parse.</param>
    /// <param name="reader">The data reader containing the values.</param>
    /// <param name="entity">The entity instance to populate.</param>
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

    /// <summary>
    /// Parses a single property value from a data reader into an entity property.
    /// </summary>
    /// <param name="property">The property to set.</param>
    /// <param name="value">The value from the data reader.</param>
    /// <param name="entity">The entity instance to update.</param>
    /// <exception cref="Exception">Thrown if the property value cannot be set.</exception>
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

    /// <summary>
    /// Parses a DateTime value from a database-specific format.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>A DateTime object representing the value.</returns>
    /// <exception cref="NotImplementedException">Thrown if the database provider is not supported.</exception>
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

    /// <summary>
    /// Creates a new collection context for the specified connection.
    /// </summary>
    /// <param name="normConnection">The database connection to use.</param>
    /// <returns>A new collection context instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the query builder is not SQL-compatible.</exception>
    public static ICollectionContext<T> Create(INormConnection normConnection)
    {
        var sqlBuilder = QueryBuilder.GetQueryBuilderForProvider(normConnection.DatabaseProviderType)
            as SqlQueryBuilder ?? throw new InvalidOperationException("QueryBuilder is not SqlQueryBuilder");

        EnsureCollectionSchema(normConnection, sqlBuilder);

        return new SqlCollectionContext<T>(normConnection);
    }

    /// <summary>
    /// Ensures that the database schema exists and matches the entity model.
    /// </summary>
    /// <param name="normConnection">The database connection to use.</param>
    /// <param name="sqlBuilder">The SQL query builder to use.</param>
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

    /// <summary>
    /// Alters a column's data type in the database.
    /// </summary>
    /// <param name="normConnection">The database connection to use.</param>
    /// <param name="collectionName">The name of the collection (table).</param>
    /// <param name="columnName">The name of the column to alter.</param>
    /// <param name="columnType">The new data type for the column.</param>
    /// <param name="builder">The SQL query builder to use.</param>
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

    /// <summary>
    /// Extracts column information from database query results.
    /// </summary>
    /// <param name="tableInfoResults">The query results containing table information.</param>
    /// <param name="properties">The properties of the entity type.</param>
    /// <returns>A list of database columns with their names and types.</returns>
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

    /// <summary>
    /// Adds any missing columns to the database table.
    /// </summary>
    /// <param name="normConnection">The database connection to use.</param>
    /// <param name="properties">The properties of the entity type.</param>
    /// <param name="columnNames">The existing column names in the database.</param>
    /// <param name="addQuery">The execution properties containing the collection context.</param>
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

    /// <summary>
    /// Adds a new column to the database table.
    /// </summary>
    /// <param name="normConnection">The database connection to use.</param>
    /// <param name="collectionName">The name of the collection (table).</param>
    /// <param name="currentColumnName">The name of the column to add.</param>
    /// <param name="columnType">The data type of the column.</param>
    private static void AddColumn(INormConnection normConnection, string? collectionName, string currentColumnName,
        string columnType)
    {
        var alterQuery =
            $"ALTER TABLE {collectionName} ADD COLUMN {currentColumnName} {columnType};";
        normConnection.ExecuteNonQuery(new SqlExecutionProperties(alterQuery));
    }

    /// <summary>
    /// Gets the column names defined in the entity class.
    /// </summary>
    /// <param name="properties">The properties of the entity type.</param>
    /// <returns>An array of column names.</returns>
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

    /// <summary>
    /// Drops a column from the database table.
    /// </summary>
    /// <param name="normConnection">The database connection to use.</param>
    /// <param name="collectionName">The name of the collection (table).</param>
    /// <param name="currentColumnName">The name of the column to drop.</param>
    private static void DropColumn(INormConnection normConnection, string? collectionName, string currentColumnName)
    {
        // Column is no longer in use, drop it
        var alterQuery = $"ALTER TABLE {collectionName} DROP COLUMN {currentColumnName};";
        normConnection.ExecuteNonQuery(new SqlExecutionProperties(alterQuery));
    }
}