using System.Data.Entity.Core;

namespace nORM.Connections;

public class NormConnectionBuilder(DatabaseProviderType databaseType)
{
    public DatabaseProviderType DatabaseProviderType { get; set; } = databaseType;
    private string ConnectionString { get; set; } = string.Empty;

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

    public NormConnectionBuilder SetHostname(string hostName)
    {
        if (DatabaseProviderType != DatabaseProviderType.MySql)
        {
            throw new ProviderIncompatibleException("You cannot use a hostname in: " + DatabaseProviderType);
        }

        ConnectionString = $"server={hostName};";

        return this;
    }
    
    public NormConnectionBuilder SetUsername(string username)
    {
        if (DatabaseProviderType != DatabaseProviderType.MySql)
        {
            throw new ProviderIncompatibleException("You cannot use a username in: " + DatabaseProviderType);
        }

        ConnectionString += $"user={username};";

        return this;
    }
    
    public NormConnectionBuilder SetPassword(string password)
    {
        if (DatabaseProviderType != DatabaseProviderType.MySql)
        {
            throw new ProviderIncompatibleException("You cannot use a password in: " + DatabaseProviderType);
        }

        ConnectionString += $"password={password};";

        return this;
    }
    
    public NormConnectionBuilder SetDatabase(string database)
    {
        if (DatabaseProviderType != DatabaseProviderType.MySql)
        {
            throw new ProviderIncompatibleException("You cannot use a database in: " + DatabaseProviderType);
        }

        ConnectionString += $"database={database};";

        return this;
    }
    
    public NormConnectionBuilder SetPort(int port)
    {
        if (DatabaseProviderType != DatabaseProviderType.MySql)
        {
            throw new ProviderIncompatibleException("You cannot use a port in: " + DatabaseProviderType);
        }

        ConnectionString += $"port={port};";

        return this;
    }
    
    public NormConnectionBuilder UseInMemoryDataSource()
    {
        if (DatabaseProviderType != DatabaseProviderType.Sqlite)
        {
            throw new ProviderIncompatibleException("You cannot use a in-memory data source in: " + DatabaseProviderType);
        }

        ConnectionString = "Data Source=:memory:";

        return this;
    }
    
    public INormConnection BuildAndConnect()
    {
        return Build(true);
    }
    
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