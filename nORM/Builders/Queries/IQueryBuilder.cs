using nORM.Models;
using nORM.Models.Properties;

namespace nORM.Builders.Queries;

public interface IQueryBuilder
{
    public IExecutionProperties GetCreateCollectionQuery<T>() where T : NormEntity;
}