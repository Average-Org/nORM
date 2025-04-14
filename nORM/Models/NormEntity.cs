using System.Collections.Concurrent;
using System.Reflection;
using nORM.Attributes;

namespace nORM.Models;

public abstract class NormEntity
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Properties = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> PrimaryKeys = new();
    private static readonly ConcurrentDictionary<Type, string> TableNames = new();
    private Type Type { get; }

    protected NormEntity()
    {
        Type = GetType();
    }
    
    public string GetCollectionName()
    {
        return TableNames.GetOrAdd(Type, t =>
            t.GetCustomAttribute<CollectionNameAttribute>()?.CollectionName ?? t.Name);
    }

    public static Span<PropertyInfo> GetPropertiesSpan(Type type)
    {
        return Properties.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    }
    
    public static IEnumerable<PropertyInfo> GetProperties(Type type)
    {
        return Properties.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    }

    private PropertyInfo? GetPrimaryKey()
    {
        return PrimaryKeys.GetOrAdd(Type, t =>
        {
            foreach(var property in GetPropertiesSpan(t))
            {
                if (property.GetCustomAttributes(typeof(PrimaryKeyAttribute), true).Length != 0)
                {
                    return property;
                }
            }
            
            return null;
        });
    }
    
    public static PropertyInfo? GetPrimaryKey(Type type)
    {
        return PrimaryKeys.GetOrAdd(type, t =>
        {
            foreach (var property in GetPropertiesSpan(t))
            {
                if (property.GetCustomAttributes(typeof(PrimaryKeyAttribute), true).Length != 0)
                {
                    return property;
                }
            }

            return null;
        });
    }
    
    public static string GetCollectionName(Type type)
    {
        return TableNames.GetOrAdd(type, t =>
            t.GetCustomAttribute<CollectionNameAttribute>()?.CollectionName ?? t.Name);
    }


    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != Type)
            return false;

        var other = (NormEntity)obj;
        var props = GetPropertiesSpan(Type);

        foreach (var prop in props)
        {
            var thisValue = prop.GetValue(this);
            var otherValue = prop.GetValue(other);

            // Normalize DateTime comparison as string
            if (thisValue is DateTime thisDateTime && otherValue is DateTime otherDateTime)
            {
                var thisString = thisDateTime.ToString("o"); // ISO 8601
                var otherString = otherDateTime.ToString("o");
                if (thisString != otherString)
                    return false;
            }
            else if (!Equals(thisValue, otherValue))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (var prop in GetPropertiesSpan(Type))
            {
                var value = prop.GetValue(this);

                if (value is DateTime dt)
                    hash = hash * 23 + dt.ToString("o").GetHashCode(); // Use string hash of ISO format
                else if (value != null)
                    hash = hash * 23 + value.GetHashCode();
            }

            return hash;
        }
    }


    internal virtual void SetId(object id)
    {
        var primaryKey = GetPrimaryKey();
        if (primaryKey == null)
        {
            return;
        }

        var targetType = primaryKey.PropertyType;
        var convertedId = Convert.ChangeType(id, targetType);
        primaryKey.SetValue(this, convertedId);
    }
}