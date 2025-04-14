using nORM.Connections;
using nORM.Tests.Models;

namespace nORM.Tests.Tests.CRUD;

[Trait("Category","CRUD")]
public class DeleteTests
{
    [Fact]
    public void DeleteRow_Sqlite()
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
        var result = collectionContext.Remove(insertedPost);
        
        Assert.True(result);
    }
    
    [Fact]
    public void DeleteRow_Mysql()
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
        var result = collectionContext.Remove(insertedPost);
        
        Assert.True(result);
    }
}