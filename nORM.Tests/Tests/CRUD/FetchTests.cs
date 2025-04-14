using nORM.Connections;
using nORM.Tests.Models;

namespace nORM.Tests.Tests.CRUD;

[Trait("Category","CRUD")]
public class FetchTests
{
    [Fact]
    public void FindOne_SqliteTest()
    {
        var sqliteConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
            .SetExplicitDataSource("database.sqlite")
            .BuildAndConnect();
        
        var collectionContext = sqliteConnection.Collection<Post>();

        var post = new Post
        {
            Title = "Test",
            Description = "Test description",
            AuthorId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var insertedPost = collectionContext.Insert(post);

        var fetchedPost = collectionContext.FindOne(p => p.Id == insertedPost.Id);
        Assert.Equal(insertedPost, fetchedPost);
    }
    
    [Fact]
    public void FindOne_MysqlTest()
    {
        var sqliteConnection = Utilities.CreateNewMySqlConnection();
        
        var collectionContext = sqliteConnection.Collection<Post>();

        var post = new Post
        {
            Title = "Test",
            Description = "Test description",
            AuthorId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var insertedPost = collectionContext.Insert(post);

        var fetchedPost = collectionContext.FindOne(p => p.Id == insertedPost.Id);
        Assert.Equal(insertedPost, fetchedPost);
    }
}