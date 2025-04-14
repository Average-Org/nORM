using System.Text;
using nORM.Connections;
using nORM.Models.Properties;

namespace nORM.Builders.Queries;

/// <summary>
/// MySQL-specific implementation of the SQL query builder.
/// </summary>
/// <remarks>
/// Provides specialized query generation for MySQL databases,
/// handling MySQL's unique syntax and features.
/// </remarks>
public class MySqlQueryBuilder : SqlQueryBuilder
{
    /// <summary>
    /// Gets the database provider type, which is MySQL for this implementation.
    /// </summary>
    public override DatabaseProviderType DatabaseProviderType { get; } = DatabaseProviderType.MySql;
    
    /// <summary>
    /// Generates a query to create a collection (table) for MySQL with proper syntax.
    /// </summary>
    /// <typeparam name="T">The entity type for which to create a collection.</typeparam>
    /// <returns>Execution properties containing the MySQL CREATE TABLE query.</returns>
    public override IExecutionProperties GetCreateCollectionQuery<T>()
    {
        var sqlite = base.GetCreateCollectionQuery<T>();
        var newQuery = sqlite.Query.ToString().Replace("AUTOINCREMENT", "AUTO_INCREMENT");

        return new SqlExecutionProperties(new StringBuilder(newQuery), sqlite.CollectionContext);
    }

    /// <summary>
    /// Generates a query to get table information (schema) for MySQL databases.
    /// </summary>
    /// <typeparam name="T">The entity type for which to get table information.</typeparam>
    /// <returns>Execution properties containing the query to retrieve table information.</returns>
    public override IExecutionProperties GetTableInfoQuery<T>()
    {
        var tableInfoQuery = base.GetTableInfoQuery<T>();
        tableInfoQuery.AppendRawText($"""
                                      SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT, COLUMN_KEY, EXTRA
                                      FROM INFORMATION_SCHEMA.COLUMNS
                                      WHERE TABLE_NAME = '{tableInfoQuery.CollectionContext}'
                                      """);
        return tableInfoQuery;
    }
    
    /// <summary>
    /// Generates a query to insert an entity into a collection with MySQL-specific syntax.
    /// </summary>
    /// <typeparam name="T">The entity type to insert.</typeparam>
    /// <param name="entity">The entity instance to insert.</param>
    /// <returns>Execution properties containing the MySQL INSERT query with last inserted ID retrieval.</returns>
    public override IExecutionProperties GetInsertQuery<T>(T entity)
    {
        var query = base.GetInsertQuery(entity);
        query.AppendRawText("SELECT LAST_INSERT_ID();");

        return query;
    }

    /// <summary>
    /// Generates a query to delete an entity from a collection with MySQL-specific syntax.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="entity">The entity instance to delete.</param>
    /// <returns>Execution properties containing the MySQL DELETE query with row count retrieval.</returns>
    public override IExecutionProperties GetDeleteQuery<T>(T entity)
    {
        return base.GetDeleteQuery(entity).AppendRawText(" SELECT ROW_COUNT();");
    }
}