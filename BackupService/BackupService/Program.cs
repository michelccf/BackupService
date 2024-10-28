using BackupService.Configuration;
using BackupService.HostedServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackupService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                File.WriteAllText("C:\\Users\\miche\\Documents\\Meus Services\\Erros\\BackupService.txt", ex.ToString());
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    new ConfigurationServices(services);
                }).ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddEventLog();
                    logging.AddEventLog(settings =>
                    {
                        settings.SourceName = "BackupService";
                    });
                })
                .UseWindowsService();
    }
}
