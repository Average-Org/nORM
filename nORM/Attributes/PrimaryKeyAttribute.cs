namespace nORM.Attributes;

/// <summary>
/// Marks a property as the primary key for an entity.
/// </summary>
/// <remarks>
/// When a property is marked with this attribute, the ORM will treat it as the 
/// primary key for the corresponding database table, using it for identity operations
/// and lookup queries.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PrimaryKeyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether the primary key should be auto-incremented by the database.
    /// The default value is true.
    /// </summary>
    public bool AutoIncrement = true;
}