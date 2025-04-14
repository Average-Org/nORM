namespace nORM.Models.Data;

/// <summary>
/// Represents metadata for a database column.
/// </summary>
/// <remarks>
/// Used during schema operations to track column names and types.
/// </remarks>
public class DatabaseColumn
{
    /// <summary>
    /// Gets or sets the name of the database column.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the data type of the database column.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseColumn"/> class
    /// with empty name and type.
    /// </summary>
    public DatabaseColumn()
    {
        Name = string.Empty;
        Type = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseColumn"/> class
    /// with the specified name and type.
    /// </summary>
    /// <param name="name">The name of the column.</param>
    /// <param name="type">The data type of the column.</param>
    public DatabaseColumn(string name, string type)
    {
        Name = name;
        Type = type;
    }
}