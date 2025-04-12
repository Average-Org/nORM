using System.Data;
using nORM.Models;
using nORM.Models.Properties;

namespace nORM.Connections;

public interface INormConnection
{
    IDbConnection GetDbConnection();
    INormConnection Connect();

    void ExecuteNonQuery(IExecutionProperties executionProperties);
    IDataReader ExecuteQuery(IExecutionProperties properties);
    List<Dictionary<string, object>> Query(IExecutionProperties executionProperties);

    public ICollectionContext<T> Collection<T>() where T : NormEntity;
}