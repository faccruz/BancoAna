using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BancoAna.Account.Domain.Models
{
    public class Tarifa
    {
        public string IdTarifa { get; set; } = Guid.NewGuid().ToString();
        public string IdContaCorrente { get; set; } = string.Empty;
        public string DataMovimento { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");
        public decimal Valor { get; set; }
    }
}
