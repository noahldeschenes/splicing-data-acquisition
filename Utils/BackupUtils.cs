using System.IO;
using System.Text;
using System;
using System.IO.Compression;



namespace Utils
{
    
    public class BackupUtils
    {
        public const string BACKUP_LOCATION = @"C:\Users\noah.deschenes\Documents\Splicer Mode Settings Backups";
        public static byte[] GetSpliceParameters(int spliceMode)
        {
            if (spliceMode < 0 || spliceMode > SplicerUtils.MAX_MODENO)
            {
                throw new ArgumentOutOfRangeException(nameof(spliceMode), "Splice mode must be between 0 and 300.");
            }

            // TODO: add logging and error handling for communication issues
            return SplicerUtils.splicer.CommandAndReceiveBinary($"%SPLH-{spliceMode}");
        }

        public static void SendSpliceParameters(int spliceMode, byte[] parameters)
        {
            if (spliceMode < 0 || spliceMode > SplicerUtils.MAX_MODENO)
            {
                throw new ArgumentOutOfRangeException(nameof(spliceMode), "Splice mode must be between 0 and 300.");
            }

            // TODO: add logging and error handling for communication issues
            string response = SplicerUtils.splicer.CommandAndReceiveText($"#SPLH-{spliceMode}");
            SplicerUtils.splicer.SendBinary(ref parameters, parameters.Length, SplicerUtils.STD_TIMEOUT);
        }

        private static void BackupSpecific(string parentPath, int spliceMode)
        {
            string id = $"{spliceMode}".PadLeft(3, '0');
            string path = parentPath+@"\"+id;

            using (FileStream fs = File.Create(path))
            {
                fs.Write(GetSpliceParameters(spliceMode), 0, GetSpliceParameters(spliceMode).Length);
            }
        }
        public static void Backup(string parentPath=BACKUP_LOCATION, bool compression=true)
        {
            //TODO: error handling, descriptions
            
            // choosing a directory name based on the date (and time, if there are conflicts)
            DateTime currentTime = DateTime.UtcNow;
            System.Console.WriteLine(currentTime.ToString("yyyy-MM-dd"));
            string path = parentPath+@"\"+currentTime.ToString("yyyy-MM-dd");
            if (Directory.Exists(path)) path += ", "+currentTime.ToString("HH");


            // creating the backup directory and adding bin files
            DirectoryInfo di = Directory.CreateDirectory(path);
            for (int i=0; i<SplicerUtils.MAX_MODENO+1; i++)
            {
                BackupSpecific(path, i);
            }

            // turning the backup into a zip archive if needed
            if (compression)
            {
                ZipFile.CreateFromDirectory(path, path+".zip");

                foreach (FileInfo f in di.GetFiles()) f.Delete();
                di.Delete();
            }

        }

    }
}