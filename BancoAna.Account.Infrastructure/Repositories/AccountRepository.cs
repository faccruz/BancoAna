using Dapper;
using BancoAna.Account.Application.Interfaces;
using BancoAna.Account.Domain.Models;
using BancoAna.Account.Infrastructure.Persistence;

namespace BancoAna.Account.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly IDbConnectionFactory _dbFactory;
    public AccountRepository(IDbConnectionFactory dbFactory) => _dbFactory = dbFactory;

    public async Task CreateAsync(Domain.Models.Account account)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"INSERT INTO ContasCorrente (NumeroConta, NomeTitular, Cpf, SenhaHash, Ativo, CriadoEm)
                    VALUES (@NumeroConta, @NomeTitular, @Cpf, @SenhaHash, @Ativo, @CriadoEm)";
        await conn.ExecuteAsync(sql, account);
    }

    public async Task<Domain.Models.Account?> GetByNumeroContaAsync(string numeroConta)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = "SELECT * FROM ContasCorrente WHERE NumeroConta = @numero";
        return await conn.QueryFirstOrDefaultAsync<Domain.Models.Account>(sql, new { numero = numeroConta });
    }

    public async Task<Domain.Models.Account?> GetByCpfAsync(string cpf)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = "SELECT * FROM ContasCorrente WHERE Cpf = @cpf";
        return await conn.QueryFirstOrDefaultAsync<Domain.Models.Account>(sql, new { cpf });
    }

    public async Task AddMovementAsync(Movement mov)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"INSERT INTO Movimentos (RequisicaoId, ContaNumero, Tipo, Valor, CriadoEm)
                    VALUES (@RequisicaoId, @ContaNumero, @Tipo, @Valor, @CriadoEm)";
        await conn.ExecuteAsync(sql, mov);
    }

    public async Task<decimal> GetSaldoAsync(string numeroConta)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"SELECT 
                        COALESCE(SUM(CASE WHEN Tipo = 'C' THEN Valor ELSE 0 END),0)
                        - COALESCE(SUM(CASE WHEN Tipo = 'D' THEN Valor ELSE 0 END),0) AS Saldo
                    FROM Movimentos
                    WHERE ContaNumero = @numero";
        var result = await conn.ExecuteScalarAsync<decimal?>(sql, new { numero = numeroConta });
        return result ?? 0m;
    }

    public async Task<bool> RequisicaoExistsAsync(string requisicaoId)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = "SELECT COUNT(1) FROM Movimentos WHERE RequisicaoId = @req";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { req = requisicaoId });
        return count > 0;
    }

    public async Task EnsureTablesAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "init-db.sql");
        if (!File.Exists(scriptPath))
        {
            var alt = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "init-db.sql");
            scriptPath = File.Exists(alt) ? alt : scriptPath;
        }

        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("init-db.sql não encontrado", scriptPath);

        var sql = await File.ReadAllTextAsync(scriptPath);
        await conn.ExecuteAsync(sql);
    }
}
