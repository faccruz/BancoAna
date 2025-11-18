using System.ComponentModel.DataAnnotations;

namespace BancoAna.Account.Api.DTOs
{
    public class TransferRequest
    {
        [Required]
        public string RequisicaoId { get; set; } = string.Empty;

        [Required]
        public int ContaOrigem { get; set; }

        [Required]
        public int ContaDestino { get; set; }

        [Required]
        [Range(0.01, 999999)]
        public decimal Valor { get; set; }
    }
}
