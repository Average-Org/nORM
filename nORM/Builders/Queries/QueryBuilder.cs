using nORM.Connections;
using nORM.Models;
using nORM.Models.Properties;

namespace nORM.Builders.Queries;

/// <summary>
/// Base abstract class for all query builders in the nORM framework.
/// </summary>
/// <remarks>
/// Provides common functionality and factory methods for creating
/// provider-specific query builders.
/// </remarks>
public abstract class QueryBuilder : IQueryBuilder
{
    /// <summary>
    /// Generates a query to create a collection (table) for the specified entity type if it doesn't exist.
    /// </summary>
    /// <typeparam name="T">The entity type for which to create a collection.</typeparam>
    /// <returns>Execution properties containing the query to create the collection.</returns>
    public abstract IExecutionProperties GetCreateCollectionQuery<T>() where T : NormEntity;

    /// <summary>
    /// Factory method that returns the appropriate query builder for the specified database provider.
    /// </summary>
    /// <param name="type">The type of database provider.</param>
    /// <returns>A query builder implementation for the specified provider.</returns>
    /// <exception cref="NotImplementedException">Thrown if there is no query builder implemented for the specified provider.</exception>
    public static IQueryBuilder GetQueryBuilderForProvider(DatabaseProviderType type)
    {
        return type switch
        {
            DatabaseProviderType.Sqlite => SqlQueryBuilder.Sqlite,
            DatabaseProviderType.MySql => SqlQueryBuilder.MySql,
            _ => throw new NotImplementedException($"No query builder implemented for {type}")
        };
    }
}