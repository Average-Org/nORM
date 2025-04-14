using System.Collections.Concurrent;
using System.Reflection;
using nORM.Attributes;

namespace nORM.Models;

/// <summary>
/// Base class for all ORM entity models. Provides core functionality for mapping
/// between database records and .NET objects.
/// </summary>
public abstract class NormEntity
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Properties = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> PrimaryKeys = new();
    private static readonly ConcurrentDictionary<Type, string> TableNames = new();
    private Type Type { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NormEntity"/> class.
    /// </summary>
    protected NormEntity()
    {
        Type = GetType();
    }
    
    /// <summary>
    /// Gets the collection (table) name for the current entity.
    /// </summary>
    /// <returns>The collection name specified by <see cref="CollectionNameAttribute"/> or the class name if not specified.</returns>
    public string GetCollectionName()
    {
        return TableNames.GetOrAdd(Type, t =>
            t.GetCustomAttribute<CollectionNameAttribute>()?.CollectionName ?? t.Name);
    }

    /// <summary>
    /// Gets all public instance properties for the specified type as a span for efficient enumeration.
    /// </summary>
    /// <param name="type">The entity type to get properties for.</param>
    /// <returns>A span containing all public instance properties of the type.</returns>
    public static Span<PropertyInfo> GetPropertiesSpan(Type type)
    {
        return Properties.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    }
    
    /// <summary>
    /// Gets all public instance properties for the specified type.
    /// </summary>
    /// <param name="type">The entity type to get properties for.</param>
    /// <returns>An enumerable collection of properties for the type.</returns>
    public static IEnumerable<PropertyInfo> GetProperties(Type type)
    {
        return Properties.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    }

    /// <summary>
    /// Gets the primary key property for the current entity.
    /// </summary>
    /// <returns>The property marked with <see cref="PrimaryKeyAttribute"/>, or null if not found.</returns>
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
    
    /// <summary>
    /// Gets the primary key property for the specified entity type.
    /// </summary>
    /// <param name="type">The entity type to get the primary key for.</param>
    /// <returns>The property marked with <see cref="PrimaryKeyAttribute"/>, or null if not found.</returns>
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
    
    /// <summary>
    /// Gets the collection (table) name for the specified entity type.
    /// </summary>
    /// <param name="type">The entity type to get the collection name for.</param>
    /// <returns>The collection name specified by <see cref="CollectionNameAttribute"/> or the class name if not specified.</returns>
    public static string GetCollectionName(Type type)
    {
        return TableNames.GetOrAdd(type, t =>
            t.GetCustomAttribute<CollectionNameAttribute>()?.CollectionName ?? t.Name);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// Compares all properties, with special handling for DateTime values.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
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

            if (thisValue is DateTime thisDateTime && otherValue is DateTime otherDateTime)
            {
                // Truncate to seconds
                thisDateTime = new DateTime(
                    thisDateTime.Year,
                    thisDateTime.Month,
                    thisDateTime.Day,
                    thisDateTime.Hour,
                    thisDateTime.Minute,
                    thisDateTime.Second
                );

                otherDateTime = new DateTime(
                    otherDateTime.Year,
                    otherDateTime.Month,
                    otherDateTime.Day,
                    otherDateTime.Hour,
                    otherDateTime.Minute,
                    otherDateTime.Second
                );

                if (thisDateTime != otherDateTime)
                    return false;
            }
            else if (!Equals(thisValue, otherValue))
            {
                return false;
            }
        }

        return true;
    }
    
    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
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

    /// <summary>
    /// Sets the primary key value for this entity.
    /// </summary>
    /// <param name="id">The ID value to set.</param>
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