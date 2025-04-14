using nORM.Connections;
using nORM.Models.Properties;

namespace nORM.Builders.Queries;

/// <summary>
/// SQLite-specific implementation of the SQL query builder.
/// </summary>
/// <remarks>
/// Provides specialized query generation for SQLite databases,
/// handling SQLite's unique syntax and features.
/// </remarks>
public class SqliteQueryBuilder : SqlQueryBuilder
{
    /// <summary>
    /// Generates a query to get table information (schema) for SQLite databases.
    /// </summary>
    /// <typeparam name="T">The entity type for which to get table information.</typeparam>
    /// <returns>Execution properties containing the query to retrieve table information.</returns>
    public override IExecutionProperties GetTableInfoQuery<T>()
    {
        var tableInfoQuery = base.GetTableInfoQuery<T>();
        tableInfoQuery.AppendRawText("PRAGMA table_info(" + tableInfoQuery.CollectionContext + ")");
        return tableInfoQuery;
    }

    /// <summary>
    /// Generates a query to insert an entity into a collection with SQLite-specific syntax.
    /// </summary>
    /// <typeparam name="T">The entity type to insert.</typeparam>
    /// <param name="entity">The entity instance to insert.</param>
    /// <returns>Execution properties containing the SQLite INSERT query with last row ID retrieval.</returns>
    public override IExecutionProperties GetInsertQuery<T>(T entity)
    {
        var query = base.GetInsertQuery(entity);
        query.AppendRawText("SELECT last_insert_rowid();");

        return query;
    }

    /// <summary>
    /// Generates a query to delete an entity from a collection with SQLite-specific syntax.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="entity">The entity instance to delete.</param>
    /// <returns>Execution properties containing the SQLite DELETE query with RETURNING clause.</returns>
    public override IExecutionProperties GetDeleteQuery<T>(T entity)
    {
        var query = base.GetDeleteQuery(entity);
        return new SqlExecutionProperties(query.Query.Replace(";", " RETURNING *;"), query.CollectionContext);
    }
}