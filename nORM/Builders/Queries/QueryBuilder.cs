using nORM.Connections;
using nORM.Models;
using nORM.Models.Properties;

namespace nORM.Builders.Queries;

public abstract class QueryBuilder : IQueryBuilder
{
    public abstract IExecutionProperties GetCreateCollectionQuery<T>() where T : NormEntity;

    public static IQueryBuilder GetQueryBuilderForProvider(DatabaseProviderType type)
    {
        return type switch
        {
            DatabaseProviderType.Sqlite => SqlQueryBuilder.Sqlite,
            DatabaseProviderType.MySql => SqlQueryBuilder.MySql,
            _ => throw new NotImplementedException($"No query builder implemented for {type}")
        };
    }
}