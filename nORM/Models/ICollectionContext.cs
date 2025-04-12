namespace nORM.Models;

public interface ICollectionContext<T> where T : NormEntity
{
    public T Insert(T entity);

}