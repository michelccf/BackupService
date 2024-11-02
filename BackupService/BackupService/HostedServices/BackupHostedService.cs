using BackupService.Models;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.IO;


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

                //Thread.Sleep(Timer > 0 ? Timer : 1000);
                await Task.Delay((Timer > 0 ? Timer : 1000));
                GenerateLogFile("Iniciou Backup" + Environment.NewLine);

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
                        GenerateLogFile("Caiu no else");
                    }
                    _logger.LogInformation("Arquivos copiados.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    GenerateLogFile(ex.Message );
                }
            }
        }

        private void CreateOrSetValueRegistryKey()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(KeyName))
                {
                    if (key != null)
                    {
                        key.SetValue("LastBackup", DateTime.Now.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                GenerateLogFile($"Erro ao criar Key: {ex.Message}");
            }
        }

        private DateTime? GetRegistryKey()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(KeyName.Replace("SOFTEARE", "WOW6432Node")))
            {
                try
                {
                    if (key != null)
                    {
                        DateTime lastBackup = Convert.ToDateTime(key.GetValue("LastBackup"));
                        GenerateLogFile($"RegistryKey Obteve lastBackup: {lastBackup}");
                        return lastBackup;
                    }
                }
                catch (Exception ex)
                {
                    GenerateLogFile($"Erro ao obter Key: {ex.Message}");
                }
                return null;
            }
        }

        private void GenerateLogFile(string Message)
        {
            string ErrorDirectory = "C:\\ProgramData\\BackupManager\\Erros\\";
            string ErrorFile = "C:\\ProgramData\\BackupManager\\Erros\\BackupService.txt";
            if (!File.Exists("ErrorDirectory"))
            {
                DirectoryInfo info = Directory.CreateDirectory("C:\\ProgramData\\BackupManager\\Erros\\");
                DefineSecurityFolder(info);
            }

            if (!File.Exists(ErrorFile))
            {
                using (FileStream fs = File.Create(ErrorFile))
                {
                }
            }
                File.AppendAllText("C:\\ProgramData\\BackupManager\\Erros\\BackupService.txt", Message + Environment.NewLine);
        }

        private void DefineSecurityFile(string errorFile)
        {
            try
            {
               FileInfo info = new FileInfo(errorFile);

                var file = FileSystemAclExtensions.GetAccessControl(info);

                // Obter o usuário atual
                WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
                SecurityIdentifier userSid = currentUser.User;

                // Definir as permissões
                FileSystemAccessRule accessRule = new FileSystemAccessRule(
                    userSid,
                    FileSystemRights.FullControl,
                    InheritanceFlags.None,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                // Adicionar a regra de acesso
                file.AddAccessRule(accessRule);

                // Aplicar as permissões ao arquivo
                FileSystemAclExtensions.SetAccessControl(info, file);
            }
            catch (Exception ex)
            {
                GenerateLogFile($"Erro ao definir segurança do arquivo de erros: {ex.Message}");
            }
        }

        private void DefineSecurityFolder(DirectoryInfo info)
        {
            try
            {
                // Obter a segurança do diretório
                DirectorySecurity dirSecurity = info.GetAccessControl();

                // Obter o usuário atual
                WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
                SecurityIdentifier userSid = currentUser.User;

                // Definir as permissões
                FileSystemAccessRule accessRule = new FileSystemAccessRule(
                    userSid,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                // Adicionar a regra de acesso
                dirSecurity.AddAccessRule(accessRule);

                // Aplicar as permissões ao diretório
                info.SetAccessControl(dirSecurity);
            }
            catch(Exception ex) 
            {
                GenerateLogFile($"Erro ao definir segurança da pasta de erros: {ex.Message}");
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
                    GenerateLogFile($"Exception no json: {ex.Message}");
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
