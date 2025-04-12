using System.Data;
using nORM.Models;
using nORM.Models.Properties;

namespace nORM.Connections;

public abstract class BaseNormConnection : INormConnection
{
    protected Dictionary<Type, object> _collectionContexts = new();
    public abstract IDbConnection GetDbConnection();
    public abstract INormConnection Connect();
    public abstract void ExecuteNonQuery(IExecutionProperties executionProperties);
    public abstract IDataReader ExecuteQuery(IExecutionProperties properties);
    public abstract List<Dictionary<string, object>> Query(IExecutionProperties executionProperties);

    public ICollectionContext<T> Collection<T>() where T : NormEntity
    {
        if (_collectionContexts.ContainsKey(typeof(T)))
        {
            return (ICollectionContext<T>)_collectionContexts[typeof(T)];
        }

        _collectionContexts[typeof(T)] = CollectionContext<T>.Create(this);
        return (ICollectionContext<T>)_collectionContexts[typeof(T)];
    }

}