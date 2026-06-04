
using System.IO.Compression;

namespace Utils
{

    public class SplicerUtils{
        /*
        This class contains utility functions for communicating with the splicer 
        and backing up splice mode settings. 

        TODO: NAK handling, error handling, descriptions, README
        */

        public const int STD_TIMEOUT = 10000; // ten seconds
        public const int MAX_MODENO = 300; // splicer has modes numbered 1-300
        public static UsbFsm100ServerClass splicer = new();
        public const string BACKUP_LOCATION = @"C:\Users\noah.deschenes\Documents\Splicer Mode Settings Backups"; // TODO: Change this
        
        
        private static byte[] GetSpliceParameters(int spliceMode)
        {

            if (spliceMode < 1 || spliceMode > MAX_MODENO)
            {
                throw new ArgumentOutOfRangeException(nameof(spliceMode), "Splice mode must be between 1 and 300.");
            }

            
            return splicer.CommandAndReceiveBinary($"%SPLH-{spliceMode}");
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
            
            // choosing a directory name based on the date (and time, if there are conflicts)
            DateTime currentTime = DateTime.UtcNow;
            System.Console.WriteLine(currentTime.ToString("yyyy-MM-dd"));
            string path = parentPath+@"\"+currentTime.ToString("yyyy-MM-dd");
            if (Directory.Exists(path)) path += ", "+currentTime.ToString("HH");


            // creating the backup directory and adding bin files
            DirectoryInfo di = Directory.CreateDirectory(path);
            for (int i=1; i<MAX_MODENO+1; i++)
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


        public static void Restore(string backupPath)
        {
            
        }

    }

}
