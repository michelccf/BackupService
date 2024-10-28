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


namespace BackupService.HostedServices
{
    public class BackupHostedService : IHostedService
    {
        public const string JsonPath = "C:\\Users\\%User%\\AppData\\Local\\BackupManager\\JsonConfig.json";
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(() => Worker());
            return task;
        }

        private void Worker()
        {
            string BackupPath = string.Empty;
            JsonConfig config = null;
            while (true)
            {
                try
                {
                    config = DesserializerJsonConfig();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Impossivel desserializar objeto.");
                }

                BackupPath = config?.BackupPath;

                if (config != null && config.Games != null && config.Games.Count > 0)
                {
                    for (int i = 0; i < config.Games.Count; i++)
                    {
                        string copyPath = $"{config.Games[i].Path}";
                        string finalBackupPath = $"{BackupPath}\\{config.Games[i].Name}";
                        CopyFiles(copyPath, finalBackupPath);
                    }
                }
                Thread.Sleep(120000);
            }
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
            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\').LastOrDefault();
            if (File.Exists(JsonPath.Replace("%User%", userName)))
            {
                string json = File.ReadAllText(JsonPath.Replace("%User%", userName));
                JsonConfig objeto = new JsonConfig();
                objeto = JsonConvert.DeserializeObject<JsonConfig>(json);
                return objeto;
            }

            return null;

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
