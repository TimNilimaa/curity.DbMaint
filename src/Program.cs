using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Logging;
using Curity.DbMaint.Models;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

int iterationAmount = 10_000;

var tables = new List<TableInfo>
{
    new("Delegations",
        "select count(*) from delegations WHERE expires < DATEDIFF(s, '1970-01-01 00:00:00', CURRENT_TIMESTAMP)",
        $"DELETE TOP ({iterationAmount}) FROM delegations WHERE expires < DATEDIFF(s, '1970-01-01 00:00:00', CURRENT_TIMESTAMP)"
    ),
    new("Tokens",
        "select count(*) from tokens WHERE expires < DATEDIFF(s, '1970-01-01 00:00:00', CURRENT_TIMESTAMP)",
        $"DELETE TOP ({iterationAmount}) FROM tokens WHERE expires < DATEDIFF(s, '1970-01-01 00:00:00', CURRENT_TIMESTAMP)"
    ),
    new("Nonces",
        "select count(*) from nonces where status = 'used'",
        $"DELETE TOP ({iterationAmount}) FROM nonces WHERE status = 'used';"
    ),
    new("Sessions",
        "select count(*) from sessions WHERE expires < DATEDIFF(s, '1970-01-01 00:00:00', CURRENT_TIMESTAMP)",
        $"DELETE TOP ({iterationAmount}) FROM sessions WHERE expires < DATEDIFF(s, '1970-01-01 00:00:00', CURRENT_TIMESTAMP)"
    ),
};

var connectionString = Environment.GetEnvironmentVariable("ConnectionString_CurityDb")?.ToString();
if (string.IsNullOrWhiteSpace(connectionString))
    throw new NotSupportedException("Connectionstring must be set");

foreach (var table in tables)
{
    await CleanDelegations(logger, iterationAmount, table, connectionString);
}


static async Task CleanDelegations(ILogger<Program> logger, int iterationAmount, TableInfo tableInfo, string? connectionString)
{
    using var connection = new SqlConnection(connectionString);

    var count = await connection.QueryAsync<int>(tableInfo.QueryRows);

    logger.LogInformation("{TableName} has {Rows}", tableInfo.TableName, count.First());

    var executions = count.First() / iterationAmount;

    for (int i = 0; i < executions; i++)
    {
        await connection.ExecuteAsync(tableInfo.DeleteRows, commandTimeout: 10000);
        logger.LogTrace("Deleted {IterationAmount} rows...", iterationAmount);
    }

    count = await connection.QueryAsync<int>(tableInfo.QueryRows);

    if (executions > 1)
        logger.LogInformation("{TableName} now has {Rows}", tableInfo.TableName, count.First());

    await connection.CloseAsync();
}
