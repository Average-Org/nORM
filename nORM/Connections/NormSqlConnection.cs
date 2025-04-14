using System.Data;
using System.Data.Entity.Core;
using System.Data.SQLite;
using nORM.Models.Properties;

namespace nORM.Connections;

/// <summary>
/// Abstract base class for SQL-based database connections in the nORM framework.
/// </summary>
/// <remarks>
/// This class provides a common implementation for SQL database providers,
/// handling SQL query execution and connection management.
/// </remarks>
public abstract class NormSqlConnection(string connectionString) : BaseNormConnection
{
    private readonly IDbConnection _connection = new SQLiteConnection(connectionString);

    /// <summary>
    /// Gets the underlying database connection.
    /// </summary>
    /// <returns>An implementation of IDbConnection specific to this SQL provider.</returns>
    public override IDbConnection GetDbConnection()
    {
        return _connection;
    }

    /// <summary>
    /// Opens the connection to the database.
    /// </summary>
    /// <returns>The connected instance of INormConnection.</returns>
    /// <exception cref="Exception">Thrown when the connection fails.</exception>
    public override INormConnection Connect()
    {
        try
        {
            GetDbConnection().Open();
            return this;
        }
        catch (Exception ex)
        {
            //TODO: Replace with logging abstraction
            throw;
        }
    }

    /// <summary>
    /// Executes a query command and returns a data reader.
    /// </summary>
    /// <param name="properties">The properties containing the SQL command to execute.</param>
    /// <returns>An IDataReader that can be used to read the results of the query.</returns>
    /// <exception cref="ProviderIncompatibleException">Thrown if the execution properties are not SQL-compatible.</exception>
    public override IDataReader ExecuteQuery(IExecutionProperties properties)
    {
        if (properties is not SqlExecutionProperties { } sqlQuery)
        {
            throw new ProviderIncompatibleException(
                "You attempted to pass execution properties that do not match the connection type");
        }

        var command = GetDbConnection().CreateCommand();
        command.CommandText = sqlQuery.ToString();
        return command.ExecuteReader();
    }

    /// <summary>
    /// Executes a non-query command against the database.
    /// </summary>
    /// <param name="executionProperties">The properties containing the SQL command to execute.</param>
    /// <exception cref="ProviderIncompatibleException">Thrown if the execution properties are not SQL-compatible.</exception>
    public override void ExecuteNonQuery(IExecutionProperties executionProperties)
    {
        if (executionProperties is not SqlExecutionProperties { } sqlQuery)
        {
            throw new ProviderIncompatibleException(
                "You attempted to pass execution properties that do not match the connection type");
        }

        var command = GetDbConnection().CreateCommand();
        command.CommandText = sqlQuery.ToString();
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Executes a query and returns the results as a collection of dictionaries.
    /// </summary>
    /// <param name="executionProperties">The properties containing the SQL command to execute.</param>
    /// <returns>A list of dictionaries, where each dictionary represents a row in the result set.</returns>
    /// <exception cref="ProviderIncompatibleException">Thrown if the execution properties are not SQL-compatible.</exception>
    public override List<Dictionary<string, object>> Query(IExecutionProperties executionProperties)
    {
        if (executionProperties is not SqlExecutionProperties { } sqlQuery)
        {
            throw new ProviderIncompatibleException(
                "You attempted to pass execution properties that do not match the connection type");
        }

        var command = GetDbConnection().CreateCommand();
        command.CommandText = sqlQuery.ToString();

        var reader = command.ExecuteReader();

        var result = new List<Dictionary<string, object>>();

        while (reader.Read())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }

            result.Add(row);
        }

        reader.Close();

        return result;
    }

    /// <summary>
    /// Executes a query that returns a single value.
    /// </summary>
    /// <param name="executionProperties">The properties containing the SQL command to execute.</param>
    /// <param name="transaction">An optional transaction in which to execute the command.</param>
    /// <returns>The first column of the first row in the result set.</returns>
    /// <exception cref="ProviderIncompatibleException">Thrown if the execution properties are not SQL-compatible.</exception>
    public override object ExecuteScalar(IExecutionProperties executionProperties, IDbTransaction? transaction = null)
    {
        if (executionProperties is not SqlExecutionProperties { } sqlQuery)
        {
            throw new ProviderIncompatibleException(
                "You attempted to pass execution properties that do not match the connection type");
        }

        var command = GetDbConnection().CreateCommand();
        if (DatabaseProviderType == DatabaseProviderType.MySql)
        {
            command.Transaction = transaction;
        }
        
        command.CommandText = sqlQuery.ToString();
        return command.ExecuteScalar() ?? DBNull.Value;
    }
    
    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    /// <returns>A new database transaction.</returns>
    public override IDbTransaction BeginTransaction()
    {
        return GetDbConnection().BeginTransaction();
    }
    
    /// <summary>
    /// Closes and disposes the connection.
    /// </summary>
    public override void Dispose()
    {
        GetDbConnection().Close();
        GetDbConnection().Dispose();
    }
}