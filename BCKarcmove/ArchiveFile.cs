using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace BCKarcmove
{
    public class ArchiveFile
    {
        public ArchiveFile(string archFilePath)
        {
            var file = new FileInfo(archFilePath);
            FileName = file.Name;
            ChangeDate = file.LastWriteTime;
            SizeInKb = file.Length / 1024;
        }

        public string FileName { get; set; }

        public DateTime ChangeDate { get; set; }

        public string Type { get; set; } = "Архив WinRAR";

        public long SizeInKb { get; set; }

    }
}
