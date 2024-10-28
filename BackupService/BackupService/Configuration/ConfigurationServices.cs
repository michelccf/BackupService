using BackupService.HostedServices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupService.Configuration
{
    public class ConfigurationServices
    {
        public ConfigurationServices(IServiceCollection service) 
        {
            service.AddHostedService<BackupHostedService>();
        }
    }
}
