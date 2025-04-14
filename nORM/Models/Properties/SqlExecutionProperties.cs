using System.Text;

namespace nORM.Models.Properties;

/// <summary>
/// Implementation of execution properties specific to SQL-based database providers.
/// </summary>
/// <remarks>
/// Provides functionality for building and manipulating SQL queries with associated collection context.
/// </remarks>
public class SqlExecutionProperties(StringBuilder query, string? workingCollectionName = null) : IExecutionProperties
{
    /// <summary>
    /// Gets or sets the SQL query text as a StringBuilder.
    /// </summary>
    public StringBuilder Query { get; set; } = query;
    
    /// <summary>
    /// Gets the name of the collection (table) this query applies to.
    /// </summary>
    public string CollectionContext { get; } = workingCollectionName ?? string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlExecutionProperties"/> class with a string query.
    /// </summary>
    /// <param name="query">The SQL query text.</param>
    /// <param name="collectionContext">Optional name of the collection (table) this query applies to.</param>
    public SqlExecutionProperties(string query, string? collectionContext = null)
        : this(new StringBuilder(query), collectionContext)
    {
    }

    /// <summary>
    /// Appends raw text to the current SQL query.
    /// </summary>
    /// <param name="rawText">The text to append to the query.</param>
    /// <returns>The updated execution properties instance for method chaining.</returns>
    public IExecutionProperties AppendRawText(string rawText)
    {
        Query.Append(rawText);
        return this;
    }

    /// <summary>
    /// Returns the string representation of the SQL query.
    /// </summary>
    /// <returns>The current SQL query as a string.</returns>
    public override string ToString()
    {
        return Query.ToString();
    }
}