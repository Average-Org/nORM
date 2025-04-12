namespace nORM.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PrimaryKeyAttribute : Attribute
{
    public bool AutoIncrement = true;
}