using System.Reflection;
using System.Text;
using nORM.Attributes;
using nORM.Models;
using nORM.Models.Properties;

namespace nORM.Builders.Queries;

public class SqliteQueryBuilder : QueryBuilder
{
    private StringBuilder currentQuery = new();
    
    public IExecutionProperties GetInsertQuery<T>(T entity)
    {
        currentQuery = new();
        
        var collectionName = typeof(T).GetCustomAttribute<CollectionNameAttribute>();
        
        if (collectionName == null)
        {
            throw new Exception($"Collection name not found for type {typeof(T).Name}");
        }
        
        var tableName = collectionName.CollectionName;
        
        currentQuery.Append($"INSERT INTO {tableName} (");
        
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            var columnName = property.GetCustomAttribute<ColumnAttribute>();
            
            if (columnName == null)
            {
                continue;
            }
            
            if(property.GetCustomAttribute<PrimaryKeyAttribute>() != null)
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
            
            if(property.GetCustomAttribute<PrimaryKeyAttribute>() != null)
            {
                continue;
            }
            
            var value = property.GetValue(entity);
            currentQuery.Append($"'{value}', ");
        }
        
        currentQuery.Remove(currentQuery.Length - 2, 2);
        currentQuery.Append(");");

        return new SqlExecutionProperties(currentQuery.ToString(), tableName);
    }
    public IExecutionProperties GetTableInfoQuery<T>()
    {
        currentQuery = new();
        
        var collectionName = typeof(T).GetCustomAttribute<CollectionNameAttribute>();
        
        if (collectionName == null)
        {
            throw new Exception($"Collection name not found for type {typeof(T).Name}");
        }
        
        currentQuery.Append($"PRAGMA table_info({collectionName.CollectionName});");
        
        return new SqlExecutionProperties(currentQuery.ToString());
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
            
            var columnType = GetSqliteType(property.PropertyType);
            
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

        return new SqlExecutionProperties(currentQuery.ToString(), tableName);
    }

    public static string GetSqliteType(Type type)
    {
        return type switch 
        {
            not null when type == typeof(int) => "INTEGER",
            not null when type == typeof(string) => "TEXT",
            not null when type == typeof(bool) => "BOOLEAN",
            not null when type == typeof(DateTime) => "DATETIME",
            not null when type == typeof(double) => "REAL",
            _ => throw new NotSupportedException($"Type {type?.Name} is not supported")
        };
    }
    
    public static string GetSqliteType(PropertyInfo property)
    {
        return GetSqliteType(property.PropertyType);
    }
}