using System.Diagnostics;
using nORM.Connections;
using nORM.Models.Context;
using nORM.Tests.Models;
using Xunit.Abstractions;

namespace nORM.Tests.Tests.StressTests;

[Collection("StressTests")]
[Trait("Category", "Stress Tests")]
public class InsertStressTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly INormConnection _sharedMemoryConnection;
    private readonly INormConnection _sharedFileConnection;
    private readonly INormConnection _sharedMySqlConnection;

    public static IEnumerable<object[]> InsertCounts =>
    [
        [5],
        [10],
        [100],
    ];

    public InsertStressTests(ITestOutputHelper testOutputHelper)
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
        
        _sharedMySqlConnection = Utilities.CreateNewMySqlConnection();
    }

    [Theory, MemberData(nameof(InsertCounts))]
    public void InsertMemory_SqliteStressTest(int count) => RunStressTest(count, _sharedMemoryConnection);

    [Theory, MemberData(nameof(InsertCounts))]
    public void InsertFileSource_SqliteStressTest(int count) => RunStressTest(count, _sharedFileConnection);
    
    [Theory, MemberData(nameof(InsertCounts))]
    public void InsertLocalHost_MySqlStressTest(int count) => RunStressTest(count, _sharedMySqlConnection);

    private void RunStressTest(int count, INormConnection connection)
    {
        var collection = connection.Collection<Post>();
        var now = DateTime.UtcNow;

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        int[] gcBefore = Utilities.GetGcCounts();

        var stopwatch = Stopwatch.StartNew();
        long totalInsertTime = 0;

        const int iterations = 10;
        for (int i = 0; i < iterations; i++)
        {
            totalInsertTime += PerformInsert(collection, count, now);
        }

        stopwatch.Stop();

        long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
        int[] gcAfter = Utilities.GetGcCounts();
        long allocatedBytes = allocatedAfter - allocatedBefore;

        _testOutputHelper.WriteLine($"Inserted {count} posts, {iterations}x");
        _testOutputHelper.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds} ms");
        _testOutputHelper.WriteLine($"Total allocated: {allocatedBytes / 1024.0:F2} KB");

        for (int gen = 0; gen <= GC.MaxGeneration; gen++)
        {
            int collections = gcAfter[gen] - gcBefore[gen];
            _testOutputHelper.WriteLine($"Gen {gen} GCs: {collections}");
        }

        _testOutputHelper.WriteLine($"Average insert iteration time: {totalInsertTime / iterations} ms");
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
                CreatedAt = timestamp
            };

            var inserted = context.Insert(post, transaction);

            Assert.NotNull(inserted);
            Assert.Equal(post.Title, inserted.Title);
        }

        transaction.Commit();
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    [Fact]
    public void Dispose()
    {
        _sharedMemoryConnection.Dispose();
        _sharedFileConnection.Dispose();

        _testOutputHelper.WriteLine("Connections disposed.");
    }
}
