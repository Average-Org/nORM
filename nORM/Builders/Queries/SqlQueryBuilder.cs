using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using nORM.Attributes;
using nORM.Connections;
using nORM.Models;
using nORM.Models.Properties;

namespace nORM.Builders.Queries;

/// <summary>
/// Base SQL query builder that provides common SQL generation functionality.
/// </summary>
/// <remarks>
/// This class serves as the foundation for all SQL-specific query builders,
/// implementing common SQL syntax while allowing provider-specific customizations.
/// </remarks>
public class SqlQueryBuilder : QueryBuilder
{
    private StringBuilder currentQuery = new();
    
    /// <summary>
    /// Gets the singleton instance of the SQLite query builder.
    /// </summary>
    public static readonly SqliteQueryBuilder Sqlite = new();
    
    /// <summary>
    /// Gets the singleton instance of the MySQL query builder.
    /// </summary>
    public static readonly MySqlQueryBuilder MySql = new();
    
    /// <summary>
    /// Gets the database provider type for this query builder.
    /// </summary>
    public virtual DatabaseProviderType DatabaseProviderType { get; } = DatabaseProviderType.Sqlite;
    
    /// <summary>
    /// Generates a SELECT query based on a predicate expression.
    /// </summary>
    /// <typeparam name="T">The entity type to query.</typeparam>
    /// <param name="predicate">A lambda expression that defines the conditions to match.</param>
    /// <returns>Execution properties containing the SELECT query.</returns>
    /// <exception cref="Exception">Thrown if the collection name cannot be determined.</exception>
    public IExecutionProperties GetSelectQuery<T>(Expression<Func<T, bool>> predicate)
    {
        currentQuery = new();

        var collectionName = typeof(T).GetCustomAttribute<CollectionNameAttribute>();
        if (collectionName == null)
        {
            throw new Exception($"Collection name not found for type {typeof(T).Name}");
        }

        var tableName = collectionName.CollectionName;
        currentQuery.Append($"SELECT * FROM {tableName} WHERE ");

        // Parse the predicate body
        var body = predicate.Body;
        var conditions = new List<string>();

        ParseExpression(body, conditions);

        currentQuery.Append(string.Join(" AND ", conditions));

        return new SqlExecutionProperties(currentQuery, tableName);
    }

    /// <summary>
    /// Generates an ALTER TABLE query to change a column's data type.
    /// </summary>
    /// <param name="collectionName">The name of the collection (table) to alter.</param>
    /// <param name="columnName">The name of the column to alter.</param>
    /// <param name="columnType">The new data type for the column.</param>
    /// <returns>Execution properties containing the ALTER TABLE query.</returns>
    public IExecutionProperties GetAlterTableTypeQuery(string collectionName, string columnName, string columnType)
    {
        currentQuery = new StringBuilder();
        currentQuery.Append($"ALTER TABLE {collectionName} ALTER COLUMN {columnName} TYPE {columnType};");
        return new SqlExecutionProperties(currentQuery, collectionName);
    }
    
    /// <summary>
    /// Parses an expression tree to build SQL conditions.
    /// </summary>
    /// <param name="expr">The expression to parse.</param>
    /// <param name="conditions">List of conditions to append to.</param>
    /// <exception cref="NotSupportedException">Thrown if the expression type is not supported.</exception>
    private void ParseExpression(Expression expr, List<string> conditions)
    {
        if (expr is BinaryExpression binary)
        {
            if (binary.NodeType == ExpressionType.AndAlso)
            {
                ParseExpression(binary.Left, conditions);
                ParseExpression(binary.Right, conditions);
            }
            else if (binary.NodeType == ExpressionType.Equal)
            {
                var member = binary.Left as MemberExpression;
                var constant = GetConstantValue(binary.Right);

                if (member != null)
                {
                    var columnName = member.Member.GetCustomAttribute<ColumnAttribute>()?.Name ?? member.Member.Name;
                    var formatted = FormatSqlValue(constant);
                    conditions.Add($"{columnName} = {formatted}");
                }
            }
        }
        else
        {
            throw new NotSupportedException($"Expression type {expr.NodeType} is not supported.");
        }
    }

    /// <summary>
    /// Extracts the constant value from an expression.
    /// </summary>
    /// <param name="expr">The expression containing a constant value.</param>
    /// <returns>The extracted constant value.</returns>
    private object? GetConstantValue(Expression expr)
    {
        if (expr is ConstantExpression c)
        {
            return c.Value;
        }


        var lambda = Expression.Lambda(expr);
        var compiled = lambda.Compile();
        return compiled.DynamicInvoke();
    }

