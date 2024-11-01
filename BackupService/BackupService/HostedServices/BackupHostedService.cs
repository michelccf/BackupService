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
using Microsoft.Win32;


namespace BackupService.HostedServices
{
    public class BackupHostedService : BackgroundService
    {
        public const string JsonPath = "C:\\ProgramData\\BackupManager\\JsonConfig.json";
        private int Timer = 0;
        private readonly ILogger<BackupHostedService> _logger;
        private string KeyName = "SOFTWARE\\BackupManager";


        private Task _executingTask;
        private CancellationTokenSource _cts;

        public BackupHostedService(ILogger<BackupHostedService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando StartAsync");
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
             Worker(_cts.Token);
            //return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
        }

        private async void Worker(CancellationToken stoppingToken)
        {
            
            _logger.LogInformation("Iniciando serviço" + Environment.NewLine);
            JsonConfig config = null;
            while (true)
            {
                try
                {
                    DateTime? lastBackup = GetRegistryKey();
                    config = DesserializerJsonConfig(lastBackup);
                    _logger.LogInformation("Json desserializado");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Impossivel desserializar objeto.");
                    GenerateLogFile(ex.Message);
                }

                Thread.Sleep(Timer > 0 ? Timer : 1000);

                try
                {
                    if (config != null && config.Games != null && config.Games.Count > 0)
                    {
                        for (int i = 0; i < config.Games.Count; i++)
                        {
                            string copyPath = $"{config.Games[i].Path}";
                            string finalBackupPath = $"{config.Games[i].Pathbackup}\\{config.Games[i].Path.Split('\\').LastOrDefault()}";
                            CopyFiles(copyPath, finalBackupPath);
                        }
                        CreateOrSetValueRegistryKey();
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
            }
        }

        private void CreateOrSetValueRegistryKey()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyName))
            {
                if (key != null)
                {
                    key.SetValue("LastBackup", DateTime.Now.ToString());
                }
            }
        }

        private DateTime? GetRegistryKey()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyName))
            {
                if (key != null)
                {
                    DateTime lastBackup = Convert.ToDateTime(key.GetValue("LastBackup"));
                    return lastBackup;
                }
                return null;
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

        private JsonConfig DesserializerJsonConfig(DateTime? lastBackup)
        {
            if (File.Exists(JsonPath))
            {
                JsonConfig objeto = null;
                try
                {
                    string json = File.ReadAllText(JsonPath);
                    objeto = new JsonConfig();
                    objeto = JsonConvert.DeserializeObject<JsonConfig>(json);

                    if (lastBackup == null || lastBackup == DateTime.MinValue)
                        CalculateTimer(objeto);
                    else
                        DefineTimer(objeto, Convert.ToDateTime(lastBackup));
                }
                catch (Exception ex)
                {
                    File.WriteAllText("C:\\Users\\miche\\Documents\\Meus Services\\Erros\\BackupService.txt", $"Exception no json: {ex.Message}");
                }
                return objeto;
            }

            return null;

        }

        private void DefineTimer(JsonConfig config, DateTime lastBackup)
        {
            if (config.Horas)
            {
                DateTime NextBackup = lastBackup.AddHours(config.Tempo);
                TimeSpan totalHours = NextBackup - DateTime.Now;
                Timer = totalHours.Microseconds < 0 ? 0 : Math.Abs(Convert.ToInt32(totalHours.TotalMilliseconds));
            }
            if (config.Minutos)
            {
                DateTime NextBackup = lastBackup.AddMinutes(config.Tempo);
                TimeSpan totalHours = NextBackup - DateTime.Now;
                Timer = totalHours.Microseconds < 0 ? 0 : Math.Abs(Convert.ToInt32(totalHours.TotalMilliseconds));
            }
            if (config.Segundos)
            {
                DateTime NextBackup = lastBackup.AddSeconds(config.Tempo);
                TimeSpan totalHours = NextBackup - DateTime.Now;
                Timer = totalHours.Microseconds < 0 ? 0 : Math.Abs(Convert.ToInt32(totalHours.TotalMilliseconds));
            }
               
        }

        private void CalculateTimer(JsonConfig? objeto)
        {
            int Millisecond = 1000;
            int DefinedTime = objeto.Tempo;
            if (objeto.Horas)
            {
                int Hour = 3600;
                Timer = (DefinedTime * Hour) * Millisecond;
            }
            else if (objeto.Minutos)
            {
                int Minute = 60;
                Timer = (DefinedTime * Minute) * Millisecond;
            }
            else
            {
                Timer = DefinedTime * Millisecond;
            }
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
