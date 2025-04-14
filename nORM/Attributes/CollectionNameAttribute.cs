namespace nORM.Attributes;

/// <summary>
/// Specifies the name of the database collection (table) that corresponds to an entity class.
/// </summary>
/// <remarks>
/// When an entity class is marked with this attribute, the ORM will use the specified name
/// when generating SQL statements instead of the class name.
/// </remarks>
[AttributeUsage(AttributeTargets.Class |
                       AttributeTargets.Struct)
]
public class CollectionNameAttribute(string collectionName) : Attribute
{
    /// <summary>
    /// Gets or sets the name of the database collection.
    /// </summary>
    public string CollectionName = collectionName;
}