using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using nORM.Attributes;
using nORM.Connections;
using nORM.Models;
using nORM.Models.Properties;

namespace nORM.Builders.Queries;

public class SqlQueryBuilder : QueryBuilder
{
    private StringBuilder currentQuery = new();
    public static readonly SqliteQueryBuilder Sqlite = new();
    public static readonly MySqlQueryBuilder MySql = new();
    public virtual DatabaseProviderType DatabaseProviderType { get; } = DatabaseProviderType.Sqlite;
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

    public IExecutionProperties GetAlterTableTypeQuery(string collectionName, string columnName, string columnType)
    {
        
    }
    
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
    
    public virtual IExecutionProperties GetInsertQuery<T>(T entity)
    {
        currentQuery = new();
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
            if(value is DateTime dt)
            {
                if (DatabaseProviderType == DatabaseProviderType.Sqlite)
                {
                    dt = dt.ToLocalTime();
                    value = dt.ToUniversalTime().ToString("o");
                }
                else if(DatabaseProviderType == DatabaseProviderType.MySql)
                {
                    value = dt.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            currentQuery.Append($"'{value}', ");
        }
        
        currentQuery.Remove(currentQuery.Length - 2, 2);
        currentQuery.Append(");");
        
        return new SqlExecutionProperties(currentQuery, tableName);
    }
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

    public static string GetSqlType(Type type, DatabaseProviderType providerType)
    {
        return providerType switch
        {
            DatabaseProviderType.Sqlite => GetSqliteType(type),
            DatabaseProviderType.MySql => GetMySqlType(type),
            _ => throw new NotSupportedException($"Database provider {providerType} is not supported")
        };
    }
    
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