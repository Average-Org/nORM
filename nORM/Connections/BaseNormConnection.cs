using System.Data;
using System.Transactions;
using nORM.Models;
using nORM.Models.Context;
using nORM.Models.Properties;

namespace nORM.Connections;

/// <summary>
/// Abstract base class that provides common implementation of INormConnection for different database providers.
/// </summary>
/// <remarks>
/// This class serves as the foundation for all database-specific connection implementations,
/// providing shared functionality while allowing specific providers to implement their unique features.
/// </remarks>
public abstract class BaseNormConnection : INormConnection
{
    /// <summary>
    /// Dictionary that stores collection contexts for entity types to avoid recreating them.
    /// </summary>
    protected Dictionary<Type, object> _collectionContexts = new();
    
    /// <summary>
    /// Gets the database provider type for this connection.
    /// </summary>
    public abstract DatabaseProviderType DatabaseProviderType { get; }
    
    /// <summary>
    /// Gets the underlying database connection.
    /// </summary>
    /// <returns>An implementation of IDbConnection specific to the current provider.</returns>
    public abstract IDbConnection GetDbConnection();
    
    /// <summary>
    /// Opens the connection to the database.
    /// </summary>
    /// <returns>The connected instance of INormConnection.</returns>
    public abstract INormConnection Connect();
    
    /// <summary>
    /// Executes a non-query command against the database.
    /// </summary>
    /// <param name="executionProperties">The properties containing the SQL command to execute.</param>
    public abstract void ExecuteNonQuery(IExecutionProperties executionProperties);
    
    /// <summary>
    /// Executes a query command and returns a data reader.
    /// </summary>
    /// <param name="executionProperties">The properties containing the SQL command to execute.</param>
    /// <returns>An IDataReader that can be used to read the results of the query.</returns>
    public abstract IDataReader ExecuteQuery(IExecutionProperties properties);
    
    /// <summary>
    /// Executes a query that returns a single value.
    /// </summary>
    /// <param name="executionProperties">The properties containing the SQL command to execute.</param>
    /// <param name="transaction">An optional transaction in which to execute the command.</param>
    /// <returns>The first column of the first row in the result set.</returns>
    public abstract object ExecuteScalar(IExecutionProperties executionProperties, IDbTransaction? transaction = null);

    /// <summary>
    /// Executes a query and returns the results as a collection of dictionaries.
    /// </summary>
    /// <param name="executionProperties">The properties containing the SQL command to execute.</param>
    /// <returns>A list of dictionaries, where each dictionary represents a row in the result set.</returns>
    public abstract List<Dictionary<string, object>> Query(IExecutionProperties executionProperties);

    /// <summary>
    /// Gets a collection context for accessing and managing entities of the specified type.
    /// </summary>
    /// <typeparam name="T">The entity type to work with.</typeparam>
    /// <returns>A collection context for the specified entity type.</returns>
    public ICollectionContext<T> Collection<T>() where T : NormEntity
    {
        if (_collectionContexts.ContainsKey(typeof(T)))
        {
            return (ICollectionContext<T>)_collectionContexts[typeof(T)];
        }

        _collectionContexts[typeof(T)] = SqlCollectionContext<T>.Create(this);
        return (ICollectionContext<T>)_collectionContexts[typeof(T)];
    }

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    /// <returns>A new database transaction.</returns>
    public abstract IDbTransaction BeginTransaction();
    
    /// <summary>
    /// Closes and disposes the connection.
    /// </summary>
    public abstract void Dispose();
}