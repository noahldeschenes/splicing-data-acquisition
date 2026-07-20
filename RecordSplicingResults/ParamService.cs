
using System;
using System.IO;

using static RecordSplicingResults.BackupService;
using static RecordSplicingResults.StatusHandler;
using static RecordSplicingResults.OutputHandler;


namespace RecordSplicingResults
{
    /// <summary>
    /// A service class which allows for backing up and restoring splice
    /// mode parameters, to and from arbitrary directories.
    /// </summary>
    public static class ParamService
    {
        
        /// <summary> Gets binary image of a given splice mode's parameters. </summary>
        internal static byte[] GetSpliceParameters(int spliceMode)
        {   
            byte[] parameters = splicer.CommandAndReceiveBinary($"%SPLH-{spliceMode}"); // assuming spliceMode is in range 1-300
            return parameters;
        }

        /// <summary>
        /// Changes a particular splice mode number's parameters.
        /// </summary>
        /// <param name="spliceMode">Splice mode to be changed.</param>
        /// <param name="parameters">A splice mode parameter binary image.</param>
        /// <exception cref="ArgumentOutOfRangeException">Splice mode not between 1 and 300.</exception>
        internal static void SetSpliceParameters(int spliceMode, byte[] parameters)
        {

            if (spliceMode < 1 || spliceMode > NUM_OF_MODES)
            {
                throw new ArgumentOutOfRangeException(nameof(spliceMode), "Splice mode must be between 1 and 300.");
            }

            string response = splicer.CommandAndReceiveText($"#SPLH-{spliceMode}");
            splicer.SendBinary(ref parameters, parameters.Length, 10000);

        }

        /// <summary>
        /// Backs up a specific splice mode's parameters. 
        /// </summary>
        /// <param name="parentPath">Path to the directory in which to put the parameter backup.</param>  
        /// <param name="spliceMode">The splice mode (1-300) to be backed up.</param>
        public static void BackupParametersSpecific(string parentPath, int spliceMode)
        {
            
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

        /// <summary>
        /// Backs up the parameters for every splice mode.
        /// </summary>
        /// <param name="parentPath">Path to the directory in which the backups will go</param>
        /// <param name="toCloud">Boolean representing whether or not to also back up parameters to S3.</param>
        public static void BackupAllParameters(string parentPath, bool toCloud=false)
        {
            
            int? serialNum = (int?) GetSingleResult("=INF", "SERNUM", true);
            if (serialNum is null) throw new Exception("Splicer query failed.");
            string splicerName = "UNKNOWN";
            if (SPLICER_NAMES.ContainsKey(serialNum.Value)) splicerName = SPLICER_NAMES[serialNum.Value];

            string path = parentPath+@"\Splice mode parameter backups\"+splicerName;

            // choosing a directory name based on the date (and time, if there are conflicts)
            DateTime currentTime = DateTime.UtcNow;
            path += @"\"+currentTime.ToString("yyyy-MM-dd");
            if (Directory.Exists(path)) path += ", "+currentTime.ToString("HHmm"); 




            // creating the backup directory and adding splice mode files
            currentBackupDirectory = Directory.CreateDirectory(path);
            for (int i=1; i<NUM_OF_MODES+1; i++)
            {
                BackupParametersSpecific(path, i);
            }

            // turning the backup into a zip archive if needed
            if (toCloud) SendDirectoryToS3(path);
            currentBackupDirectory = null; 

        }

        /// <summary>
        /// Takes a backup produced by BackupAllParameters and restores all of the splicer's
        /// splice mode parameters. 
        /// </summary>
        /// <param name="path">Path to the directory containing the splice mode parameter backups.</param>
        /// <exception cref="FileNotFoundException">One of the 300 splice mode parameter files doesn't exist.</exception>
        /// <exception cref="InvalidDataException">The filesize for a parameter file is inc</exception>
        public static void RestoreAllParameters(string path)
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