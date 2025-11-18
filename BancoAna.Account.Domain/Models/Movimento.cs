namespace BancoAna.Account.Domain.Models;

public class Movimento
{
    public string IdMovimento { get; set; } = Guid.NewGuid().ToString();
    public string IdContaCorrente { get; set; } = string.Empty;
    public string DataMovimento { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");
    public string TipoMovimento { get; set; } = string.Empty; // "C" ou "D"
    public decimal Valor { get; set; }
}
