using System.Data;
using System.Transactions;
using nORM.Models;
using nORM.Models.Context;
using nORM.Models.Properties;

namespace nORM.Connections;

/// <summary>
/// Defines the core functionality for database connections in the nORM framework.
/// </summary>
/// <remarks>
/// This interface provides a unified API for interacting with different database providers,
/// abstracting away the specifics of each database system.
/// </remarks>
public interface INormConnection
{
    /// <summary>
    /// Gets the database provider type for this connection.
    /// </summary>
    DatabaseProviderType DatabaseProviderType { get; }

    /// <summary>
    /// Gets the underlying database connection.
    /// </summary>
    /// <returns>An implementation of IDbConnection specific to the current provider.</returns>
    IDbConnection GetDbConnection();

    /// <summary>
    /// Opens the connection to the database.
    /// </summary>
    /// <returns>The connected instance of INormConnection.</returns>
    INormConnection Connect();

    /// <summary>
    /// Executes a non-query command against the database.
    /// </summary>
    /// <param name="executionProperties">The properties containing the SQL command to execute.</param>
    void ExecuteNonQuery(IExecutionProperties executionProperties);

    /// <summary>
    /// Executes a query command and returns a data reader.
    /// </summary>
    /// <param name="executionProperties">The properties containing the SQL command to execute.</param>
    /// <returns>An IDataReader that can be used to read the results of the query.</returns>
    IDataReader ExecuteQuery(IExecutionProperties executionProperties);

    /// <summary>
    /// Executes a query that returns a single value.
    /// </summary>
    /// <param name="executionProperties">The properties containing the SQL command to execute.</param>
    /// <param name="transaction">An optional transaction in which to execute the command.</param>
    /// <returns>The first column of the first row in the result set.</returns>
    object ExecuteScalar(IExecutionProperties executionProperties, IDbTransaction? transaction = null);

    /// <summary>
    /// Executes a query and returns the results as a collection of dictionaries.
    /// </summary>
    /// <param name="executionProperties">The properties containing the SQL command to execute.</param>
    /// <returns>A list of dictionaries, where each dictionary represents a row in the result set.</returns>
    List<Dictionary<string, object>> Query(IExecutionProperties executionProperties);

    /// <summary>
    /// Gets a collection context for accessing and managing entities of the specified type.
    /// </summary>
    /// <typeparam name="T">The entity type to work with.</typeparam>
    /// <returns>A collection context for the specified entity type.</returns>
    public ICollectionContext<T> Collection<T>() where T : NormEntity;

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    /// <returns>A new database transaction.</returns>
    public IDbTransaction BeginTransaction();

    /// <summary>
    /// Closes and disposes the connection.
    /// </summary>
    public void Dispose();
}