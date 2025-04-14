using nORM.Connections;
using nORM.Tests.Models;

namespace nORM.Tests.Tests.TableOperations;

[Trait("Category", "Collection Operations")]
public class CreateCollectionTests
{
    [Fact]
    public void CreateSqliteTable()
    {
        var sqliteConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
            .SetExplicitDataSource("database.sqlite")
            .BuildAndConnect();
        
        var collectionContext = sqliteConnection.Collection<Post>();
        
        Assert.NotNull(collectionContext);
    }
    
    [Fact]
    public void CreateMySqlTable()
    {
        var sqliteConnection = Utilities.CreateNewMySqlConnection();
        
        var collectionContext = sqliteConnection.Collection<Post>();
        
        Assert.NotNull(collectionContext);
    }
}