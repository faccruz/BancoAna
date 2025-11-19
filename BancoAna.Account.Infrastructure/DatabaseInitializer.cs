using BancoAna.Account.Application.Interfaces;
using BancoAna.Account.Infrastructure.Persistence;
using Dapper;

namespace BancoAna.Account.Infrastructure
{
    public class DatabaseInitializer
    {
        private readonly IAccountRepository _repo;
        private readonly IDbConnectionFactory _dbFactory;

        public DatabaseInitializer(IAccountRepository repo, IDbConnectionFactory dbFactory) 
        {
            _repo = repo;
            _dbFactory = dbFactory;
        }

        public void EnsureCreated()
        {
            // chama de forma síncrona o método assíncrono (apenas no startup)
            _repo.EnsureTablesAsync().GetAwaiter().GetResult();
        }

        public void ResetDatabase()
        {
            using var conn = _dbFactory.CreateConnection();

            conn.Execute("DELETE FROM movimento;");
            conn.Execute("DELETE FROM transferencia;");
            conn.Execute("DELETE FROM tarifa;");
            conn.Execute("DELETE FROM idempotencia;");
            conn.Execute("DELETE FROM contacorrente;");
        }
    }
}
