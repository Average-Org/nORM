using System.Data;
using System.Linq.Expressions;

namespace nORM.Models.Context;

public interface ICollectionContext<T> where T : NormEntity
{
    public T Insert(T entity);
    public bool Remove(T entity);
    public bool Truncate();
    public IDbTransaction BeginTransaction();
    public T? FindOne(Expression<Func<T, bool>> predicate);
    public IEnumerable<T> InsertMany(IEnumerable<T> entities);

}