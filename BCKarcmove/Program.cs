using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Web;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;




/*
 * Перед началом работы надо проверить, есть ли 7z или WinRar по путям, прописанным в настройках
 * А так же доступ к каталогам источника и назначения
 * 
 * 
 Удаляем старые архивы из папки назначения
 Создаём архивы с файлами .BAK  , рядом с этими же файлами
 
 Копируем архивы по пути назначения, удаляем архив из назначения, если всё прошло хорошо
 было бы хорошо прикрутить ещё и отправку уведомления по почте

     */

namespace BCKarcmove
{
    class Program
    {
        public static StreamWriter writer = new StreamWriter("logfile.log");
        
        static void Main(string[] args)
        {
            Console.WriteLine("Выполняется архивирование копий БД");
            writer.WriteLine(DateTime.Now + " --- Program started ---");
            if (!SettingsCheck())
            {
                
                exit(false);
            }

            
            string[] filesSource = Directory.GetFiles(Properties.Settings.Default.BackupPath,"*.BAK");
            writer.WriteLine(DateTime.Now + " INFO: Found " + filesSource.Length + " BAK files");

            foreach (string file in filesSource)
            {
                //  Пакуем все найденные бэкапы
                Process.Start("WinRar.exe", "a -ep -ibck " + file + "_" + DateTime.Now.DayOfYear + ".rar" + " " + file).WaitForExit();
                writer.WriteLine(DateTime.Now + " INFO: File " + file + " RARed" );
            }
            writer.WriteLine(DateTime.Now + " INFO: Packing done");

            string[] filesDest = Directory.GetFiles(Properties.Settings.Default.ReservePath, "*.rar");
            writer.WriteLine(DateTime.Now + " INFO: {0} .rar files found in destination folder", filesDest.Length );
            writer.WriteLine(DateTime.Now + " INFO: Starting to delete old archives");
            if (filesDest.Length > 0)
            { // Удаляем в папке назначения файлы старше N дней, где N задаётся в настройках
                foreach (string file in filesDest)
                {
                    if (DateTime.Now.AddHours(2) > File.GetCreationTime(file).AddDays(Properties.Settings.Default.DaysToLive))
                    { // если файлу больше заданного дней
                        writer.WriteLine("");
                        File.Delete(file);
                        writer.WriteLine(DateTime.Now + " INFO: {0}  - deleted", file);
                    }
                }
            }
            writer.WriteLine(DateTime.Now + " INFO: Deleteing done");
            writer.WriteLine(DateTime.Now + " INFO: Starting to move RARs to destination");

            string[] archives = Directory.GetFiles(Properties.Settings.Default.BackupPath, "*.rar");
            Console.WriteLine("Перемещение архивов в место хранения");
            writer.WriteLine(DateTime.Now + " INFO: Found {0} archives", archives.Length);
            foreach (string item in archives)
            {
                string filename = item.Substring(item.LastIndexOf("\\"), item.LastIndexOf("r") - item.LastIndexOf("\\") +1);

                writer.WriteLine(DateTime.Now + " INFO: Moving {0} to destination", filename );
                try
                {
                    File.Move(item, Properties.Settings.Default.ReservePath + filename);
                }
                catch (System.IO.IOException ex)
                {
                    string str = ex.ToString();
                    writer.WriteLine(DateTime.Now + " ERROR: {0} file : {1}", ex.Message, filename);
                    writer.WriteLine(DateTime.Now + " ERROR: Seems program already has ran today");

                    if (File.GetCreationTime(item) >= File.GetCreationTime(Properties.Settings.Default.ReservePath + filename))
                    {
                        File.Delete(Properties.Settings.Default.ReservePath + filename);
                        writer.WriteLine(DateTime.Now + " INFO: File in dest folder is older, removing");
                        File.Move(item, Properties.Settings.Default.ReservePath + filename);
                        writer.WriteLine(DateTime.Now + " INFO: File {0} replaced with new one", filename );
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
        static bool SettingsCheck()
        {
            string bckPath = Properties.Settings.Default.BackupPath;
            string arcPath = Properties.Settings.Default.ReservePath;

            if (!(Directory.Exists(bckPath)))
            {
                //writer.WriteLine("");
                writer.WriteLine(DateTime.Now + " ERROR: Path with backups is not exists");
                writer.WriteLine(DateTime.Now + " Check app.config");
                return false;
            }

            if (!(Directory.Exists(arcPath)))
            {
                writer.WriteLine(DateTime.Now + " ERROR: Path for archives is not exists");
                writer.WriteLine("Check app.config");
                return false;
            }

            if ( ((bckPath).LastIndexOf("\\") != (bckPath.Length - 1)) || ((arcPath).LastIndexOf("\\") != (arcPath.Length - 1)))
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

        static void sendmail(bool flag)
        {
            Console.WriteLine("Отправка отчёта на {0}" , Properties.Settings.Default.MailAddress);
            MailAddress mailadress = new MailAddress(Properties.Settings.Default.MailAddress);
            MailMessage message = new MailMessage(mailadress, mailadress);
            if (flag)
            {
                message.Subject = "Бэкапы готовы";
                message.Body = "Бэкапы успешно упакованы и скопированы";
            }
            else
            {
                message.Subject = "Что то пошло не так...";
                message.Body = "С бэкапами что-то не так, проверь лог файл.";
            }


            message.Attachments.Add(new Attachment("logfile.log"));

            SmtpClient smtp = new SmtpClient(Properties.Settings.Default.Mailserver);
            smtp.Credentials = new NetworkCredential(Properties.Settings.Default.MailAddress, Properties.Settings.Default.Mailpwd);
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
