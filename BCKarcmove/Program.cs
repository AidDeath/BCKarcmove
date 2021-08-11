using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using BCKarcmove.Properties;

/*Запустить программу можно без параметров, с 2 и 3 параметрами
 Без параметров - Все настройки программа берёт из xml файлика рядом
 2 параметра - первый - путь к бэкапам, второй - путь, куда складывать архивы, удялает бэкапы старше того ког-ва дней, которое прописанов  xml
 3 параметра -  то же, что и 2 параметра, но количество дней прописываем тут
 
     
     Функционал:
     Ищет в заданной папке .BAK файлы, пакует их тут же в архивы .rar  и перемещет в заданную папку для архивов.
     В параметре DaysToLive прописывается, за сколько дней хранятся архивы. Т.е. если поставить 3 - то перед перемещением новых архивов, программа удалит
     из папки для архивов все *.rar старше 3 дней

    Также отправляет сообщение с прикреплённым логом на почту, которую можно настроить в xml ке
    Если почта не настроена - просто пропустит этот шаг и закроется

    Пример запуска BCKarcmove.exe E:\BackUps\ \\Resserver\copies\ 5     - Архивы bak файлов создаются на резервном сервере и хранятся 5 дней
                   BCKarcmove.exe E:\BackUps\ \\Resserver\copies\ - аналогично, но хранятся столько дней, сколько прописано в параметре 
                        <setting name="DaysToLive" serializeAs="String">
                                 <value>3</value>
                       в XMLке
                   BCKarcmove.exe   - все параметры берёт из xml файла.
                   

            Добавлен параметр ArchiveDirectly. Если true - то архивы файлов создаются сразу на сервере назначения, false - рядом с файлами бэкапов и потом перемещаются.
    
     */


namespace BCKarcmove
{
    internal class Program
    {
        private static Logger _logger = new Logger();
        public static string BckPath;
        public static string ArcPath;
        public static double DaysToLive;
        public static bool ArchiveDirectly;

