


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
        static readonly string[] SPLICER_INFO = ["MODELNAME", "SERNUM", "TARCCOUNT"];
        static readonly string[] NONVOLATILE_MEM = ["ESTLOSS", "ESTOFFSETLOSS", "ESTDEFORMLOSS", 
        "ESTMFDLOSS", "ESTMINLOSS", "CLVANGLEL", "CLVANGLER", "FIBERANGLE", "GAP", "COREOFSAFTER",
        "CLADOFSAFTER", "ERR", "FIBERTYPE", "MODETITLE1", "MODETITLE2"];
        static readonly string[] LEFTFIBERINFO =  ["LCLAMPAT", "LCOATINGDIAMETER", "LCLADDIAMETER2", "LCLADDIAMETER", 
        "LCOREDIAMETER", "LMFD", "LCLEAVELENGTH"];
        static readonly string[] RIGHTFIBERINFO = ["RCLAMPAT", "RCOATINGDIAMETER", "RCLADDIAMETER2", "RCLADDIAMETER", 
        "RCOREDIAMETER", "RMFD", "RCLEAVELENGTH"];
        static readonly string[] MAINARC = ["MAINARCPOWERABS", "MAINARCPOWERREL", "MAINARCTIME", 
        "MAINARCELSWING", "MAINARCELSWPOSITION","MAINARCELSWRANGE", "MAINARCTIMECOMPBYECC"];
        static readonly string[] ESTIMATION = ["LOSSESTIMATIONMETHOD", "AXISOFFSETMEASURE", 
        "COREDEFORMATION", "MFDMISMATCHMEASURE", "MINIMUMLOSS", "WAVELENGTH", "COREDEFORMATIONCOEF", 
        "MFDMISMATCHOFFSET", "MFDMISMATCHSENSITIVITY", "ESTMODEFOROLDMETHOD", "CORESTEPCOEF", 
        "CORECURVECOEF", "OLDMFDMISMATCH", "REFPER"];
        
        static string[] imageIDs = ["PREARC", "WSI", "CLD"];
        BMPImage[] bitmaps = new BMPImage[imageIDs.Length*2];
        string serializedJSON = "";
        MemoryImage spliceModeParams;
        string dirPath = "";

        /// <summary>
        /// 
        /// </summary>
        public DataBackup(DateTime currentTime)
        {
            InitBMPImages();

            int location = (int) GetSingleResult("=MEMLATEST", "MEMLATEST");
            InitJSON(location);

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
        /// 
        /// </summary>
        /// <param name="location"></param>
        public void InitJSON(int location)
        {
               
            Dictionary<string, object> unserializedJSON = new();

            // getting splicer and splice info 
            unserializedJSON["SPLICER_INFO"] = GetOutputAsDict("=INF", SPLICER_INFO, true);
            unserializedJSON["NONVOLATILE_MEM"] = GetOutputAsDict($"=MEM-{location}", NONVOLATILE_MEM, false);

            // getting settings info
            Dictionary<string, object> settings = new();
            unserializedJSON["SETTINGS"] = settings;
            settings["LEFTFIBERINFO"] = GetOutputAsDict($"%SPL", LEFTFIBERINFO, true);
            settings["RIGHTFIBERINFO"] = GetOutputAsDict($"%SPL", RIGHTFIBERINFO, true);
            settings["MAINARC"] = GetOutputAsDict($"%SPL", MAINARC, true);
            settings["ESTIMATION"] = GetOutputAsDict($"%SPL", ESTIMATION, true);
            

            // serializing JSON
            var options = new JsonSerializerOptions { WriteIndented = true };
            serializedJSON = JsonSerializer.Serialize(unserializedJSON, options);

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
        /// Saving the BMP image that the splicer gives as a png.
        /// </summary>
        /// <param name="bitmap">Bitmap image as a byte array.</param>
        internal void SaveBMPasPNG(BMPImage bitmap)
        {

            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                throw new Exception("Image processing not supported for operating systems that aren't Windows 6.1+.");
            } 

            // VGA is a resolution display standard which is 480x640 pixels
            int VGA_HEIGHT = 640;
            int VGA_WIDTH = 480;

            // There are metadata bytes at the start of the byte stream, so we only use the last VGA_HEIGHT*VGA_WIDTH bytes
            byte[] image = bitmap.image;
            image = image [(image.Length-VGA_HEIGHT*VGA_WIDTH)..];


            // Allocating memory that the garbage collector doesn't touch
            GCHandle handle = GCHandle.Alloc(image, GCHandleType.Pinned);
            
            using (Bitmap bmp = new Bitmap(VGA_WIDTH, VGA_HEIGHT, VGA_WIDTH, 
                PixelFormat.Format8bppIndexed, handle.AddrOfPinnedObject()))
            {
                // creating color palette
                ColorPalette pal = bmp.Palette;
                for (int i = 0; i < 256; i++) pal.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                bmp.Palette = pal;

                string outputPath = dirPath+bitmap.id+"-"+bitmap.view; 
                bmp.Save(outputPath, ImageFormat.Png);
            }

            handle.Free();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Backup()
        {
            foreach (BMPImage bitmap in bitmaps)
            {
                SaveBMPasPNG(bitmap);
            }

            File.WriteAllText(dirPath+"spliceData.JSON", serializedJSON);
            spliceModeParams.Backup(dirPath);
        }

    }
}