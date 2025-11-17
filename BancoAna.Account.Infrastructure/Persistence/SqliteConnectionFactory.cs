using System.Data;
using Microsoft.Data.Sqlite;

namespace BancoAna.Account.Infrastructure.Persistence;

public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    public SqliteConnectionFactory(string connectionString) => _connectionString = connectionString;
    public IDbConnection CreateConnection() => new SqliteConnection(_connectionString);
}
