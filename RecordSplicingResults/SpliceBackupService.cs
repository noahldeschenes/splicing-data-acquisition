
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


using static RecordSplicingResults.StatusHandler;
using static RecordSplicingResults.OutputHandler;

namespace RecordSplicingResults
{
    /// <summary>
    /// Service class that deals with backing up splice data (not mode parameters).
    /// </summary>
    public static class SpliceBackupService
    {
        
        internal static bool continuousModeOn = false;
        private static readonly RegionEndpoint BucketRegion = RegionEndpoint.USEast1;
        internal static DirectoryInfo? currentBackupDirectory = null; // used for cleanup when program fails

        internal static string MAIN_BACKUP_DIRECTORY = "";
        internal static Dictionary<int, string> SPLICER_NAMES = [];
        internal static string S3_BUCKET_NAME = "";

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
    }
}