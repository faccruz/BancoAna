using Dapper;
using BancoAna.Account.Application.Interfaces;
using BancoAna.Account.Domain.Models;
using BancoAna.Account.Infrastructure.Persistence;

namespace BancoAna.Account.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly IDbConnectionFactory _dbFactory;
    public AccountRepository(IDbConnectionFactory dbFactory) => _dbFactory = dbFactory;

    public async Task CriarContaAsync(ContaCorrente conta)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"
            INSERT INTO contacorrente
            (idcontacorrente, numero, nome, ativo, senha, salt, Cpf)
            VALUES
            (@IdContaCorrente, @Numero, @Nome, @Ativo, @Senha, @Salt, @Cpf);
        ";
        await conn.ExecuteAsync(sql, conta);
    }

    public async Task<ContaCorrente?> ObterPorNumeroAsync(int numero)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"SELECT idcontacorrente AS IdContaCorrente, numero AS Numero, nome AS Nome, ativo AS Ativo, senha AS Senha, salt AS Salt, Cpf
                    FROM contacorrente WHERE numero = @numero;";
        return await conn.QueryFirstOrDefaultAsync<ContaCorrente>(sql, new { numero });
    }

    public async Task<ContaCorrente?> ObterPorIdAsync(string idConta)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"SELECT idcontacorrente AS IdContaCorrente, numero AS Numero, nome AS Nome, ativo AS Ativo, senha AS Senha, salt AS Salt, Cpf
                    FROM contacorrente WHERE idcontacorrente = @id;";
        return await conn.QueryFirstOrDefaultAsync<ContaCorrente>(sql, new { id = idConta });
    }

    public async Task<ContaCorrente?> ObterPorCpfAsync(string cpf)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"SELECT idcontacorrente AS IdContaCorrente, numero AS Numero, nome AS Nome, ativo AS Ativo, senha AS Senha, salt AS Salt, Cpf
                    FROM contacorrente WHERE Cpf = @cpf;";
        return await conn.QueryFirstOrDefaultAsync<ContaCorrente>(sql, new { cpf });
    }

    public async Task AdicionarMovimentoAsync(Movimento mov)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"
            INSERT INTO movimento
            (idmovimento, idcontacorrente, datamovimento, tipomovimento, valor)
            VALUES
            (@IdMovimento, @IdContaCorrente, @DataMovimento, @TipoMovimento, @Valor);
        ";
        await conn.ExecuteAsync(sql, mov);
    }

    public async Task<decimal> ObterSaldoAsync(string idConta)
    {
        using var conn = _dbFactory.CreateConnection();

        var sql = @"
        SELECT COALESCE(SUM(
            CASE 
                WHEN UPPER(tipomovimento) = 'C' THEN valor
                WHEN UPPER(tipomovimento) = 'D' THEN -valor
                ELSE 0
            END
        ), 0) AS Saldo
        FROM movimento
        WHERE idcontacorrente = @id;
    ";

        // ExecuteScalarAsync pode retornar double (sqlite) ou decimal; tratar generically
        var obj = await conn.ExecuteScalarAsync<object>(sql, new { id = idConta });
        if (obj == null || obj == DBNull.Value) return 0m;

        // converter com cuidado
        try
        {
            return Convert.ToDecimal(obj);
        }
        catch
        {
            // fallback: tentar double -> decimal
            if (obj is double d) return Convert.ToDecimal(d);
            if (obj is float f) return Convert.ToDecimal(f);
            if (decimal.TryParse(obj.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dec))
                return dec;
            return 0m;
        }
    }

    public async Task<bool> IdempotenciaExisteAsync(string chave)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = "SELECT COUNT(1) FROM idempotencia WHERE chave_idempotencia = @chave;";
        var cnt = await conn.ExecuteScalarAsync<int>(sql, new { chave });
        return cnt > 0;
    }

    public async Task RegistrarIdempotenciaAsync(Idempotencia idem)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"
            INSERT INTO idempotencia (chave_idempotencia, requisicao, resultado)
            VALUES (@ChaveIdempotencia, @Requisicao, @Resultado);
        ";
        await conn.ExecuteAsync(sql, idem);
    }

    public async Task RegistrarTransferenciaAsync(Transferencia tr)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"
            INSERT INTO transferencia
            (idtransferencia, idcontacorrente_origem, idcontacorrente_destino, datamovimento, valor)
            VALUES (@IdTransferencia, @IdContaCorrenteOrigem, @IdContaCorrenteDestino, @DataMovimento, @Valor);
        ";
        await conn.ExecuteAsync(sql, tr);
    }

    public async Task RegistrarTarifaAsync(Tarifa t)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"
            INSERT INTO tarifa
            (idtarifa, idcontacorrente, datamovimento, valor)
            VALUES (@IdTarifa, @IdContaCorrente, @DataMovimento, @Valor);
        ";
        await conn.ExecuteAsync(sql, t);
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
