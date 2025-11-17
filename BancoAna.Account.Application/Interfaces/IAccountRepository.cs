using BancoAna.Account.Domain.Models;

namespace BancoAna.Account.Application.Interfaces;

public interface IAccountRepository
{
    Task CreateAsync(Domain.Models.Account account);
    Task<Domain.Models.Account?> GetByNumeroContaAsync(string numeroConta);
    Task<Domain.Models.Account?> GetByCpfAsync(string cpf);
    Task AddMovementAsync(Movement mov);
    Task<decimal> GetSaldoAsync(string numeroConta);
    Task<bool> RequisicaoExistsAsync(string requisicaoId);
    Task EnsureTablesAsync();
}
