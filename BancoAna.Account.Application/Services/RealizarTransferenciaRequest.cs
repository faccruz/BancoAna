using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BancoAna.Account.Application.Services
{
    public class RealizarTransferenciaRequest
    {
        public string RequisicaoId { get; set; } = string.Empty;
        public string NumeroOrigem { get; set; } = string.Empty;
        public string NumeroDestino { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }
}
