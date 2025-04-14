using System.Data;
using nORM.Models;
using nORM.Models.Context;
using nORM.Models.Properties;

namespace nORM.Connections;

public interface INormConnection
{
    DatabaseProviderType DatabaseProviderType { get; }
    IDbConnection GetDbConnection();
    INormConnection Connect();

    void ExecuteNonQuery(IExecutionProperties executionProperties);
    IDataReader ExecuteQuery(IExecutionProperties executionProperties);
    object ExecuteScalar(IExecutionProperties executionProperties);
    List<Dictionary<string, object>> Query(IExecutionProperties executionProperties);

    public ICollectionContext<T> Collection<T>() where T : NormEntity;
    public IDbTransaction BeginTransaction();
    public void Dispose();
}