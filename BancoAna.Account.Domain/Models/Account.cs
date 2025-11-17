namespace BancoAna.Account.Domain.Models;

public class Account
{
    public int Id { get; set; }
    public string NumeroConta { get; set; } = string.Empty;
    public string NomeTitular { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
