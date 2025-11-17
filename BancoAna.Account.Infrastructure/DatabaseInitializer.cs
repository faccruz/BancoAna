using BancoAna.Account.Application.Interfaces;

namespace BancoAna.Account.Infrastructure
{
    public class DatabaseInitializer
    {
        private readonly IAccountRepository _repo;
        public DatabaseInitializer(IAccountRepository repo) => _repo = repo;

        public void EnsureCreated()
        {
            // chama de forma síncrona o método assíncrono (apenas no startup)
            _repo.EnsureTablesAsync().GetAwaiter().GetResult();
        }
    }
}
