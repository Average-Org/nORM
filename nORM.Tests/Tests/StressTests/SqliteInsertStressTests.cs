using System.Diagnostics;
using nORM.Connections;
using nORM.Models.Context;
using nORM.Tests.Models;
using Xunit.Abstractions;

namespace nORM.Tests.Tests.StressTests;

[Collection("StressTests")]
[Trait("Category","Stress Tests")]
public class SqliteInsertStressTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly INormConnection _sharedMemoryConnection;
    private readonly INormConnection _sharedFileConnection;

    public static IEnumerable<object[]> InsertCounts =>
    [
        [5],
        [10],
        [100],
    ];

    public SqliteInsertStressTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        // Redirect Console output to test output helper
        Console.SetOut(new StringWriter());
        Console.SetError(new StringWriter());

        _sharedMemoryConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
            .SetExplicitDataSource("StressDbMemory;Mode=Memory;Cache=Shared")
            .BuildAndConnect();

        _sharedFileConnection = new NormConnectionBuilder(DatabaseProviderType.Sqlite)
            .SetExplicitDataSource("stress_db.sqlite")
            .BuildAndConnect();
    }

    [Theory, MemberData(nameof(InsertCounts))]
    public void InsertMemory_StressTest(int count) => RunStressTest(count, _sharedMemoryConnection);

    [Theory, MemberData(nameof(InsertCounts))]
    public void InsertFileSource_StressTest(int count) => RunStressTest(count, _sharedFileConnection);

    private void RunStressTest(int count, INormConnection connection)
    {
        var collection = connection.Collection<Post>();
        var now = DateTime.UtcNow;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long memoryBefore = GC.GetTotalMemory(true);
        int[] gcBefore = GetGcCounts();

        var stopwatch = Stopwatch.StartNew();
        long totalInsertTime = 0;

        const int iterations = 10;
        for (int i = 0; i < iterations; i++)
        {
            totalInsertTime += PerformInsert(collection, count, now);
        }

        stopwatch.Stop();

        long memoryAfter = GC.GetTotalMemory(true);
        int[] gcAfter = GetGcCounts();
        long memoryUsed = memoryAfter - memoryBefore;

        _testOutputHelper.WriteLine($"Inserted {count} posts, {iterations}x");
        _testOutputHelper.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds} ms");
        _testOutputHelper.WriteLine($"Memory used: {memoryUsed / 1024.0:F2} KB");

        for (int gen = 0; gen <= GC.MaxGeneration; gen++)
        {
            int collections = gcAfter[gen] - gcBefore[gen];
            _testOutputHelper.WriteLine($"Gen {gen} GCs: {collections}");
        }

        _testOutputHelper.WriteLine($"Average insert time: {totalInsertTime / iterations} ms");
    }

    private static long PerformInsert(ICollectionContext<Post> context, int count, DateTime timestamp)
    {
        var stopwatch = Stopwatch.StartNew();

        using var transaction = context.BeginTransaction();
        for (int i = 0; i < count; i++)
        {
            var post = new Post
            {
                Title = $"Test {i}",
                Description = "Test description",
                AuthorId = 1,
                CreatedAt = timestamp
            };

            var inserted = context.Insert(post);

            Assert.NotNull(inserted);
            Assert.Equal(post.Title, inserted.Title);
        }

        transaction.Commit();
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    private static int[] GetGcCounts() =>
        Enumerable.Range(0, GC.MaxGeneration + 1)
            .Select(GC.CollectionCount)
            .ToArray();
    
    [Fact]
    public void Dispose()
    {
        _sharedMemoryConnection.Dispose();
        _sharedFileConnection.Dispose();
        
        _testOutputHelper.WriteLine("Connections disposed.");
    }
}