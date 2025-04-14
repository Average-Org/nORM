using nORM.Connections;
using nORM.Models.Properties;

namespace nORM.Builders.Queries;

public class SqliteQueryBuilder : SqlQueryBuilder
{
    public override IExecutionProperties GetTableInfoQuery<T>()
    {
        var tableInfoQuery = base.GetTableInfoQuery<T>();
        tableInfoQuery.AppendRawText("PRAGMA table_info(" + tableInfoQuery.CollectionContext + ")");
        return tableInfoQuery;
    }

    public override IExecutionProperties GetInsertQuery<T>(T entity)
    {
        var query = base.GetInsertQuery(entity);
        query.AppendRawText("SELECT last_insert_rowid();");

        return query;
    }

    public override IExecutionProperties GetDeleteQuery<T>(T entity)
    {
        var query = base.GetDeleteQuery(entity);
        return new SqlExecutionProperties(query.Query.Replace(";", " RETURNING *;"), query.CollectionContext);
    }
}