
using System;
using System.IO;

using static RecordSplicingResults.SpliceBackupService;
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
            byte[] parameters = splicer.CommandAndReceiveBinary($"%SPLH-{spliceMode}"); 
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
            string modeTitle = (string) GetSingleResult($"%SPL-{spliceMode}|MODETITLE1", "MODETITLE1");

            string path = parentPath+@$"\{id} ({modeTitle})";


            // writing to file
            using (FileStream fs = File.Create(path))
            {
                byte[] parameters = GetSpliceParameters(spliceMode);
                fs.Write(parameters, 0, parameters.Length);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentTime"></param>
        /// <returns></returns>
        internal static string GetNewParameterBackupPath(DateTime currentTime)
        {
            int serialNum = (int) GetSingleResult("=INF", "SERNUM", true);
            string splicerName = "UNKNOWN";
            if (SPLICER_NAMES.ContainsKey(serialNum)) splicerName = SPLICER_NAMES[serialNum];

            string serialNumStr = $"{serialNum.ToString().PadLeft(5, '0')} ({splicerName})";

            string path = MAIN_BACKUP_DIRECTORY+@$"\Splice mode parameter backups\{serialNumStr}";

            // choosing a directory name based on the date (and time, if there are conflicts)
            path += @"\"+currentTime.ToString("yyyy-MM-dd");
            if (Directory.Exists(path)) path += ", "+currentTime.ToString("HHmm");

            return path;
        }

        /// <summary>
        /// Backs up the parameters for every splice mode.
        /// </summary>
        /// <param name="toCloud">Boolean representing whether or not to also back up parameters to S3.</param>
        public static void BackupAllParameters(bool toCloud=false)
        {
            
            string path = GetNewParameterBackupPath(DateTime.Now);

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
        /// Restores the splice parameters for a single splice mode. 
        /// </summary>
        /// <param name="path">Path to either the parent directory or the bin file itself.</param>
        /// <param name="spliceMode">The splice mode number to restore (1-300).</param>
        /// <param name="pathIsParent">Whether or not the path is to a parent directory or to the file itself.</param>
        /// <exception cref="Exception">Wrong number of files that match the splice mode number.</exception>
        /// <exception cref="InvalidDataException">File isn't 4100 bytes.</exception>
        public static void RestoreParametersSpecific(string path, int spliceMode, bool pathIsParent=true)
        {
            if (pathIsParent) {
                string searchPattern = spliceMode.ToString().PadLeft(3, '0')+"*";
                string[] matchedFiles = Directory.GetFiles(path, searchPattern);
                if (matchedFiles.Length > 1) throw new Exception($"Multiple backup files found for splice mode {spliceMode}.");
                if (matchedFiles.Length == 0) throw new Exception($"No backup files found for splice mode {spliceMode}.");
                path = matchedFiles[0];
            }
            if (new FileInfo(path).Length != 4100) throw new InvalidDataException($"File for splice mode {spliceMode} is not 4100 bytes.");

            byte[] parameters = File.ReadAllBytes(path);
            SetSpliceParameters(spliceMode, parameters);

        }

        /// <summary>
        /// Takes a backup produced by BackupAllParameters and restores all of the splicer's
        /// splice mode parameters. 
        /// </summary>
        /// <param name="path">Path to the directory containing the splice mode parameter backups.</param>
        public static void RestoreAllParameters(string path)
        {
            for (int i=1; i<NUM_OF_MODES+1; i++)
            {
                RestoreParametersSpecific(path, i);
            }
            // maybe add console output here
        } 
    }
}