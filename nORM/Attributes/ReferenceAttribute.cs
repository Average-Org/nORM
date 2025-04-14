namespace nORM.Attributes;

/// <summary>
/// Specifies that a property references another entity through a database column.
/// </summary>
/// <remarks>
/// Use this attribute to define relationships between entities, such as foreign key references.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class ReferenceAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the database column that stores the reference.
    /// </summary>
    public string ColumnName { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceAttribute"/> class.
    /// </summary>
    /// <param name="columnName">The name of the database column that stores the reference.</param>
    public ReferenceAttribute(string columnName)
    {
        ColumnName = columnName;
    }
}