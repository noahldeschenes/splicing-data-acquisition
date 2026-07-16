
using System;
using System.IO;

using static RecordSplicingResults.BackupService;
using static RecordSplicingResults.DataProcessor;
using static RecordSplicingResults.StatusHandler;
using static RecordSplicingResults.OutputHandler;


namespace RecordSplicingResults
{
    public static class ParamService
    {
        public static byte[] GetSpliceParameters(int spliceMode)
        {

            // <summary> Gets binary image of a given splice mode's parameters. </summary>

            byte[] parameters = splicer.CommandAndReceiveBinary($"%SPLH-{spliceMode}"); // assuming spliceMode is in range 1-300
            return parameters;
        }

        public static void SetSpliceParameters(int spliceMode, byte[] parameters)
        {

            if (spliceMode < 1 || spliceMode > NUM_OF_MODES)
            {
                throw new ArgumentOutOfRangeException(nameof(spliceMode), "Splice mode must be between 1 and 300.");
            }

            string response = splicer.CommandAndReceiveText($"#SPLH-{spliceMode}");
            splicer.SendBinary(ref parameters, parameters.Length, 10000);

        }

        public static void BackupSpecificParameters(string parentPath, int spliceMode)
        {
            
            // <summary> Backs up a splice mode's parameters to a given directory. </summary>

            string id = $"{spliceMode}".PadLeft(3, '0'); //formatting spliceMode to have leading zeros (e.g. 001, 002, ..., 300)
            string? modeTitle = (string?) GetSingleResult($"%SPL-{spliceMode}|MODETITLE1", "MODETITLE1");
            if (modeTitle == null) throw new Exception("Splicer query failed.");

            string path = parentPath+@$"\{id} ({modeTitle})";


            // writing to file
            using (FileStream fs = File.Create(path))
            {
                byte[] parameters = GetSpliceParameters(spliceMode);
                fs.Write(parameters, 0, parameters.Length);
            }
        }

        public static void BackupParameters(string parentPath, bool toCloud=false)
        {
            
            // choosing a directory name based on the date (and time, if there are conflicts)
            DateTime currentTime = DateTime.UtcNow;
            string path = parentPath+@"\"+currentTime.ToString("yyyy-MM-dd");
            if (Directory.Exists(path)) path += ", "+currentTime.ToString("HHmm"); 


            // creating the backup directory and adding splice mode files
            currentBackupDirectory = Directory.CreateDirectory(path);
            for (int i=1; i<NUM_OF_MODES+1; i++)
            {
                BackupSpecificParameters(path, i);
            }

            // turning the backup into a zip archive if needed
            if (toCloud) SendDirectoryToS3(path);
            currentBackupDirectory = null; 

        }

        public static void RestoreParameters(string path)
        {

            // checking backup is formatted correctly
            for (int i=1; i<NUM_OF_MODES+1; i++)
            {
                string id = $"{i}".PadLeft(3, '0');
                string filePath = path+@"\"+id;
                if (!File.Exists(filePath)) throw new FileNotFoundException($"File for splice mode {i} not found at {filePath}.");
                if (new FileInfo(filePath).Length != 4100) throw new InvalidDataException($"File for splice mode {i} is not 4100 bytes.");
            }

            // restoring splice modes
            for (int i=1; i<NUM_OF_MODES+1; i++)
            {
                string id = $"{i}".PadLeft(3, '0');
                string filePath = path+@"\"+id;
                byte[] parameters = File.ReadAllBytes(filePath);
                SetSpliceParameters(i, parameters);
            }
        } 
    }
}