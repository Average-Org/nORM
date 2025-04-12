using nORM.Builders.Queries;
using nORM.Connections;
using nORM.Tests.Models;
using Xunit.Abstractions;

namespace nORM.Tests;

public class NormConnectionBuilderTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private StringWriter _sw = new();
    
    private INormConnection _sharedConnection;
    
    public NormConnectionBuilderTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        _sw = new StringWriter();
        Console.SetOut(_sw);
        Console.SetError(_sw);
    }

    [Fact]
    public void EstablishConnection()
    {
        _sharedConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
            .SetFileDataSource("database.sqlite")
            .Build(autoConnect: false)
            .Connect();

        Assert.NotNull(_sharedConnection);
    }
    
    [Fact]
    public void CreateTable()
    {
        _sharedConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
            .SetFileDataSource("database.sqlite")
            .Build(autoConnect: false)
            .Connect();

        var collectionContext = _sharedConnection.Collection<Post>();

        Assert.NotNull(collectionContext);
    }
    
    [Fact]
    public void Insert_Test()
    {
        _sharedConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
            .SetFileDataSource("database.sqlite")
            .Build(autoConnect: false)
            .Connect();

        var collectionContext = _sharedConnection.Collection<Post>();

        var post = new Post
        {
            Title = "Test",
            Description = "Test description",
            AuthorId = "test_author",
            CreatedAt = DateTime.UtcNow
        };

        var insertedPost = collectionContext.Insert(post);

        Assert.NotNull(insertedPost);
        Assert.Equal(post.Title, insertedPost.Title);
    }
    
    
}