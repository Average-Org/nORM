using nORM.Connections;
using nORM.Tests.Models;

namespace nORM.Tests.Tests.TableOperations;

[Trait("Category","Collection Operations")]
public class TruncateCollectionTests
{
    [Fact]
    public void TruncateTable_Sqlite()
    {
        var sqliteConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
            .SetExplicitDataSource("database.sqlite")
            .BuildAndConnect();
        
        var collectionContext = sqliteConnection.Collection<Post>();
        var post = new Post
        {
            Title = "Test",
            Description = "Test description",
            CreatedAt = DateTime.UtcNow
        };

        var insertedPost = collectionContext.Insert(post);

        var result = collectionContext.Truncate();
        Assert.True(result);
    }
}