        private static void Main(string[] args)
        {
            switch (args.Length)
            {
                case 0:
                {
                    BckPath = Settings.Default.BackupPath;
                    ArcPath = Settings.Default.ReservePath;
                    DaysToLive = Settings.Default.DaysToLive;
                    ArchiveDirectly = Settings.Default.ArchiveDirectly;
                    _logger.WriteToLog("Start without params", EventType.Info);
                    break;
                }
                case 2:
                {
                    BckPath = args[0];
                    ArcPath = args[1];
                    DaysToLive = Settings.Default.DaysToLive;
                    ArchiveDirectly = Settings.Default.ArchiveDirectly;
                    _logger.WriteToLog("Start with 2 params", EventType.Info);
                    break;
                }
                case 3:
                {
                    BckPath = args[0];
                    ArcPath = args[1];
                    DaysToLive = Convert.ToDouble(args[2]);
                    ArchiveDirectly = Settings.Default.ArchiveDirectly;
                    _logger.WriteToLog("Start with 3 params", EventType.Info);
                    break;
                }
                default:
                {
                    _logger.WriteToLog("Params not valid. Allowed 0 2 or 3 params", EventType.Err);
                    Exit(false);
                    break;
                }
            }

            _logger.WriteToLog($"Params initiated: \nBAK path = {BckPath}\nARC path = {ArcPath}\nDaysToLive = {DaysToLive}\nArchiveDirectly = {ArchiveDirectly}", EventType.Info);

            
            
            if (!SettingsCheck()) Exit(false);
            Console.WriteLine("Проверка параметров прошла успешно");

            Console.WriteLine($"Удаление архивов старше {DaysToLive} дней");
            _logger.WriteToLog("Starting to delete old backups", EventType.Info);

            var filesDest = Directory.GetFiles(ArcPath, "*_*??.rar");
            _logger.WriteToLog($"Found {filesDest.Length} archives", EventType.Info);
            foreach (var arch in filesDest)
            {
                var tail = arch.Substring(arch.LastIndexOf("_", StringComparison.Ordinal) + 1);
                var numDay = int.Parse(tail.Substring(0, tail.IndexOf(".", StringComparison.Ordinal)));
                if (numDay + 3 >= DateTime.Today.DayOfYear) continue;
                File.Delete(arch);
                _logger.WriteToLog($"File {arch} removed as too old", EventType.Info);
            }



            Console.WriteLine("Выполняется архивирование копий БД");
            var filesSource = Directory.GetFiles(BckPath, "*.BAK");
            _logger.WriteToLog($"Found {filesSource.Length} .BAK files", EventType.Info);
            Console.WriteLine($"Найдено {filesSource.Length} .BAK файлов");

            if (filesSource.Length > 0)
            {
                foreach (var filePath in filesSource)
                {
                    var backup = new BackupFile
                    {
                        FileName = Path.GetFileName(filePath),
                        FullFileName = filePath,
                        CreatedTime = File.GetCreationTime(filePath),
                        ArchiveDestination = ArchiveDirectly ? ArcPath : BckPath
                    };

                    try
                    {
                        var archFullPath = backup.CreateArch();
                        _logger.WriteToLog($"Created archive {archFullPath}", EventType.Info);

                        if (!ArchiveDirectly)
                        {
                            var arcFullPath = Path.Combine(ArcPath, Path.GetFileName(archFullPath));
                            if (File.Exists(arcFullPath))
                            {
                                File.Delete(arcFullPath);
                                _logger.WriteToLog($"Overwriting archive {arcFullPath}", EventType.Warn);
                            }
                            
                            File.Move(archFullPath, arcFullPath);
                            _logger.WriteToLog($"Archive moved to {arcFullPath}", EventType.Info);

                        }
                    }
                    catch (Exception e)
                    {
                        _logger.WriteToLog(e.GetBaseException().Message, EventType.Err);
                        Exit(false);
                    }
                }
            }

            Exit(true);
        }

        public static void Exit(bool flag)
        {
            _logger.FinishLogger();
            try
            {
                Sendmail(flag);
            }
            catch (Exception e)
            {
                Console.WriteLine("Что то пошло не так при создании архивов");
                Console.WriteLine(e);
                Console.ReadKey();
            }
            Environment.Exit(0);
        }

        private static bool SettingsCheck()
        {
            if (!Directory.Exists(BckPath))
            {
                _logger.WriteToLog("Path with backups is not exists", EventType.Err);
                return false;
            }

            if (!Directory.Exists(ArcPath))
            {
                _logger.WriteToLog("Path for archives is not exists", EventType.Err);
                return false;
            }

            try
            {
                Directory.GetFiles(BckPath);
                Directory.GetFiles(ArcPath);
            }
            catch (Exception ex)
            {
                _logger.WriteToLog(ex.GetBaseException().Message, EventType.Err);
            }


            return true;
        }

        private static void Sendmail(bool flag)
        {
            Console.WriteLine($"Отправка отчёта на {Settings.Default.MailAddress}");
            var mailAddress = new MailAddress(Settings.Default.MailAddress);
            var message = new MailMessage(mailAddress, mailAddress);
            if (flag)
            {
                message.Subject = "Созданы архивы BAK файлов из " + BckPath;
                message.Body = "Бэкапы успешно упакованы и скопированы на " + ArcPath;
            }
            else
            {
                message.Subject = "Что то пошло не так...";
                message.Body = "С бэкапами что-то не так, проверь лог файл.";
            }


            message.Attachments.Add(new Attachment("logfile.log"));

            var smtp = new SmtpClient(Settings.Default.Mailserver)
            {
                Credentials = new NetworkCredential(Settings.Default.MailAddress, Settings.Default.Mailpwd),
                EnableSsl = false
            };

            try
            {
                smtp.Send(message);
            }
            catch (Exception)
            {
                Environment.Exit(0);
            }
        }
    }
}