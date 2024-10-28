using BackupService.Configuration;
using BackupService.HostedServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackupService
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                File.WriteAllText("C:\\Users\\miche\\Documents\\Meus Services\\Erros\\BackupService.txt", "Iniciou");
                IHost host = Host.CreateDefaultBuilder(args)
                .UseWindowsService(options => { options.ServiceName = "BackupService2"; })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<BackupHostedService>();
                }).Build();
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                File.WriteAllText("C:\\Users\\miche\\Documents\\Meus Services\\Erros\\BackupService.txt", ex.ToString());
            }
        }

   
    }
}
