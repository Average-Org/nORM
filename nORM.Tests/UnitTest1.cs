using System.Diagnostics;
using nORM.Connections;
using nORM.Models.Context;
using nORM.Tests.Models;
using Xunit.Abstractions;

namespace nORM.Tests;

public class NormConnectionBuilderTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private StringWriter _sw;

    private readonly INormConnection _sharedConnection;

    public NormConnectionBuilderTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        _sw = new StringWriter();
        Console.SetOut(_sw);
        Console.SetError(_sw);

        _sharedConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
            .SetFileDataSource("database.sqlite")
            /*.UseInMemoryDataSource()*/
            /*.Build()
            .Connect()*/
            .BuildAndConnect();
    }

    [Fact]
    public void EstablishConnection()
    {
        Assert.NotNull(_sharedConnection);
    }

    [Fact]
    public void CreateTable()
    {
        var collectionContext = _sharedConnection.Collection<Post>();
        Assert.NotNull(collectionContext);
    }

    [Fact]
    public void FetchOne()
    {
        var collectionContext = _sharedConnection.Collection<Post>();

        var post = new Post
        {
            Title = "Test",
            Description = "Test description",
            AuthorId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var insertedPost = collectionContext.Insert(post);

        var fetchedPost = collectionContext.FindOne(p => p.Id == insertedPost.Id);

        _testOutputHelper.WriteLine(_sw.ToString());
        Assert.Equal(insertedPost, fetchedPost);
    }

    [Fact]
    public void TruncateTable()
    {
        var collectionContext = _sharedConnection.Collection<Post>();
        var post = new Post
        {
            Title = "Test",
            Description = "Test description",
            AuthorId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var insertedPost = collectionContext.Insert(post);

        var result = collectionContext.Truncate();
        Assert.True(result);
    }

    [Fact]
    public void Insert_Test()
    {
        var collectionContext = _sharedConnection.Collection<Post>();

        var post = new Post
        {
            Title = "Test",
            Description = "Test description",
            AuthorId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var insertedPost = collectionContext.Insert(post);

        Assert.NotNull(insertedPost);
        Assert.Equal(post.Title, insertedPost.Title);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void Insert_Stress_Test(int count)
    {
        var collectionContext = _sharedConnection.Collection<Post>();
        var time = DateTime.UtcNow;

        // Force GC and get baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long memoryBefore = GC.GetTotalMemory(true);
        int[] gcCountsBefore = Enumerable.Range(0, GC.MaxGeneration + 1)
            .Select(GC.CollectionCount)
            .ToArray();

        var stopwatch = Stopwatch.StartNew();

        long totalTime = 0;
        int totalIterations = 0;
        
        for (int i = 0; i < 10; i++)
        {
            totalTime += DoStressTest(count, collectionContext, time);
            totalIterations++;
        }

        stopwatch.Stop();

        long memoryAfter = GC.GetTotalMemory(true);
        int[] gcCountsAfter = Enumerable.Range(0, GC.MaxGeneration + 1)
            .Select(GC.CollectionCount)
            .ToArray();

        long memoryUsed = memoryAfter - memoryBefore;

        _testOutputHelper.WriteLine($"Inserted {count} posts 10x");
        _testOutputHelper.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");
        _testOutputHelper.WriteLine($"Memory used: {memoryUsed / 1024.0:F2} KB");

        for (int gen = 0; gen <= GC.MaxGeneration; gen++)
        {
            var collections = gcCountsAfter[gen] - gcCountsBefore[gen];
            _testOutputHelper.WriteLine($"Gen {gen} GCs: {collections}");
        }
        
        _testOutputHelper.WriteLine($"Average time for each iteration of {count}: {(totalIterations > 0 ? (int)(totalTime / totalIterations) : -1)} ms");
    }

    private static long DoStressTest(int count, ICollectionContext<Post> collectionContext, DateTime time)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var transaction = collectionContext.BeginTransaction();
        for (var i = 0; i < count; i++)
        {
            var post = new Post
            {
                Title = $"Test {i}",
                Description = "Test description",
                AuthorId = 1,
                CreatedAt = time
            };

            var insertedPost = collectionContext.Insert(post);

            Assert.NotNull(insertedPost);
            Assert.Equal(post.Title, insertedPost.Title);
        }

        transaction.Commit();
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }


    [Fact]
    public void Delete_Test()
    {
        var collectionContext = _sharedConnection.Collection<Post>();

        var post = new Post
        {
            Title = "Test",
            Description = "Test description",
            AuthorId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var insertedPost = collectionContext.Insert(post);
        var result = collectionContext.Remove(insertedPost);

        _testOutputHelper.WriteLine(_sw.ToString());

        Assert.True(result);
    }
    
    [Fact]
    public void Dispose()
    {
        _sw?.Dispose();
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
    }
}