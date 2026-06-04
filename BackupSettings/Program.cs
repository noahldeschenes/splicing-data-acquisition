
using System.Diagnostics;
using System.IO.Compression;
using Utils;

namespace BackupSettings
{

    public static class Program
    {
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

            // <summary> Gets binary image of a given splice mode's parameters. </summary>

            // input validation
            if (spliceMode < 1 || spliceMode > MAX_MODENO)
            {
                throw new ArgumentOutOfRangeException(nameof(spliceMode), "Splice mode must be between 1 and 300.");
            }

            // getting parameters and handling NAKs
            byte[] parameters = splicer.CommandAndReceiveBinary($"%SPLH-{spliceMode}");
            SplicerUtils.QuitIfNAK(Encoding.ASCII.GetString(parameters));
            return parameters;
        }

        private static void BackupSpecific(string parentPath, int spliceMode)
        {
            
            // <summary> Backs up a splice mode's parameters to a given directory. </summary>

            string id = $"{spliceMode}".PadLeft(3, '0'); //formatting spliceMode to have leading zeros (e.g. 001, 002, ..., 300)
            string path = parentPath+@"\"+id;

            // writing to file
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


            // creating the backup directory and adding binary backups
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

        public static void Main(string[] args)
        {
            SplicerUtils.InitializeAndLock();
            SplicerUtils.QuitIfDisconnected();
            
            try { Backup(); }
            catch (Exception e)
            {
                using (StreamWriter sw = File.AppendText(SplicerUtils.LOG_FILE_LOCATION))
                {
                    sw.WriteLine($"Error during backup at {DateTime.UtcNow}: {e.Message}");
                }
            }

            splicer.Command("$UNLOCK");
        }

    }

}