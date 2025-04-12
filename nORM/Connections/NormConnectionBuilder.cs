using System.Data;
using System.Data.Entity.Core;

namespace nORM.Connections;

public class NormConnectionBuilder(DatabaseProviderType databaseType)
{
    public DatabaseProviderType DatabaseProviderType { get; set; } = databaseType;
    private string ConnectionString { get; set; } = string.Empty;

    public NormConnectionBuilder SetFileDataSource(string fileName)
    {
        if (DatabaseProviderType != DatabaseProviderType.Sqlite)
        {
            throw new ProviderIncompatibleException("You cannot use a file data source in: " + DatabaseProviderType);
        }

        ConnectionString = $"Data Source={fileName}";

        return this;
    }
    
    public INormConnection Build(bool autoConnect = false)
    {
        INormConnection connection;
        
        switch (DatabaseProviderType)
        {
            case DatabaseProviderType.Sqlite:
            {
                connection = new NormSqliteConnection(ConnectionString);
                return autoConnect ? connection.Connect() : connection;
            }
        }

        throw new NotImplementedException("Unimplemented");
    }
}