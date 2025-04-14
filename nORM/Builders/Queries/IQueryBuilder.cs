using nORM.Models;
using nORM.Models.Properties;

namespace nORM.Builders.Queries;

/// <summary>
/// Defines the contract for database query builders in the nORM framework.
/// </summary>
/// <remarks>
/// Query builders are responsible for generating database-specific queries
/// while maintaining a consistent interface across different database providers.
/// </remarks>
public interface IQueryBuilder
{
    /// <summary>
    /// Generates a query to create a collection (table) for the specified entity type if it doesn't exist.
    /// </summary>
    /// <typeparam name="T">The entity type for which to create a collection.</typeparam>
    /// <returns>Execution properties containing the query to create the collection.</returns>
    public IExecutionProperties GetCreateCollectionQuery<T>() where T : NormEntity;
}