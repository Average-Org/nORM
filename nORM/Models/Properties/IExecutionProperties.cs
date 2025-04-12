namespace nORM.Models.Properties;

public interface IExecutionProperties
{
    public string? CollectionContext { get; }

    public IExecutionProperties AppendRawText(string text);
}