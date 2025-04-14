using System.Text;

namespace nORM.Models.Properties;

public class SqlExecutionProperties(StringBuilder query, string? workingCollectionName = null) : IExecutionProperties
{
    public StringBuilder Query { get; set; } = query;
    public string CollectionContext { get; } = workingCollectionName ?? string.Empty;

    public SqlExecutionProperties(string query, string? collectionContext = null)
        : this(new StringBuilder(query), collectionContext)
    {
    }

    public IExecutionProperties AppendRawText(string rawText)
    {
        Query.Append(rawText);
        return this;
    }

    public override string ToString()
    {
        return Query.ToString();
    }
}