using System.ComponentModel.DataAnnotations;

namespace BancoAna.Account.Api.DTOs
{
    public class LoginRequest
    {
        [Required]
        public int Numero { get; set; }

        [Required]
        public string Senha { get; set; } = string.Empty;
    }
}
