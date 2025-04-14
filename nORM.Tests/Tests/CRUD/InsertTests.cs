using nORM.Connections;
using nORM.Tests.Models;

namespace nORM.Tests.Tests.CRUD;

[Trait("Category","CRUD")]
public class InsertTests
{
    [Fact]
    public void InsertRow_Sqlite()
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

        Assert.NotNull(insertedPost);
        Assert.Equal(post.Title, insertedPost.Title);
    }

    [Fact]
    public void InsertRow_Mysql()
    {
        var mysqlConnection = Utilities.CreateNewMySqlConnection();
        
        var collectionContext = mysqlConnection.Collection<Post>();
        
        var post = new Post
        {
            Title = "Test",
            Description = "Test description",
            CreatedAt = DateTime.UtcNow
        };
        
        var insertedPost = collectionContext.Insert(post);
        
        Assert.NotNull(insertedPost);
        Assert.Equal(post.Title, insertedPost.Title);
    }
}