using System.Data;
using MySqlConnector;

namespace nORM.Connections;

/// <summary>
/// Implements a connection to MySQL databases for the nORM framework.
/// </summary>
/// <remarks>
/// Provides MySQL-specific functionality for connecting to and interacting with MySQL servers.
/// </remarks>
public class NormMySqlConnection(string connectionString) : NormSqlConnection(connectionString)
{
    private readonly IDbConnection _connection = new MySqlConnection(connectionString);

    /// <summary>
    /// Gets the database provider type, which is MySQL for this implementation.
    /// </summary>
    public override DatabaseProviderType DatabaseProviderType { get; } = DatabaseProviderType.MySql;

    /// <summary>
    /// Gets the underlying MySQL database connection.
    /// </summary>
    /// <returns>The MySQL connection object.</returns>
    public override IDbConnection GetDbConnection()
    {
        return _connection;
    }
    
}