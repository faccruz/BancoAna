namespace BancoAna.Account.Api.DTOs;

public class CreateAccountRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string NumeroConta { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class MovementRequest
{
    public string RequisicaoId { get; set; } = string.Empty;
    public string? NumeroConta { get; set; }
    public decimal Valor { get; set; }
    public char Tipo { get; set; } // 'C' or 'D'
}

public class BalanceResponse
{
    public string NumeroConta { get; set; } = string.Empty;
    public decimal Saldo { get; set; }
    public DateTime DataHora { get; set; }
}
