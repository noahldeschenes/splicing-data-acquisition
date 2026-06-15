
using System.Diagnostics;
using System.Text;
using System.IO.Compression;
using Utils;

namespace Utils
{

    public static class BackupUtils
    {
        /*
        This class contains utility functions for communicating with the splicer 
        and backing up splice mode settings. 
        */
        
        
        public const string BACKUP_LOCATION = ""; // TODO: Change this
        
        
        private static byte[] GetSpliceParameters(int spliceMode)
        {

            // <summary> Gets binary image of a given splice mode's parameters. </summary>

            byte[] parameters = SplicerUtils.splicer.CommandAndReceiveBinary($"%SPLH-{spliceMode}"); // assuming spliceMode is in range 1-300
            return parameters;
        }

        public static void BackupSpecific(string parentPath, int spliceMode)
        {
            
            // <summary> Backs up a splice mode's parameters to a given directory. </summary>

            string id = $"{spliceMode}".PadLeft(3, '0'); //formatting spliceMode to have leading zeros (e.g. 001, 002, ..., 300)
            string path = parentPath+@"\"+id;

            // writing to file
            using (FileStream fs = File.Create(path))
            {
                byte [] parameters = GetSpliceParameters(spliceMode);
                fs.Write(parameters, 0, parameters.Length);
            }
        }
        public static void Backup(string parentPath=BACKUP_LOCATION, bool compression=true)
        {
            
            // choosing a directory name based on the date (and time, if there are conflicts)
            DateTime currentTime = DateTime.UtcNow;
            string path = parentPath+@"\"+currentTime.ToString("yyyy-MM-dd");
            if (Directory.Exists(path)) path += ", "+currentTime.ToString("HH"); // adding hour to the name if there are multiple backups on the same day


            // creating the backup directory and adding splice mode files
            DirectoryInfo di = Directory.CreateDirectory(path);
            for (int i=1; i<SplicerUtils.MAX_MODENO+1; i++)
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