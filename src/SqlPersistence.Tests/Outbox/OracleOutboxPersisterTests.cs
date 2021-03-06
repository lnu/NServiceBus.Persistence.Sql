using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class OracleOutboxPersisterTests : OutboxPersisterTests
{
    public OracleOutboxPersisterTests() : base(BuildSqlDialect.Oracle, null)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return OracleConnectionBuilder.Build;
    }

    protected override string GetTablePrefix()
    {
        return "OUTBOX PERSISTER";
    }

    protected override string GetTableSuffix()
    {
        return "_OD";
    }

    protected override string BuildOperationsFromMessageIdCommand(string messageId)
    {
        return $@"select Operations
from ""{GetTablePrefix()}{GetTableSuffix()}""
where MessageId = '{messageId}'";
    }
}