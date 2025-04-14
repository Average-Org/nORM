using nORM.Connections;

namespace nORM.Tests.Models;

public class Utilities
{
    private static INormConnection CreateNewConnection(string dataSource)
    {
        return new NormConnectionBuilder(DatabaseProviderType.Sqlite)
            .SetExplicitDataSource(dataSource)
            .BuildAndConnect();
    }
    
    public static INormConnection CreateNewMySqlConnection()
    {
        return new NormConnectionBuilder(DatabaseProviderType.MySql)
            .SetHostname("localhost")
            .SetUsername("test")
            .SetPassword("password")
            .SetDatabase("testdb")
            .BuildAndConnect();
    }
}