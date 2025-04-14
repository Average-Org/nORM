using System.Data;
using MySqlConnector;

namespace nORM.Connections;

public class NormMySqlConnection(string connectionString) : NormSqlConnection(connectionString)
{
    private readonly IDbConnection _connection = new MySqlConnection(connectionString);

    public override DatabaseProviderType DatabaseProviderType { get; } = DatabaseProviderType.MySql;

    public override IDbConnection GetDbConnection()
    {
        return _connection;
    }
    
}