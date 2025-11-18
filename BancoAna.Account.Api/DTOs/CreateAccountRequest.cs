using System.ComponentModel.DataAnnotations;

namespace BancoAna.Account.Api.DTOs
{
    public class CreateAccountRequest
    {
        [Required]
        public string Nome { get; set; } = string.Empty;

        [Required]
        public string Senha { get; set; } = string.Empty;

        [Required]
        public string Cpf { get; set; } = string.Empty;
    }
}
