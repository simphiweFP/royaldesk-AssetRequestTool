using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace RoyalDesk.Api.Data;

public sealed class SqliteConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public DbConnection CreateConnection() => new SqliteConnection(connectionString);
}
