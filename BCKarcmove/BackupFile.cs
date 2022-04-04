using System;
using System.Diagnostics;
using System.IO;
using BCKarcmove.Properties;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Writers.Zip;

namespace BCKarcmove
{
    public class BackupFile
    {
        public string FullFileName { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedTime { get; set; }
        public string ArchiveName => $"{FileName}_{CreatedTime.DayOfYear}.rar";
        public string ArchiveFullPath { get; set; }
        public string ArchiveDestination { get; set; }
        private readonly WriterOptions _options = new ZipWriterOptions(CompressionType.Deflate);

        public BackupFile(string filePath, string archPath = null)
        {
            FileName = Path.GetFileName(filePath);
            FullFileName = filePath;
            CreatedTime = File.GetCreationTime(filePath);
            ArchiveDestination = archPath ?? Path.GetDirectoryName(filePath);
        }

        public string CreateArch()
        {

            ArchiveFullPath = Path.Combine(ArchiveDestination, ArchiveName);
            if (File.Exists(ArchiveFullPath)) File.Delete(ArchiveFullPath);


            switch (Settings.Default.ArchiverType)
            {
                case 0:
                    //Архивирование при помощи установленного в системе WinRar
                    var winRarProc = Process.Start("WinRar.exe", $"a -ep -inul -ibck \"{ArchiveFullPath}\" \"{FullFileName}\"");
                    if (winRarProc == null) throw new Exception("Error while starting WinRar. Is it installed?");
                    winRarProc.WaitForExit();
                    break;
                case 1:
                    //Архивирование при помощи библиотеки SharpCompress
                    using (var zip = ZipArchive.Create())
                    {
                        zip.AddEntry(FileName, FullFileName);
                        zip.SaveTo(ArchiveFullPath, _options);
                    }
                    break;
                default:
                    throw new Exception("Неверный тип архиватора в настройках");
            }






            return ArchiveFullPath;

            
            
            
        }

    }
}
