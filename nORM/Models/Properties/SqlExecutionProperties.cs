namespace nORM.Models.Properties;

public class SqlExecutionProperties(string query, string? workingCollectionName = null) : IExecutionProperties
{
    public string Query { get; set; } = query;
    public string CollectionContext { get; } = workingCollectionName ?? string.Empty;
    
    public IExecutionProperties AppendRawText(string rawText)
    {
        Query += rawText;
        return this;
    }
}