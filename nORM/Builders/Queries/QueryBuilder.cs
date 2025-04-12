using nORM.Models;
using nORM.Models.Properties;

namespace nORM.Builders.Queries;

public abstract class QueryBuilder : IQueryBuilder
{
    public abstract IExecutionProperties GetCreateCollectionQuery<T>() where T : NormEntity;
}