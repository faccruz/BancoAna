using BancoAna.Account.Domain.Models;

namespace BancoAna.Account.Application.Interfaces;

public interface IAccountRepository
{
    // contas
    Task CriarContaAsync(ContaCorrente conta);
    Task<ContaCorrente?> ObterPorNumeroAsync(int numero);
    Task<ContaCorrente?> ObterPorIdAsync(string idConta);
    Task<ContaCorrente?> ObterPorCpfAsync(string cpf);

    // movimentos
    Task AdicionarMovimentoAsync(Movimento mov);
    Task<decimal> ObterSaldoAsync(string idConta);

    // idempotencia
    Task<bool> IdempotenciaExisteAsync(string chave);
    Task RegistrarIdempotenciaAsync(Idempotencia idem);

    // transferencias / tarifas
    Task RegistrarTransferenciaAsync(Transferencia tr);
    Task RegistrarTarifaAsync(Tarifa t);

    // inicialização DB
    Task EnsureTablesAsync();
}
