using BackupService.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using Microsoft.Extensions.Logging;


namespace BackupService.HostedServices
{
    public class BackupHostedService : BackgroundService
    {
        public const string JsonPath = "C:\\ProgramData\\BackupManager\\JsonConfig.json";
        private readonly ILogger<BackupHostedService> _logger;

        private Task _executingTask;
        private CancellationTokenSource _cts;

        public BackupHostedService(ILogger<BackupHostedService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            File.WriteAllText("C:\\Users\\miche\\Documents\\Meus Services\\Erros\\BackupService.txt", "Entrou no execute");
            _logger.LogInformation("Iniciando StartAsync");
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
             Worker(_cts.Token);
            //return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
        }

        private async void Worker(CancellationToken stoppingToken)
        {
            GenerateLogFile("Entrou no Worker" + Environment.NewLine);
            _logger.LogInformation("Iniciando serviço" + Environment.NewLine);
            string BackupPath = string.Empty;
            JsonConfig config = null;
            while (true)
            {
                try
                {
                    config = DesserializerJsonConfig();
                    GenerateLogFile("Desserializou" + Environment.NewLine);
                    _logger.LogInformation("Json desserializado");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Impossivel desserializar objeto.");
                    GenerateLogFile(ex.Message);
                }

                BackupPath = config?.BackupPath;
                try
                {
                    if (config != null && config.Games != null && config.Games.Count > 0)
                    {
                        GenerateLogFile("Entrou no if" + Environment.NewLine);
                        for (int i = 0; i < config.Games.Count; i++)
                        {
                            GenerateLogFile("Achou Elementos" + Environment.NewLine);
                            string copyPath = $"{config.Games[i].Path}";
                            string finalBackupPath = $"{BackupPath}\\{config.Games[i].Name}";
                            CopyFiles(copyPath, finalBackupPath);
                        }
                    }
                    else 
                    {
                        GenerateLogFile("Caiu no else" + Environment.NewLine);
                    }
                    _logger.LogInformation("Arquivos copiados.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    GenerateLogFile(ex.Message + Environment.NewLine);
                }
                Thread.Sleep(5000);
            }
        }

        private void GenerateLogFile(string Message)
        {
            File.AppendAllText("C:\\Users\\miche\\Documents\\Meus Services\\Erros\\BackupService.txt", Message);
        }

        private void CopyFiles(string PathOrign, string BackupPath)
        {
            CloneDirectory(PathOrign, BackupPath);
        }

        public void CloneDirectory(string PathOrign, string BackupPath)
        {
            if (!Directory.Exists(PathOrign))
            {
                throw new DirectoryNotFoundException($"Diretório de origem não encontrado: {PathOrign}");
            }

            // Se o diretório de destino não existir, cria-o
            if (!Directory.Exists(BackupPath))
            {
                Directory.CreateDirectory(BackupPath);
            }

            foreach (string file in Directory.GetFiles(PathOrign))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(BackupPath, fileName);
                File.Copy(file, destFile, true);
            }

            // Copia todos os subdiretórios
            foreach (string subDir in Directory.GetDirectories(PathOrign))
            {
                string subDirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(BackupPath, subDirName);
                CloneDirectory(subDir, destSubDir);
            }
        }

        private JsonConfig DesserializerJsonConfig()
        {
            GenerateLogFile("Metodo Serializer" + Environment.NewLine);
            
            if (File.Exists(JsonPath))
            {
                JsonConfig objeto = null;
                try
                {
                    string json = File.ReadAllText(JsonPath);
                    GenerateLogFile(json);
                    objeto = new JsonConfig();
                    objeto = JsonConvert.DeserializeObject<JsonConfig>(json);
                }
                catch (Exception ex)
                {
                    File.WriteAllText("C:\\Users\\miche\\Documents\\Meus Services\\Erros\\BackupService.txt", $"Exception no json: {ex.Message}");
                }
                return objeto;
            }

            return null;

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Parando serviço");

            if (_executingTask == null)
            {
                return;
            }

            try
            {
                _cts.Cancel();
            }
            finally
            {
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
        }
    }
}
