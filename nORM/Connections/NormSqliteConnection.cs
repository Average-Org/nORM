using System.Data;
using System.Data.SQLite;

namespace nORM.Connections;

/// <summary>
/// Implements a connection to SQLite databases for the nORM framework.
/// </summary>
/// <remarks>
/// Provides SQLite-specific functionality for both file-based and in-memory databases.
/// </remarks>
public class NormSqliteConnection(string connectionString) : NormSqlConnection(connectionString)
{
    private readonly SQLiteConnection _connection = new(connectionString);
    
    /// <summary>
    /// Gets the database provider type, which is SQLite for this implementation.
    /// </summary>
    public override DatabaseProviderType DatabaseProviderType { get; } = DatabaseProviderType.Sqlite;

    /// <summary>
    /// Gets the underlying SQLite database connection.
    /// </summary>
    /// <returns>The SQLite connection object.</returns>
    public override IDbConnection GetDbConnection()
    {
        return _connection;
    }
}