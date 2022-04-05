using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using BCKarcmove.Properties;
using System.Drawing;
using System.Linq;

namespace BCKarcmove
{
    public static class Mailer
    {
        public static void SendMail(bool isSuccess, List<ArchiveFile> archivesFiles = null)
        {
            Console.WriteLine($"Отправка отчёта на {Settings.Default.MailAddress}");
            var mailAddress = new MailAddress(Settings.Default.MailAddress);
            var message = new MailMessage(mailAddress, mailAddress);
            if (isSuccess)
            {
                message.Subject = "Созданы архивы BAK файлов";
                message.Body = "Бэкапы успешно упакованы и скопированы";
            }
            else
            {
                message.Subject = "Что то пошло не так...";
                message.Body = "С бэкапами что-то не так, проверь лог файл.";
            }

            //Если пришёл список архивов - генерируем картинки и вкладываем в сообщение
            if (archivesFiles != null)
            {
                GenerateImage(archivesFiles.OrderByDescending(arch => arch.ChangeDate).ToList());
                message.Attachments.Add(new Attachment(@"pictures/output.png"));
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

        private static void GenerateImage(List<ArchiveFile> archivesFiles)
        {
            ImageGenerator imageGenerator = new ImageGenerator();
            imageGenerator.GetImageForGu(archivesFiles);

        }
    }
}