    /// <summary>
    /// Formats a value for use in SQL statements, handling different data types.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>A string representation of the value suitable for SQL.</returns>
    private string FormatSqlValue(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s => $"'{s.Replace("'", "''")}'",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            bool b => b ? "1" : "0",
            _ => value.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Generates a query to delete all rows from a collection.
    /// </summary>
    /// <typeparam name="T">The entity type for which to truncate the collection.</typeparam>
    /// <returns>Execution properties containing the DELETE query.</returns>
    /// <exception cref="Exception">Thrown if the collection name cannot be determined.</exception>
    public IExecutionProperties GetTruncateQuery<T>()
    {
        currentQuery = new();
        
        var collectionName = typeof(T).GetCustomAttribute<CollectionNameAttribute>();
        
        if (collectionName == null)
        {
            throw new Exception($"Collection name not found for type {typeof(T).Name}");
        }
        
        var tableName = collectionName.CollectionName;
        
        currentQuery.Append($"DELETE FROM {tableName} RETURNING *;");
        
        return new SqlExecutionProperties(currentQuery, tableName);
    }
    
    /// <summary>
    /// Generates a query to delete an entity from a collection.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="entity">The entity instance to delete.</param>
    /// <returns>Execution properties containing the DELETE query.</returns>
    /// <exception cref="Exception">Thrown if the collection name cannot be determined.</exception>
    public virtual IExecutionProperties GetDeleteQuery<T>(T entity)
    {
        currentQuery = new();
        
        var collectionName = typeof(T).GetCustomAttribute<CollectionNameAttribute>();
        
        if (collectionName == null)
        {
            throw new Exception($"Collection name not found for type {typeof(T).Name}");
        }
        
        var tableName = collectionName.CollectionName;
        
        currentQuery.Append($"DELETE FROM {tableName} WHERE ");
        
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            var columnName = property.GetCustomAttribute<ColumnAttribute>();
            
            if (columnName == null)
            {
                continue;
            }
            
            if(property.GetCustomAttribute<PrimaryKeyAttribute>() == null)
            {
                continue;
            }
            
            var value = property.GetValue(entity);
            currentQuery.Append($"{columnName.Name} = '{value}' AND ");
        }
        
        currentQuery.Remove(currentQuery.Length - 5, 5);
        currentQuery.Append(';');
        
        return new SqlExecutionProperties(currentQuery, tableName);
    }
    
    /// <summary>
    /// Generates a query to insert an entity into a collection.
    /// </summary>
    /// <typeparam name="T">The entity type to insert.</typeparam>
    /// <param name="entity">The entity instance to insert.</param>
    /// <returns>Execution properties containing the INSERT query.</returns>
    public virtual IExecutionProperties GetInsertQuery<T>(T entity)
    {
        currentQuery = new StringBuilder();
        var type = typeof(T);
        
        var tableName = NormEntity.GetCollectionName(type);
        
        currentQuery.Append($"INSERT INTO {tableName} (");

        var properties = NormEntity.GetPropertiesSpan(type);
        var primaryKey = NormEntity.GetPrimaryKey(type);
        
        foreach (var property in properties)
        {
            var columnName = property.GetCustomAttribute<ColumnAttribute>();
            
            if (columnName == null)
            {
                continue;
            }
            
            if(primaryKey != null && property.Name == primaryKey.Name)
            {
                continue;
            }
            
            currentQuery.Append($"{columnName.Name}, ");
        }
        
        currentQuery.Remove(currentQuery.Length - 2, 2);
        currentQuery.Append(") VALUES (");
        
        foreach (var property in properties)
        {
            var columnName = property.GetCustomAttribute<ColumnAttribute>();
            
            if (columnName == null)
            {
                continue;
            }
            
            if(primaryKey != null && property.Name == primaryKey.Name)
            {
                continue;
            }
            
            var value = property.GetValue(entity);
            if(value is DateTime dateTime)
            {
                value = HandleDateTimeValue(dateTime, value);
            }
            currentQuery.Append($"'{value}', ");
        }
        
        currentQuery.Remove(currentQuery.Length - 2, 2);
        currentQuery.Append(");");
        
        return new SqlExecutionProperties(currentQuery, tableName);
    }

    /// <summary>
    /// Handles formatting of DateTime values based on the database provider.
    /// </summary>
    /// <param name="dateTime">The DateTime value to format.</param>
    /// <param name="value">The original value.</param>
    /// <returns>The formatted DateTime value suitable for the current database provider.</returns>
    private object HandleDateTimeValue(DateTime dateTime, object value)
    {
        switch (DatabaseProviderType)
        {
            case DatabaseProviderType.Sqlite:
            {
                dateTime = dateTime.ToLocalTime();
                value = dateTime.ToUniversalTime().ToString("o");
                break;
            }
            case DatabaseProviderType.MySql:
            {
                value = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                value = $"{value}";
                break;
            }
        }

