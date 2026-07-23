


using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text.Json;
using Spectre.Console;


using static RecordSplicingResults.SpliceBackupService;
using static RecordSplicingResults.StatusHandler;
using static RecordSplicingResults.OutputHandler;
using static RecordSplicingResults.DataProcessor;
using Amazon.S3.Model;


namespace RecordSplicingResults
{
    /// <summary>
    /// 
    /// </summary>
    internal struct BMPImage
    {
        internal string id {get; set;}
        internal string view {get; set;}
        internal byte[] image {get; set;}
    }

    /// <summary>
    /// 
    /// </summary>
    public class DataBackup
    {
        
        
        static string[] imageIDs = ["PREARC", "WSI", "CLD"];
        BMPImage[] bitmaps = new BMPImage[imageIDs.Length*2];
        string serializedJSON;
        MemoryImage spliceModeParams;
        string dirPath = "";

        /// <summary>
        /// 
        /// </summary>
        public DataBackup(DateTime currentTime)
        {
            InitBMPImages();

            int location = (int) GetSingleResult("=MEMLATEST", "MEMLATEST");
            serializedJSON = CreateJSON(location);

            int smode = (int) GetSingleResult("%SMODE", "SMODE");
            spliceModeParams = new MemoryImage(smode);
            
            InitDirPath(smode, currentTime); 

        }

        /// <summary>
        /// 
        /// </summary>
        public void InitBMPImages()
        {
            for(int i=0; i<imageIDs.Length; i++)
            {
                
                byte[] imgX = splicer.CommandAndReceiveBinary($"=IMGH-{imageIDs[i]}-X");
                byte[] imgY = splicer.CommandAndReceiveBinary($"=IMGH-{imageIDs[i]}-Y");

                bitmaps[i*2] = new BMPImage {id=imageIDs[i], view="X", image=imgX};
                bitmaps[i*2+1] = new BMPImage {id=imageIDs[i], view="Y", image=imgY};

            }
        }

        /// <summary>
        /// Generates the appropriate path for the most recent splice.
        /// </summary>
        /// <param name="smode">Splice mode the most recent splice used.</param>
        /// <param name="currentTime"></param>
        /// <returns>A path of the form:
        ///  MAIN_BACKUP_DIRECTORY\Splice data backups\[serial number]([splicer name])\[mode title, e.g. FLEX-SMF]\[date]\[time].</returns>
        /// <exception cref="Exception"></exception>
        internal void InitDirPath(int smode, DateTime currentTime)
        {
            string DATA_BACKUP_MAIN_DIRECTORY = "Splice mode parameter backups";
            
            string date = currentTime.ToString("yyyy-MM-dd");
            string hour = currentTime.ToString("HH");
            string minute = currentTime.ToString("mm");

            string modeTitle = (string) GetSingleResult($"%SPL-{smode}", "MODETITLE1", true);
            string serialNumStr = ParamBackup.GetSerialNumStr();

            dirPath = @$"{MAIN_BACKUP_DIRECTORY}\{DATA_BACKUP_MAIN_DIRECTORY}\
                {serialNumStr}\{modeTitle}\{date}\{hour}h{minute}";
                
        }

        /// <summary>
        /// 
        /// </summary>
        public void Backup()
        {
            foreach (BMPImage bitmap in bitmaps)
            {
                SaveBMPasPNG(bitmap, dirPath);
            }

            File.WriteAllText(dirPath+"spliceData.JSON", serializedJSON);
            spliceModeParams.Backup(dirPath);
        }

    }
}