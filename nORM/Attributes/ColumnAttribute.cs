namespace nORM.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ColumnAttribute(string name) : Attribute
{
    public string Name = name;
}