namespace BancoAna.Account.Domain.Models;

public class Movement
{
    public int Id { get; set; }
    public string RequisicaoId { get; set; } = string.Empty;
    public string ContaNumero { get; set; } = string.Empty;
    public char Tipo { get; set; } // 'C' ou 'D'
    public decimal Valor { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
