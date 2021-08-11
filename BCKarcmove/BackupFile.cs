using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;

namespace BCKarcmove
{
    public class BackupFile
    {
        public string FullFileName { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedTime { get; set; }
        public string ArchiveName => FileName + $"_{CreatedTime.DayOfYear}.rar";
        public string ArchiveFullPath { get; set; }
        public string ArchiveDestination { get; set; }

        public string CreateArch()
        {

            ArchiveFullPath = Path.Combine(ArchiveDestination, ArchiveName);
            if (File.Exists(ArchiveFullPath)) File.Delete(ArchiveFullPath);
            
            if (new DriveInfo (new DirectoryInfo(ArchiveDestination).Root.ToString()).AvailableFreeSpace < new FileInfo(FullFileName).Length)
                throw new Exception("Not enough space for one of archives");
            

            var winRarProc = Process.Start("WinRar.exe", $"a -ep -ibck \"{ArchiveFullPath}\" \"{FullFileName}\"");
            if (winRarProc == null) throw new Exception("Error while starting WinRar. Is it installed?");
            winRarProc.WaitForExit();


            return ArchiveFullPath;
        }

    }
}
