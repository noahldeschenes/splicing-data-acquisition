
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text.Json;

using static RecordSplicingResults.BackupService;
using static RecordSplicingResults.StatusHandler;
using static RecordSplicingResults.ParamService;
using static RecordSplicingResults.OutputHandler;

namespace RecordSplicingResults
{
    internal static class DataProcessor
    {
        static readonly string[] SPLICER_INFO = ["MODELNAME", "SERNUM", "TARCCOUNT"];
        static readonly string[] NONVOLATILE_MEM = ["ESTLOSS", "ESTOFFSETLOSS", "ESTDEFORMLOSS", 
        "ESTMFDLOSS", "ESTMINLOSS", "CLVANGLEL", "CLVANGLER", "FIBERANGLE", "GAP", "COREOFSAFTER",
        "CLADOFSAFTER", "ERR", "FIBERTYPE", "MODETITLE1", "MODETITLE2"];
        //static readonly string[] VOLATILE_MEM = ["PRMDEFORM", "PRMINDEXDIF", "PRMOFFSET", 
        //"PRMCORESTEP", "PRMCORECURVE", "FIBERANGBEFORE", "FIBERANGBEFOREL", "FIBERANGBEFORER",
        //"FIBERANGAFTER", "FIBERANGAFTERL", "FIBERANGAFTERR", "CLADOFSBEFORE", "COREOFSBEFORE", 
        //"ARCPOWER", "ARCTIME", "AXISMOVEMENT"];
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

        public static string CreateJSON(string parentDir, int location)
        {
            // <summary>Queries the splicer for various info we want to keep in a JSON file.<\summary>
               
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
            string serializedJSON = JsonSerializer.Serialize(unserializedJSON, options);
            return serializedJSON;

        } 


        public static void GetImages(string parentDir)
        {
            // <summary>Gets the prearc, warm splice, and cold splice images from the splicer.<\summary>
            string dirname = parentDir+@"\images";

            Directory.CreateDirectory(dirname);

            string[] imageIDs = ["PREARC", "WSI", "CLD"];

            foreach (string id in imageIDs)
            {

                // getting image in X view
                byte[] imgX = splicer.CommandAndReceiveBinary($"=IMGH-{id}-X");
                SaveBMPasPNG(imgX, dirname+@$"\{id}-X.png");

                // getting image in Y view
                byte[] imgY = splicer.CommandAndReceiveBinary($"=IMGH-{id}-Y");
                SaveBMPasPNG(imgY, dirname+@$"\{id}-Y.png");

            }
        }

        public static void SaveBMPasPNG(byte[] image, string outputPath)
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

    }
}