

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text.Json;


using static RecordSplicingResults.SpliceBackupService;
using static RecordSplicingResults.StatusHandler;
using static RecordSplicingResults.OutputHandler;




namespace RecordSplicingResults
{
    /// <summary>
    /// 
    /// </summary>
    internal class MemoryImage
    {
        readonly int spliceMode;
        readonly string? modeTitle;
        byte[] parameters;
        string? filename;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spliceMode"></param>
        public MemoryImage(int spliceMode)
        {   
            if (spliceMode < 1 || spliceMode > ParamBackup.NUM_OF_MODES)
            {
                throw new ArgumentOutOfRangeException(nameof(spliceMode), "Splice mode must be between 1 and 300.");
            }

            this.spliceMode = spliceMode;
            modeTitle = (string) GetSingleResult($"%SPL-{spliceMode}|MODETITLE1", "MODETITLE1");
            parameters = splicer.CommandAndReceiveBinary($"%SPLH-{spliceMode}"); 

            if (parameters.Length != 4100) 
            {
                throw new InvalidDataException($"Parameter retrieval failed for splice mode {spliceMode} ({modeTitle}).");
            }

            InitFilename();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spliceMode"></param>
        /// <param name="path"></param>
        public MemoryImage(int spliceMode, string path)
        {
            this.spliceMode = spliceMode;
            parameters = File.ReadAllBytes(path);
            if (parameters.Length != 4100) 
            {
                throw new InvalidDataException($"Incorrect file length for memory image at '{path}'.");
            }
        }

        internal MemoryImage(int spliceMode, string? modeTitle, byte[] parameters)
        {
            this.spliceMode = spliceMode;
            this.modeTitle = modeTitle;
            this.parameters = parameters;
            InitFilename();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentPath"></param>  
        public void Backup(string parentPath)
        {
            string path = parentPath+filename;
            File.WriteAllBytes(path, parameters);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        public void Restore()
        {
            string response = splicer.CommandAndReceiveText($"#SPLH-{spliceMode}");
            splicer.SendBinary(ref parameters, parameters.Length, 10000);
        }

        private void InitFilename()
        {

            filename = $"{spliceMode}".PadLeft(3, '0');

            if (modeTitle is not null)
            {   
                filename += $" ({modeTitle})";
            }

        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ParamBackup
    {
        internal const int NUM_OF_MODES = 300;
        readonly MemoryImage[] memoryImages = new MemoryImage[NUM_OF_MODES];
        string? dirPath;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentTime"></param>
        public ParamBackup(DateTime currentTime)
        {
            
            for (int i=0; i<NUM_OF_MODES; i++)
            {
                memoryImages[i] = new MemoryImage(i+1);
            }

            InitDirPath(currentTime);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="Exception"></exception>
        public ParamBackup(string path)
        {
            for (int i=0; i<NUM_OF_MODES; i++)
            {
                string searchPattern = (i+1).ToString().PadLeft(3, '0')+"*";
                string[] matchedFiles = Directory.GetFiles(path, searchPattern);
                
                if (matchedFiles.Length > 1) throw new Exception($"Multiple backup files found for splice mode {i+1}.");
                if (matchedFiles.Length == 0) throw new Exception($"No backup files found for splice mode {i+1}.");
                
                memoryImages[i] = new MemoryImage(i+1, matchedFiles[0]);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal static string GetSerialNumStr()
        {
            int serialNum = (int) GetSingleResult("=INF", "SERNUM", true);
            string splicerName = "UNKNOWN";
            if (SPLICER_NAMES.ContainsKey(serialNum)) splicerName = SPLICER_NAMES[serialNum];

            string serialNumStr = $"{serialNum.ToString().PadLeft(5, '0')} ({splicerName})";
            
            return serialNumStr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentTime"></param>
        /// <returns></returns>
        private void InitDirPath(DateTime currentTime)
        {
            string PARAM_BACKUP_MAIN_DIRECTORY = "Splice data backups";

            string serialNumStr = GetSerialNumStr();
            dirPath = MAIN_BACKUP_DIRECTORY+@"\"+PARAM_BACKUP_MAIN_DIRECTORY+$@"\{serialNumStr}";

            // choosing a directory name based on the date (and time, if there are conflicts)
            dirPath += @"\"+currentTime.ToString("yyyy-MM-dd");
            if (Directory.Exists(dirPath)) dirPath += ", "+currentTime.ToString("HHmm");
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void BackupAll()
        {
            if (dirPath == null) throw new NullReferenceException("DirPath uninitialized.");
            
            for (int i=0; i<NUM_OF_MODES; i++)
            {
                memoryImages[i].Backup(dirPath);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public void RestoreAll()
        {
            for (int i=0; i<NUM_OF_MODES; i++)
            {
                memoryImages[i].Restore();
            }

        }

    }
}