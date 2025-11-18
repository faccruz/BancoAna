using System.ComponentModel.DataAnnotations;

namespace BancoAna.Account.Api.DTOs
{
    public class MovementRequest
    {
        [Required]
        public string RequisicaoId { get; set; } = string.Empty;

        // agora o cliente passa o número da conta (int). Opcional se o token indicar a conta.
        public int? NumeroConta { get; set; }

        [Required]
        [Range(0.01, 999999999)]
        public decimal Valor { get; set; }

        [Required]
        [RegularExpression("C|D")]
        public string Tipo { get; set; } = "C"; // "C" = crédito, "D" = débito
    }
}
