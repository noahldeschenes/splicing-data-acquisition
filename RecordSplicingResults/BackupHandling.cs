
using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Spectre.Console;

namespace RecordSplicingResults
{
    
    public static class BackupUtils
    {
        /*
        This class contains utility functions for communicating with the splicer 
        and backing up splice mode settings. 
        */
        
        
        public const string BACKUP_LOCATION = @"C:\Users\noah.deschenes\Documents\Splicer Mode Settings Backups"; // TODO: Change this
        private const string BucketName = "<PLACEHOLDER>";
        private static readonly RegionEndpoint BucketRegion = RegionEndpoint.USEast1;
        public static DirectoryInfo? currentBackupDirectory = null; // used for cleanup when program fails
        public static Dictionary<int, string> splicerNames = new Dictionary<int, string>
            {
                {07142, "ODIE"}
            };
        
        static string CreateNewSpliceDirectory()
        {
            // <summary>Creates a new directory with structure [serial number]\[date]\[time]<\summary>
            DateTime currentTime = DateTime.Now;
            string date = currentTime.ToString("yyyy-MM-dd");
            string time = currentTime.ToString("HHmm");

            int serialNum = (int) SplicerUtils.GetOutputAsDict("=INF", ["SERNUM"], true)["SERNUM"];
            string name = "UNKNOWN";
            if (SplicerUtils.splicerNames.ContainsKey(serialNum)) name = SplicerUtils.splicerNames[serialNum];
            string serialNumStr = $"{serialNum.ToString().PadLeft(5, '0')} ({name})";


            string dirname = SplicerUtils.RECORDS_DIRECTORY_PATH+@$"\{serialNumStr}\{date}\{time}";
            SplicerUtils.currentBackupDirectory = Directory.CreateDirectory(dirname);

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
                BucketName = BucketName,
                Key = s3Key,
                StorageClass = S3StorageClass.StandardInfrequentAccess  
            };

            transferUtility.Upload(uploadRequest);

        }

        public static void OpenBackups(){
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = SplicerUtils.RECORDS_DIRECTORY_PATH,
                UseShellExecute = true 
            };

            Process.Start(startInfo);
        }

        public static void BackupLastSplice()
        {
            string dirname = CreateNewSpliceDirectory();
            int location = (int) SplicerUtils.GetSingleResult("=MEMLATEST", "MEMLATEST");
            AnsiConsole.Status()
                .Start("[blue]Backing up data...[/]", ctx =>
                {
                    string serializedJSON = SplicerUtils.CreateJSON(dirname, location);
                    File.WriteAllText(dirname + @"\spliceData.JSON", serializedJSON);
                    Thread.Sleep(500);
                    AnsiConsole.MarkupLine("Data backed up.");
                });
            AnsiConsole.Status()
                .Start("[blue]Backing up images...[/]", ctx =>
                {
                    SplicerUtils.GetImages(dirname);
                    AnsiConsole.MarkupLine("Images backed up.");
                });

            AnsiConsole.Status()
                .Start("[blue]Backing up settings...[/]", ctx =>
                {
                    int smode = (int) SplicerUtils.GetSingleResult("%SMODE", "SMODE");
                    BackupSpecificParameters(dirname, smode);
                    Thread.Sleep(500);
                    AnsiConsole.MarkupLine("Settings backed up.");
                });
            
            SplicerUtils.currentBackupDirectory = null;
        }

        public static void BackupSplicesContinuously()
        {
            AnsiConsole.MarkupLine("Press [green][[Ctrl+C]][/] to end continuous backup.");
            SplicerUtils.continuousModeOn = true;
            object prevArcCount;
            object currentArcCount;
            int POLLING_INTERVAL_MS = 1000;
            
            while (true){
                prevArcCount = SplicerUtils.GetSingleResult("=INF", "TARCCOUNT", true);
                if (prevArcCount != SplicerUtils.NAK) break;
                Thread.Sleep(POLLING_INTERVAL_MS);
                break;
            }
            
            while (true)
            {
                currentArcCount = SplicerUtils.GetSingleResult("=INF", "TARCCOUNT", true);
                bool noNewArcs = (currentArcCount == prevArcCount);
                bool invalid = (currentArcCount == SplicerUtils.NAK);

                if (noNewArcs || invalid || !SplicerUtils.SplicerResting(false))
                {
                    Thread.Sleep(POLLING_INTERVAL_MS); 
                    continue;
                } 
                BackupLastSplice();
                prevArcCount = currentArcCount;
            }
        }  
        

        
    }
}