using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BancoAna.Account.Application.Services
{
    public class RealizarTransferenciaResult
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public string? NumeroOrigem { get; set; }
        public string? NumeroDestino { get; set; }
        public decimal Valor { get; set; }
    }
}
