
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Spectre.Console;
using System.Collections.Generic;


using static RecordSplicingResults.DataProcessor;
using static RecordSplicingResults.StatusHandler;
using static RecordSplicingResults.ParamService;
using static RecordSplicingResults.OutputHandler;

namespace RecordSplicingResults
{
    /// <summary>
    /// Service class that deals with backing up splice data (not mode parameters).
    /// </summary>
    public static class BackupService
    {
        
        internal static bool continuousModeOn = false;
        private static readonly RegionEndpoint BucketRegion = RegionEndpoint.USEast1;
        internal static DirectoryInfo? currentBackupDirectory = null; // used for cleanup when program fails

        internal static string MAIN_BACKUP_DIRECTORY = "";
        internal static Dictionary<int, string> SPLICER_NAMES = [];
        internal static string S3_BUCKET_NAME = "";

        /// <summary>
        /// Generates the appropriate path for the most recent splice.
        /// </summary>
        /// <param name="smode">Splice mode the most recent splice used.</param>
        /// <returns>A path of the form:
        ///  MAIN_BACKUP_DIRECTORY\Splice data backups\[serial number]([splicer name])\[mode title, e.g. FLEX-SMF]\[date]\[time].</returns>
        /// <exception cref="Exception"></exception>
        internal static string GetNewSpliceDirectoryPath(int smode)
        {
            
            DateTime currentTime = DateTime.Now;
            string date = currentTime.ToString("yyyy-MM-dd");
            string hour = currentTime.ToString("HH");
            string minute = currentTime.ToString("mm");

            int serialNum = (int) GetSingleResult("=INF", "SERNUM", true);
            string modeTitle = (string) GetSingleResult($"%SPL-{smode}|MODETITLE1", "MODETITLE1");

            string name = "UNKNOWN";
            if (SPLICER_NAMES.ContainsKey(serialNum)) name = SPLICER_NAMES[serialNum];
            string serialNumStr = $"{serialNum.ToString().PadLeft(5, '0')} ({name})";

            string dirname = MAIN_BACKUP_DIRECTORY+@$"Splice data backups\{serialNumStr}\{modeTitle}\{date}\{hour}h{minute}";

            return dirname;
                
        }

        public static void SendDirectoryToS3(string path)
        {
            var di = new DirectoryInfo(path);
            ZipFile.CreateFromDirectory(path, path+".zip");
            foreach (FileInfo f in di.GetFiles()) f.Delete();
            di.Delete();
            

            string s3Key = $"<PLACEHOLDER>"; // name of backup in the bucket
            using var s3Client = new AmazonS3Client(BucketRegion);
            using var transferUtility = new TransferUtility(s3Client);
            
            var uploadRequest = new TransferUtilityUploadRequest
            {
                FilePath = path,
                BucketName = S3_BUCKET_NAME,
                Key = s3Key,
                StorageClass = S3StorageClass.StandardInfrequentAccess  
            };

            transferUtility.Upload(uploadRequest);

        }

        /// <summary>
        /// Opens MAIN_BACKUP_DIRECTORY to the user for easier navigation.
        /// </summary>
        public static void OpenBackups(){
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = MAIN_BACKUP_DIRECTORY,
                UseShellExecute = true 
            };

            Process.Start(startInfo);
        }

        /// <summary>
        /// Gets the information that the splicer stores about the previous splice (images, 
        /// cleave/fiber angles, etc) and backs it up to S3/the local filesystem.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void BackupLastSplice()
        {

            int? location = (int?) GetSingleResult("=MEMLATEST", "MEMLATEST");
            int? smode = (int?) GetSingleResult("%SMODE", "SMODE");
            if (location is null || smode is null) throw new Exception("Splicer query failed.");

            string dirname = GetNewSpliceDirectoryPath(smode.Value);
            currentBackupDirectory = Directory.CreateDirectory(dirname);

            AnsiConsole.Status()
                .Start("[blue]Backing up data...[/]", ctx =>
                {
                    string serializedJSON = CreateJSON(location.Value);
                    File.WriteAllText(dirname + @"\spliceData.JSON", serializedJSON);
                    Thread.Sleep(500);
                    AnsiConsole.MarkupLine("Data backed up.");
                });
            AnsiConsole.Status()
                .Start("[blue]Backing up images...[/]", ctx =>
                {
                    GetImages(dirname);
                    AnsiConsole.MarkupLine("Images backed up.");
                });

            AnsiConsole.Status()
                .Start("[blue]Backing up settings...[/]", ctx =>
                {

                    BackupParametersSpecific(dirname, smode.Value);
                    Thread.Sleep(500);
                    AnsiConsole.MarkupLine("Settings backed up.");
                });
            
            currentBackupDirectory = null;
        }

        /// <summary>
        /// This is an "automatic mode" which allows the user to back up splices without
        /// needing to interact with the CLI.
        /// </summary>
        public static void BackupSplicesContinuously()
        {
            AnsiConsole.MarkupLine("Press [green][[Ctrl+C]][/] to end continuous backup.");
            continuousModeOn = true;
            
            while (true)
            {
                WaitForNewSplice(1000);
                BackupLastSplice();
            }
        }   
    }
}