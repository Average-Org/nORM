using System.Text;

namespace nORM.Models.Properties;

/// <summary>
/// Defines properties for database command execution.
/// </summary>
/// <remarks>
/// This interface provides abstractions for database commands across different providers,
/// storing the query text and collection context information.
/// </remarks>
public interface IExecutionProperties
{
    /// <summary>
    /// Gets the name of the collection context (table) this execution applies to.
    /// </summary>
    public string? CollectionContext { get; }
    
    /// <summary>
    /// Gets or sets the query text as a StringBuilder for efficient manipulation.
    /// </summary>
    public StringBuilder Query { get; set; }

    /// <summary>
    /// Appends raw text to the current query.
    /// </summary>
    /// <param name="text">The text to append to the query.</param>
    /// <returns>The updated execution properties instance for method chaining.</returns>
    public IExecutionProperties AppendRawText(string text);
}