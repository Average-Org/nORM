using System.Text;
using nORM.Connections;
using nORM.Models.Properties;

namespace nORM.Builders.Queries;

public class MySqlQueryBuilder : SqlQueryBuilder
{
    public override DatabaseProviderType DatabaseProviderType { get; } = DatabaseProviderType.MySql;
    public override IExecutionProperties GetCreateCollectionQuery<T>()
    {
        var sqlite = base.GetCreateCollectionQuery<T>();
        var newQuery = sqlite.Query.ToString().Replace("AUTOINCREMENT", "AUTO_INCREMENT");

        return new SqlExecutionProperties(new StringBuilder(newQuery), sqlite.CollectionContext);
    }

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
    
    public override IExecutionProperties GetInsertQuery<T>(T entity)
    {
        var query = base.GetInsertQuery(entity);
        query.AppendRawText("SELECT LAST_INSERT_ID();");

        return query;
    }

    public override IExecutionProperties GetDeleteQuery<T>(T entity)
    {
        return base.GetDeleteQuery(entity).AppendRawText(" SELECT ROW_COUNT();");
    }
}