        return value;
    }

    /// <summary>
    /// Generates a query to get table information (schema) for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type for which to get table information.</typeparam>
    /// <returns>Execution properties containing a partial query setup for table information.</returns>
    /// <exception cref="Exception">Thrown if the collection name cannot be determined.</exception>
    public virtual IExecutionProperties GetTableInfoQuery<T>()
    {
        currentQuery = new();
        
        var collectionName = typeof(T).GetCustomAttribute<CollectionNameAttribute>();
        
        if (collectionName == null)
        {
            throw new Exception($"Collection name not found for type {typeof(T).Name}");
        }
        
        return new SqlExecutionProperties(currentQuery.ToString(), collectionName.CollectionName);
    }
    
    /// <summary>
    /// Generates a query to create a collection (table) for the specified entity type if it doesn't exist.
    /// </summary>
    /// <typeparam name="T">The entity type for which to create a collection.</typeparam>
    /// <returns>Execution properties containing the CREATE TABLE query.</returns>
    /// <exception cref="Exception">Thrown if the collection name cannot be determined.</exception>
    public override IExecutionProperties GetCreateCollectionQuery<T>()
    {
        currentQuery = new();

        var collectionName = typeof(T).GetCustomAttribute<CollectionNameAttribute>();
        
        if (collectionName == null)
        {
            throw new Exception($"Collection name not found for type {typeof(T).Name}");
        }

        var tableName = collectionName.CollectionName;
        
        currentQuery.Append($"CREATE TABLE IF NOT EXISTS {tableName} (");
        
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            var columnName = property.GetCustomAttribute<ColumnAttribute>();
            
            if (columnName == null)
            {
                continue;
            }
            
            var columnType = GetSqlType(property.PropertyType, DatabaseProviderType);
            
            currentQuery.Append($"{columnName.Name} {columnType} ");
            
            var primaryKey = property.GetCustomAttribute<PrimaryKeyAttribute>();
            
            if (primaryKey == null)
            {
                currentQuery.Append(", ");
                continue;
            }
            
            currentQuery.Append("PRIMARY KEY ");
            
            if(primaryKey.AutoIncrement)
            {
                currentQuery.Append("AUTOINCREMENT ");
            }
            
            currentQuery.Append(", ");
        }
        
        currentQuery.Remove(currentQuery.Length - 2, 2);
        currentQuery.Append(");");

        return new SqlExecutionProperties(currentQuery, tableName);
    }

    /// <summary>
    /// Gets the SQL data type for a .NET type based on the database provider.
    /// </summary>
    /// <param name="type">The .NET type to convert.</param>
    /// <param name="providerType">The database provider type.</param>
    /// <returns>The SQL data type as a string.</returns>
    /// <exception cref="NotSupportedException">Thrown if the provider is not supported.</exception>
    public static string GetSqlType(Type type, DatabaseProviderType providerType)
    {
        return providerType switch
        {
            DatabaseProviderType.Sqlite => GetSqliteType(type),
            DatabaseProviderType.MySql => GetMySqlType(type),
            _ => throw new NotSupportedException($"Database provider {providerType} is not supported")
        };
    }
    
    /// <summary>
    /// Gets the MySQL data type for a .NET type.
    /// </summary>
    /// <param name="type">The .NET type to convert.</param>
    /// <returns>The MySQL data type as a string.</returns>
    /// <exception cref="NotSupportedException">Thrown if the type is not supported.</exception>
    public static string GetMySqlType(Type type)
    {
        return type switch 
        {
            not null when type == typeof(int) => "INT",
            not null when type == typeof(string) => "VARCHAR(255)",
            not null when type == typeof(bool) => "TINYINT(1)",
            not null when type == typeof(DateTime) => "DATETIME",
            not null when type == typeof(double) => "DOUBLE",
            _ => throw new NotSupportedException($"Type {type?.Name} is not supported")
        };
    }
    
    /// <summary>
    /// Gets the SQLite data type for a .NET type.
    /// </summary>
    /// <param name="type">The .NET type to convert.</param>
    /// <returns>The SQLite data type as a string.</returns>
    /// <exception cref="NotSupportedException">Thrown if the type is not supported.</exception>
    public static string GetSqliteType(Type type)
    {
        return type switch 
        {
            not null when type == typeof(int) => "INTEGER",
            not null when type == typeof(string) => "TEXT",
            not null when type == typeof(bool) => "BOOLEAN",
            not null when type == typeof(DateTime) => "TEXT",
            not null when type == typeof(double) => "REAL",
            _ => throw new NotSupportedException($"Type {type?.Name} is not supported")
        };
    }
}