using System.Data;
using System.Data.SQLite;

namespace nORM.Connections;

public class NormSqliteConnection(string connectionString) : NormSqlConnection(connectionString)
{
    private readonly SQLiteConnection _connection = new(connectionString);
    
    public override DatabaseProviderType DatabaseProviderType { get; } = DatabaseProviderType.Sqlite;

    public override IDbConnection GetDbConnection()
    {
        return _connection;
    }
}