using System.Data;
using System.Linq.Expressions;

namespace nORM.Models.Context;

/// <summary>
/// Defines operations for working with a collection of entities in a database.
/// </summary>
/// <typeparam name="T">The entity type that this collection context manages.</typeparam>
/// <remarks>
/// This interface provides a consistent API for CRUD operations on database collections
/// regardless of the underlying database provider.
/// </remarks>
public interface ICollectionContext<T> where T : NormEntity
{
    /// <summary>
    /// Inserts a new entity into the collection.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <returns>The inserted entity with any database-generated values (like auto-incremented IDs) populated.</returns>
    public T Insert(T entity);
    
    /// <summary>
    /// Inserts a new entity into the collection using the specified transaction.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <param name="transaction">The transaction in which to perform the insert.</param>
    /// <returns>The inserted entity with any database-generated values (like auto-incremented IDs) populated.</returns>
    public T Insert(T entity, IDbTransaction transaction);
    
    /// <summary>
    /// Removes an entity from the collection.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <returns>true if the entity was successfully removed; otherwise, false.</returns>
    public bool Remove(T entity);
    
    /// <summary>
    /// Removes all entities from the collection.
    /// </summary>
    /// <returns>true if the operation was successful; otherwise, false.</returns>
    public bool Truncate();
    
    /// <summary>
    /// Begins a new transaction for operations on this collection.
    /// </summary>
    /// <returns>A new database transaction.</returns>
    public IDbTransaction BeginTransaction();
    
    /// <summary>
    /// Finds a single entity in the collection that matches the specified predicate.
    /// </summary>
    /// <param name="predicate">A lambda expression that defines the conditions to match.</param>
    /// <returns>The matching entity, or null if no match is found.</returns>
    public T? FindOne(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Inserts multiple entities into the collection as a batch operation.
    /// </summary>
    /// <param name="entities">The collection of entities to insert.</param>
    /// <returns>The collection of inserted entities with any database-generated values populated.</returns>
    public IEnumerable<T> InsertMany(IEnumerable<T> entities);
}