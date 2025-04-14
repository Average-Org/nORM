using System.Data.Entity.Core;

namespace nORM.Connections;

/// <summary>
/// Provides a fluent interface for building database connections with various providers.
/// </summary>
/// <remarks>
/// This builder enables configuring and creating database connections with different
/// providers in a consistent way, abstracting away provider-specific connection details.
/// </remarks>
public class NormConnectionBuilder(DatabaseProviderType databaseType)
{
    /// <summary>
    /// Gets or sets the type of database provider to use for the connection.
    /// </summary>
    public DatabaseProviderType DatabaseProviderType { get; set; } = databaseType;
    
    /// <summary>
    /// Gets or sets the connection string for the database connection.
    /// </summary>
    private string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Sets an explicit data source for SQLite connections.
    /// </summary>
    /// <param name="dataSource">The path to the SQLite database file, or ":memory:" for in-memory databases.</param>
    /// <returns>The builder instance for chaining calls.</returns>
    /// <exception cref="ProviderIncompatibleException">Thrown if the current provider is not SQLite.</exception>
    public NormConnectionBuilder SetExplicitDataSource(string dataSource)
    {
        if (DatabaseProviderType != DatabaseProviderType.Sqlite)
        {
            throw new ProviderIncompatibleException("You cannot use a file data source in: " + DatabaseProviderType);
        }

        // if someone passes ":memory:" or wants to use in-memory sqlite with shared cache
        if (dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase) ||
            dataSource.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase))
        {
            ConnectionString = $"Data Source={dataSource}";
        }
        else
        {
            // treat as file-based datasource
            ConnectionString = $"Data Source={dataSource};Mode=ReadWriteCreate;Cache=Shared";
        }

        return this;
    }

    /// <summary>
    /// Sets the hostname for MySQL connections.
    /// </summary>
    /// <param name="hostName">The hostname of the MySQL server.</param>
    /// <returns>The builder instance for chaining calls.</returns>
    /// <exception cref="ProviderIncompatibleException">Thrown if the current provider is not MySQL.</exception>
    public NormConnectionBuilder SetHostname(string hostName)
    {
        if (DatabaseProviderType != DatabaseProviderType.MySql)
        {
            throw new ProviderIncompatibleException("You cannot use a hostname in: " + DatabaseProviderType);
        }

        ConnectionString = $"server={hostName};";

        return this;
    }
    
    /// <summary>
    /// Sets the username for MySQL connections.
    /// </summary>
    /// <param name="username">The username for authenticating with the MySQL server.</param>
    /// <returns>The builder instance for chaining calls.</returns>
    /// <exception cref="ProviderIncompatibleException">Thrown if the current provider is not MySQL.</exception>
    public NormConnectionBuilder SetUsername(string username)
    {
        if (DatabaseProviderType != DatabaseProviderType.MySql)
        {
            throw new ProviderIncompatibleException("You cannot use a username in: " + DatabaseProviderType);
        }

        ConnectionString += $"user={username};";

        return this;
    }
    
    /// <summary>
    /// Sets the password for MySQL connections.
    /// </summary>
    /// <param name="password">The password for authenticating with the MySQL server.</param>
    /// <returns>The builder instance for chaining calls.</returns>
    /// <exception cref="ProviderIncompatibleException">Thrown if the current provider is not MySQL.</exception>
    public NormConnectionBuilder SetPassword(string password)
    {
        if (DatabaseProviderType != DatabaseProviderType.MySql)
        {
            throw new ProviderIncompatibleException("You cannot use a password in: " + DatabaseProviderType);
        }

        ConnectionString += $"password={password};";

        return this;
    }
    
    /// <summary>
    /// Sets the database name for MySQL connections.
    /// </summary>
    /// <param name="database">The name of the MySQL database to connect to.</param>
    /// <returns>The builder instance for chaining calls.</returns>
    /// <exception cref="ProviderIncompatibleException">Thrown if the current provider is not MySQL.</exception>
    public NormConnectionBuilder SetDatabase(string database)
    {
        if (DatabaseProviderType != DatabaseProviderType.MySql)
        {
            throw new ProviderIncompatibleException("You cannot use a database in: " + DatabaseProviderType);
        }

        ConnectionString += $"database={database};";

        return this;
    }
    
    /// <summary>
    /// Sets the port number for MySQL connections.
    /// </summary>
    /// <param name="port">The port number of the MySQL server.</param>
    /// <returns>The builder instance for chaining calls.</returns>
    /// <exception cref="ProviderIncompatibleException">Thrown if the current provider is not MySQL.</exception>
    public NormConnectionBuilder SetPort(int port)
    {
        if (DatabaseProviderType != DatabaseProviderType.MySql)
        {
            throw new ProviderIncompatibleException("You cannot use a port in: " + DatabaseProviderType);
        }

        ConnectionString += $"port={port};";

        return this;
    }
    
    /// <summary>
    /// Configures the builder to use an in-memory SQLite database.
    /// </summary>
    /// <returns>The builder instance for chaining calls.</returns>
    /// <exception cref="ProviderIncompatibleException">Thrown if the current provider is not SQLite.</exception>
    public NormConnectionBuilder UseInMemoryDataSource()
    {
        if (DatabaseProviderType != DatabaseProviderType.Sqlite)
        {
            throw new ProviderIncompatibleException("You cannot use a in-memory data source in: " + DatabaseProviderType);
        }

        ConnectionString = "Data Source=:memory:";

        return this;
    }
    
    /// <summary>
    /// Builds and automatically connects to the database.
    /// </summary>
    /// <returns>A connected INormConnection instance.</returns>
    public INormConnection BuildAndConnect()
    {
        return Build(true);
    }
    
    /// <summary>
    /// Builds the connection with optional automatic connection.
    /// </summary>
    /// <param name="autoConnect">If true, the connection will be opened automatically.</param>
    /// <returns>An INormConnection instance, connected if autoConnect is true.</returns>
    /// <exception cref="NotImplementedException">Thrown if the provider type is not supported.</exception>
    public INormConnection Build(bool autoConnect = false)
    {
        INormConnection connection;
        
        switch (DatabaseProviderType)
        {
            case DatabaseProviderType.Sqlite:
            {
                connection = new NormSqliteConnection(ConnectionString);
                break;
            }
            case DatabaseProviderType.MySql:
            {
                connection = new NormMySqlConnection(ConnectionString);
                break;
            }
            default:
            {
                throw new NotImplementedException("Unimplemented");
            }
        }

        return autoConnect ? connection.Connect() : connection;
    }
}