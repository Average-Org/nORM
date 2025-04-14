using System.Data;
using System.Data.Entity.Core;
using System.Data.SQLite;
using nORM.Models.Properties;

namespace nORM.Connections;

public abstract class NormSqlConnection(string connectionString) : BaseNormConnection
{
    private readonly IDbConnection _connection = new SQLiteConnection(connectionString);

    public override IDbConnection GetDbConnection()
    {
        return _connection;
    }

    public override INormConnection Connect()
    {
        try
        {
            GetDbConnection().Open();
            return this;
        }
        catch (Exception ex)
        {
            //TODO: Replace with logging abstraction
            throw;
        }
    }

    public override IDataReader ExecuteQuery(IExecutionProperties properties)
    {
        if (properties is not SqlExecutionProperties { } sqlQuery)
        {
            throw new ProviderIncompatibleException(
                "You attempted to pass execution properties that do not match the connection type");
        }

        var command = GetDbConnection().CreateCommand();
        command.CommandText = sqlQuery.ToString();
        return command.ExecuteReader();
    }

    public override void ExecuteNonQuery(IExecutionProperties executionProperties)
    {
        if (executionProperties is not SqlExecutionProperties { } sqlQuery)
        {
            throw new ProviderIncompatibleException(
                "You attempted to pass execution properties that do not match the connection type");
        }

        var command = GetDbConnection().CreateCommand();
        command.CommandText = sqlQuery.ToString();
        command.ExecuteNonQuery();
    }

    public override List<Dictionary<string, object>> Query(IExecutionProperties executionProperties)
    {
        if (executionProperties is not SqlExecutionProperties { } sqlQuery)
        {
            throw new ProviderIncompatibleException(
                "You attempted to pass execution properties that do not match the connection type");
        }

        var command = GetDbConnection().CreateCommand();
        command.CommandText = sqlQuery.ToString();

        var reader = command.ExecuteReader();

        var result = new List<Dictionary<string, object>>();

        while (reader.Read())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }

            result.Add(row);
        }

        reader.Close();

        return result;
    }

    public override object ExecuteScalar(IExecutionProperties executionProperties)
    {
        if (executionProperties is not SqlExecutionProperties { } sqlQuery)
        {
            throw new ProviderIncompatibleException(
                "You attempted to pass execution properties that do not match the connection type");
        }

        var command = GetDbConnection().CreateCommand();
        command.CommandText = sqlQuery.ToString();
        return command.ExecuteScalar() ?? DBNull.Value;
    }
    
    public override IDbTransaction BeginTransaction()
    {
        return GetDbConnection().BeginTransaction();
    }
    
    public override void Dispose()
    {
        GetDbConnection().Close();
        GetDbConnection().Dispose();
    }
}