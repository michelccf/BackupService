using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupService.Models
{
    public class JsonConfig
    {
        public string BackupPath { get; set; }
        public List<Game> Games { get; set; }
    }
}
