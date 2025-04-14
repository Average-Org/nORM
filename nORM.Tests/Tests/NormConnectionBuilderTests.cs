using nORM.Connections;

namespace nORM.Tests.Tests;

[Trait("Category", "Connection")]
public class NormConnectionBuilderTests
{
    [Fact]
    public void EstablishSqliteConnection_FileDataSource()
    {
        var sqliteConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
            .SetExplicitDataSource("database.sqlite")
            .BuildAndConnect();
        
        Assert.NotNull(sqliteConnection);
    }
    
    [Fact]
    public void EstablishSqliteConnection_MemoryDataSource()
    {
        var sqliteConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
            .UseInMemoryDataSource()
            .BuildAndConnect();
        
        Assert.NotNull(sqliteConnection);
    }

    [Fact]
    public void EstablishMysqlConnection()
    {
        var mysqlConnection = new NormConnectionBuilder(DatabaseProviderType.MySql)
            .SetHostname("localhost")
            .SetUsername("test")
            .SetPassword("password")
            .SetDatabase("testdb")
            .BuildAndConnect();
        
        Assert.NotNull(mysqlConnection);
    }
   
}