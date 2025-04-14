using System.Text;

namespace nORM.Models.Properties;

public interface IExecutionProperties
{
    public string? CollectionContext { get; }
    public StringBuilder Query { get; set; }

    public IExecutionProperties AppendRawText(string text);
}