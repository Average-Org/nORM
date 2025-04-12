namespace nORM.Attributes;

[AttributeUsage(AttributeTargets.Class |
                       AttributeTargets.Struct)
]
public class CollectionNameAttribute(string collectionName) : Attribute
{
    public string CollectionName = collectionName;
}