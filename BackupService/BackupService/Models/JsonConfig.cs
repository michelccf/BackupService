using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupService.Models
{
    public class JsonConfig
    {
        public bool Horas { get; set; }
        public bool Minutos { get; set; }
        public bool Segundos { get; set; }
        public int Tempo { get; set; }
        public List<Game> Games { get; set; }
    }
}
