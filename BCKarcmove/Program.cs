using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
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
    
     */


namespace BCKarcmove
{
    internal class Program
    {
        public static StreamWriter writer = new StreamWriter("logfile.log");
        public static string bckPath;
        public static string arcPath;
        public static double DaysToLive;

        private static void Main(string[] args)
        {
            switch (args.Length)
            {
                case 0:
                {
                    bckPath = Settings.Default.BackupPath;
                    arcPath = Settings.Default.ReservePath;
                    DaysToLive = Settings.Default.DaysToLive;
                    writer.WriteLine(DateTime.Now + " INFO: Start without params");

                    break;
                }
                case 2:
                {
                    bckPath = args[0];
                    arcPath = args[1];
                    DaysToLive = Settings.Default.DaysToLive;
                    writer.WriteLine(DateTime.Now + " INFO: Start with 2 params");
                    break;
                }
                case 3:
                {
                    bckPath = args[0];
                    arcPath = args[1];
                    DaysToLive = Convert.ToDouble(args[2]);
                    writer.WriteLine(DateTime.Now + " INFO: Start with 3 params");
                    break;
                }
                default:
                {
                    writer.WriteLine(DateTime.Now + " ERROR: Params not valid. Allowed 0 2 or 3 params ");
                    exit(false);
                    break;
                }
            }

            writer.WriteLine(DateTime.Now +
                             $" INFO: Params initiated: \nBAK path = {bckPath}\nARC path = {arcPath}\nDaysToLive = {DaysToLive}");
            writer.WriteLine(DateTime.Now + $" INFO: starting host: {Environment.MachineName}");


            Console.WriteLine("Выполняется архивирование копий БД");
            writer.WriteLine(DateTime.Now + " --- Program started ---");


            if (!SettingsCheck()) exit(false);

            var filesSource = Directory.GetFiles(bckPath, "*.BAK");
            writer.WriteLine(DateTime.Now + " INFO: Found " + filesSource.Length + " BAK files");

            foreach (var file in filesSource)
            {
                //  Пакуем все найденные бэкапы
                Process.Start("WinRar.exe", "a -ep -ibck " + file + "_" + DateTime.Now.DayOfYear + ".rar" + " " + file)
                    .WaitForExit();
                writer.WriteLine(DateTime.Now + " INFO: File " + file + " RARed");
            }

            writer.WriteLine(DateTime.Now + " INFO: Packing done");

            var filesDest = Directory.GetFiles(arcPath, "*.rar");
            writer.WriteLine(DateTime.Now + " INFO: {0} .rar files found in destination folder", filesDest.Length);
            writer.WriteLine(DateTime.Now + " INFO: Starting to delete old archives");
            if (filesDest.Length > 0)
                // Удаляем в папке назначения файлы старше N дней, где N задаётся в настройках
                foreach (var file in filesDest)
                    if (DateTime.Now.AddHours(2) > File.GetCreationTime(file).AddDays(DaysToLive))
                    {
                        // если файлу больше заданного дней
                        writer.WriteLine("");
                        File.Delete(file);
                        writer.WriteLine(DateTime.Now + " INFO: {0}  - deleted", file);
                    }

            writer.WriteLine(DateTime.Now + " INFO: Deleteing done");
            writer.WriteLine(DateTime.Now + " INFO: Starting to move RARs to destination");
            writer.WriteLine(DateTime.Now + " INFO: Destination is: {0}", arcPath);

            var archives = Directory.GetFiles(bckPath, "*.rar");
            Console.WriteLine("Перемещение архивов в место хранения");
            writer.WriteLine(DateTime.Now + " INFO: Found {0} archives", archives.Length);
            foreach (var item in archives)
            {
                var filename = item.Substring(item.LastIndexOf("\\"),
                    item.LastIndexOf("r") - item.LastIndexOf("\\") + 1);

                writer.WriteLine(DateTime.Now + " INFO: Moving {0} to destination", filename);
                try
                {
                    File.Move(item, arcPath + filename);
                }
                catch (IOException ex)
                {
                    writer.WriteLine(DateTime.Now + " ERROR: {0} file : {1}", ex.Message, filename);
                    writer.WriteLine(DateTime.Now + " ERROR: Seems program already has ran today");

                    if (File.GetCreationTime(item) >= File.GetCreationTime(arcPath + filename))
                    {
                        File.Delete(arcPath + filename);
                        writer.WriteLine(DateTime.Now + " INFO: File in dest folder is older, removing");
                        File.Move(item, arcPath + filename);
                        writer.WriteLine(DateTime.Now + " INFO: File {0} replaced with new one", filename);
                    }
                }
            }

            writer.WriteLine(DateTime.Now + " INFO: Done moving archives to destination");
            exit(true);
        }

        public static void exit(bool flag)
        {
            writer.WriteLine(DateTime.Now + " --- Program ends now ---");
            writer.Close();

            sendmail(flag);
            Environment.Exit(0);
        }

        private static bool SettingsCheck()
        {
            if (!Directory.Exists(bckPath))
            {
                writer.WriteLine(DateTime.Now + " ERROR: Path with backups is not exists");
                writer.WriteLine(DateTime.Now + " Check app.config");
                return false;
            }

            if (!Directory.Exists(arcPath))
            {
                writer.WriteLine(DateTime.Now + " ERROR: Path for archives is not exists");
                writer.WriteLine("Check app.config");
                return false;
            }

            if (bckPath.LastIndexOf("\\") != bckPath.Length - 1 || arcPath.LastIndexOf("\\") != arcPath.Length - 1)
            {
                writer.WriteLine(DateTime.Now + " ERROR: Path is not ending with \\");
                writer.WriteLine(DateTime.Now + " ERROR: Check app.config");
                return false;
            }

            try
            {
                Directory.GetFiles(bckPath);
                Directory.GetFiles(arcPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                writer.WriteLine(DateTime.Now + " ERROR: Ошибка доступа");
                writer.WriteLine(DateTime.Now + " ERROR: {0}", ex.Message);
                return false;
            }


            return true;
        }

        private static void sendmail(bool flag)
        {
            Console.WriteLine("Отправка отчёта на {0}", Settings.Default.MailAddress);
            var mailadress = new MailAddress(Settings.Default.MailAddress);
            var message = new MailMessage(mailadress, mailadress);
            if (flag)
            {
                message.Subject = "Созданы архивы BAK файлов из " + bckPath;
                message.Body = "Бэкапы успешно упакованы и скопированы на " + arcPath;
            }
            else
            {
                message.Subject = "Что то пошло не так...";
                message.Body = "С бэкапами что-то не так, проверь лог файл.";
            }


            message.Attachments.Add(new Attachment("logfile.log"));

            var smtp = new SmtpClient(Settings.Default.Mailserver);
            smtp.Credentials = new NetworkCredential(Settings.Default.MailAddress, Settings.Default.Mailpwd);
            smtp.EnableSsl = false;
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