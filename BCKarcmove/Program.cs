using System;
using System.IO;
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

        Добавлен параметр типа архивации  0  - Вызывает установленный в системе WinRAR, 1 - встроенный в программу архиватор
          <setting name="ArchiverType" serializeAs="String">
            <value>0</value>
          </setting>
    
        Лучше всего прописывать вызов програмы в задании на sql-сервере, чтобы по завершении создания бэкапов - начиналась архивация
        Программа не следит за BAK файлами и не удаляет старые! Это сделано, чтобы админ сам следил за удалением орагиналов бэкапов (Я делаю это в плане обслеживания на sql сервере)
     */


namespace BCKarcmove
{
    internal class Program
    {
        private static readonly Logger Logger = new Logger();
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
                    Logger.WriteToLog("Start without params", EventType.Info);
                    break;
                }
                case 2:
                {
                    BckPath = args[0];
                    ArcPath = args[1];
                    DaysToLive = Settings.Default.DaysToLive;
                    ArchiveDirectly = Settings.Default.ArchiveDirectly;
                    Logger.WriteToLog("Start with 2 params", EventType.Info);
                    break;
                }
                case 3:
                {
                    BckPath = args[0];
                    ArcPath = args[1];
                    DaysToLive = Convert.ToInt32(args[2]);
                    ArchiveDirectly = Settings.Default.ArchiveDirectly;
                    Logger.WriteToLog("Start with 3 params", EventType.Info);
                    break;
                }
                default:
                {
                    Logger.WriteToLog("Params not valid. Allowed 0 2 or 3 params", EventType.Err);
                    Exit(false);
                    break;
                }
            }

            Logger.WriteToLog($"Params initiated: \nBAK path = {BckPath}\nARC path = {ArcPath}\nDaysToLive = {DaysToLive}\nArchiveDirectly = {ArchiveDirectly}", EventType.Info);
            
            
            if (!SettingsCheck()) Exit(false);
            Console.WriteLine("Проверка параметров прошла успешно");

            Console.WriteLine($"Удаление архивов старше {DaysToLive} дней");
            Logger.WriteToLog("Starting to delete old backups", EventType.Info);

            var filesDest = Directory.GetFiles(ArcPath, "*_*??.rar");
            Logger.WriteToLog($"Found {filesDest.Length} archives", EventType.Info);
            foreach (var arch in filesDest)
            {
                var tail = arch.Substring(arch.LastIndexOf("_", StringComparison.Ordinal) + 1);
                var numDay = int.Parse(tail.Substring(0, tail.IndexOf(".", StringComparison.Ordinal)));
                if (numDay + DaysToLive > DateTime.Today.DayOfYear) continue;
                File.Delete(arch);
                Logger.WriteToLog($"File {arch} removed as too old", EventType.Info);
            }



            Console.WriteLine("Выполняется архивирование копий БД");
            var filesSource = Directory.GetFiles(BckPath, "*.BAK");
            Logger.WriteToLog($"Found {filesSource.Length} .BAK files", EventType.Info);
            Console.WriteLine($"Найдено {filesSource.Length} .BAK файлов");

            if (filesSource.Length > 0)
            {
                foreach (var filePath in filesSource)
                {
                    var backup = new BackupFile(filePath, ArchiveDirectly ? ArcPath : null);

                    try
                    {
                        var archFullPath = backup.CreateArch();
                        Console.WriteLine($"Создан архив {Path.GetFileName(archFullPath)}");
                        Logger.WriteToLog($"Created archive {archFullPath}", EventType.Info);

                        if (!ArchiveDirectly)
                        {
                            var arcFullPath = Path.Combine(ArcPath, Path.GetFileName(archFullPath));
                            if (File.Exists(arcFullPath))
                            {
                                File.Delete(arcFullPath);
                                Console.WriteLine($"Файл уже существовал, перезаписываем");
                                Logger.WriteToLog($"Overwriting archive {arcFullPath}", EventType.Warn);
                            }
                            
                            File.Move(archFullPath, arcFullPath);
                            Console.WriteLine($"Перемещён в {arcFullPath}");
                            Logger.WriteToLog($"Archive moved to {arcFullPath}", EventType.Info);

                        }
                    }
                    catch (Exception e)
                    {
                        Logger.WriteToLog(e.GetBaseException().Message, EventType.Err);
                        Exit(false);
                    }
                }
            }

            Exit(true);
        }

        public static void Exit(bool flag)
        {
            if (flag)
            {
                Console.WriteLine("Успешное завершение программы");
                Logger.WriteToLog("Programm finished succesfully", EventType.Info);
            }
            else
            {
                Console.WriteLine("Аварийное завершение программы");
                Logger.WriteToLog("Program routine failed!", EventType.Err);
            }
            
            Logger.FinishLogger();
            try
            {
                Mailer.SendMail(flag);
            }
            catch (Exception e)
            {
                Console.WriteLine("Что то пошло не так при отправке почты");
                Console.WriteLine(e);
                Console.ReadKey();
            }
            Environment.Exit(flag ? 0 : -1);
        }

        private static bool SettingsCheck()
        {
            if (!Directory.Exists(BckPath))
            {
                Logger.WriteToLog("Path with backups is not exists", EventType.Err);
                return false;
            }

            if (!Directory.Exists(ArcPath))
            {
                Logger.WriteToLog("Path for archives is not exists", EventType.Err);
                return false;
            }

            try
            {
                Directory.GetFiles(BckPath);
                Directory.GetFiles(ArcPath);
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(ex.GetBaseException().Message, EventType.Err);
            }

            return true;
        }
    }
}