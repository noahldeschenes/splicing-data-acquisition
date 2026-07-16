
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

        public static void SaveBMP(byte[] image, string outputPath)
        {
            // <summary>Saving the .BMP image that the splicer gives as a png.<\summary>


            // VGA is a resolution display standard which is 480x640 pixels
            int VGA_HEIGHT = 640;
            int VGA_WIDTH = 480;

            image = image[(image.Length-VGA_HEIGHT*VGA_WIDTH)..];

            GCHandle handle = GCHandle.Alloc(image, GCHandleType.Pinned);
            
            using (Bitmap bmp = new Bitmap(VGA_WIDTH, VGA_HEIGHT, VGA_WIDTH, 
                PixelFormat.Format8bppIndexed, handle.AddrOfPinnedObject()))
            {
                // creating color palette
                ColorPalette pal = bmp.Palette;
                for (int i = 0; i < 256; i++) pal.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                bmp.Palette = pal;

                bmp.Save(outputPath, ImageFormat.Png);
            }

            handle.Free();
        }

        public static void BackupSpecificParameters(string parentPath, int spliceMode)
        {
            
            // <summary> Backs up a splice mode's parameters to a given directory. </summary>

            string id = $"{spliceMode}".PadLeft(3, '0'); //formatting spliceMode to have leading zeros (e.g. 001, 002, ..., 300)
            string modeTitle = (string) SplicerUtils.GetSingleResult($"%SPL-{spliceMode}|MODETITLE1", "MODETITLE1");
            string path = parentPath+@$"\{id} ({modeTitle})";


            // writing to file
            using (FileStream fs = File.Create(path))
            {
                byte[] parameters = SplicerUtils.GetSpliceParameters(spliceMode);
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
            SplicerUtils.currentBackupDirectory = Directory.CreateDirectory(path);
            for (int i=1; i<SplicerUtils.NUM_OF_MODES+1; i++)
            {
                BackupSpecificParameters(path, i);
            }

            // turning the backup into a zip archive if needed
            if (toCloud) SendDirectoryToS3(path);
            SplicerUtils.currentBackupDirectory = null; 

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
        

        public static void RestoreParameters(string path)
        {

            // checking backup is formatted correctly
            for (int i=1; i<MAX_MODENO+1; i++)
            {
                string id = $"{i}".PadLeft(3, '0');
                string filePath = path+@"\"+id;
                if (!File.Exists(filePath)) throw new FileNotFoundException($"File for splice mode {i} not found at {filePath}.");
                if (new FileInfo(filePath).Length != 4100) throw new InvalidDataException($"File for splice mode {i} is not 4100 bytes.");
            }

            // restoring splice modes
            for (int i=1; i<MAX_MODENO+1; i++)
            {
                string id = $"{i}".PadLeft(3, '0');
                string filePath = path+@"\"+id;
                byte[] parameters = File.ReadAllBytes(filePath);
                SplicerUtils.SetSpliceParameters(i, parameters);
            }
        } 
    }
}