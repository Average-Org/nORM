using System.Diagnostics;
using nORM.Models.Context;
using nORM.Tests.Models;
using Xunit.Abstractions;

namespace nORM.Tests.Tests.StressTests;

[Collection("StressTests")]
[Trait("Category", "Stress Tests")]
public class SelectStressTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public SelectStressTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static IEnumerable<object[]> SelectCounts =>
    [
        [5],
        [10],
        [100],
    ];

    [Theory, MemberData(nameof(SelectCounts))]
    public void SelectStressTest(int count)
    {
        var connection = Utilities.CreateNewMySqlConnection();
        var collection = connection.Collection<Post>();
        var now = DateTime.UtcNow;

        var post = new Post
        {
            Title = $"Title 1",
            Description = $"Description 2",
            CreatedAt = now
        };
        collection.Insert(post);
        
        // Get allocation stats before
        long allocationsBefore = GC.GetAllocatedBytesForCurrentThread();
        int[] gcBefore = Utilities.GetGcCounts();

        var stopwatch = Stopwatch.StartNew();
        long totalSelectTime = 0;

        const int iterations = 10;
        for (int i = 0; i < iterations; i++)
        {
            totalSelectTime += PerformSelect(collection, count, now);
        }

        stopwatch.Stop();

        // Get allocation stats after
        long allocationsAfter = GC.GetAllocatedBytesForCurrentThread();
        int[] gcAfter = Utilities.GetGcCounts();

        _testOutputHelper.WriteLine($"Total Select Time: {totalSelectTime} ms");
        _testOutputHelper.WriteLine($"Total Allocated: {(allocationsAfter - allocationsBefore) / 1000} KB");
        _testOutputHelper.WriteLine($"GC Count Before: {string.Join(", ", gcBefore)}");
        _testOutputHelper.WriteLine($"GC Count After: {string.Join(", ", gcAfter)}");
        
        _testOutputHelper.WriteLine($"Average select iteration time: {totalSelectTime / iterations} ms");

    }

    private long PerformSelect(ICollectionContext<Post> collection, int count, DateTime now)
    {
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < count; i++)
        {
            var selected = collection.FindOne(p => p.Title == $"Title 1");
            Assert.NotNull(selected);
        }

        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }
}
