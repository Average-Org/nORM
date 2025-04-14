namespace nORM.Attributes;

/// <summary>
/// Specifies the database column name for a property or field.
/// </summary>
/// <remarks>
/// When a property is marked with this attribute, the ORM will use the specified name
/// for the corresponding column in SQL statements rather than the property name.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ColumnAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets or sets the name of the database column.
    /// </summary>
    public string Name = name;
}