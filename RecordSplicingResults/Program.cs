
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Utils;



namespace RecordSplicingResults
{
    /*
        Folder: date
        Files inside: JSON, images, errors


        JSON format: Splicer info, Relevant splice settings, Volatile memory splice data, 
        Non-volatile memory splice data

    */

    


    class Program
    {

        static readonly string[] SPLICER_INFO = ["MODELNAME", "SERNUM", "TARCCOUNT"];
        static readonly string[] NONVOLATILE_MEM = ["ESTLOSS", "ESTOFFSETLOSS", "ESTDEFORMLOSS", 
        "ESTMFDLOSS", "ESTMINLOSS", "CLVANGLEL", "CLVANGLER", "FIBERANGLE", "GAP", "COREOFSAFTER",
        "CLADOFSAFTER", "ERR", "FIBERTYPE", "MODETITLE1", "MODETITLE2"];
        static readonly string[] VOLATILE_MEM = ["PRMDEFORM", "PRMINDEXDIF", "PRMOFFSET", 
        "PRMCORESTEP", "PRMCORECURVE", "FIBERANGBEFORE", "FIBERANGBEFOREL", "FIBERANGBEFORER",
        "FIBERANGAFTER", "FIBERANGAFTERL", "FIBERANGAFTERR", "CLADOFSBEFORE", "COREOFSBEFORE", 
        "ARCPOWER", "ARCTIME", "AXISMOVEMENT"];
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


        static readonly string RECORDS_DIRECTORY_PATH = ""; // TODO: find directory
        
        static bool SplicerConnected()
        {   
        
            SplicerUtils.splicer.InitDriver(Process.GetCurrentProcess().Handle); //test if you can/should do this multiple times
            if (!SplicerUtils.splicer.ConnectionStatus)
            {
                Console.WriteLine("ERROR: Splicer disconnected. To troubleshoot:");
                Console.WriteLine("    1. Check that the splicer has a USB cable at the back connected to the computer.");
                Console.WriteLine("    2. Turn the splicer off and back on again.");
                return false;
            }

            Console.WriteLine("Splicer connected...");
            return true;
                     
        }

        static bool SplicerResting()
        {
            string currentStatus = SplicerUtils.QuerySplicer("=FUNCSTAT", []);
            if (currentStatus != "READY" && currentStatus != "FINISH")
            {
                Console.WriteLine("ERROR: Splicer is not at the READY or FINISH state.");
                return false;
            }
            else
            {
                Console.WriteLine("Locking keypad...");
                return true;
            }
        }

        static string CreateNewSpliceDirectory()
        {

            DateTime currentTime = DateTime.UtcNow;
            string date = currentTime.ToString("yyyy-MM-dd");
            string time = currentTime.ToString("HHmm");

            string dirname = RECORDS_DIRECTORY_PATH+"/"+date+"/"+time;
            Directory.CreateDirectory(dirname);

            return dirname;
                
        }

        static void GetImages(string parentDir)
        {

            string dirname = parentDir+"/images";
            Directory.CreateDirectory(dirname);

            string[] imageIDs = ["PREARC", "WSI", "CLD", "LIVE", "EV"];

            foreach (string id in imageIDs)
            {

                // getting image in X view
                SplicerUtils.QuerySplicer($"=IMGH-{id}-X", []);
                byte[] imgX = SplicerUtils.splicer.ReceiveBinary();
                SaveBMP(imgX, dirname+"/"+id+"-X.png");

                // getting image in Y view
                SplicerUtils.QuerySplicer($"=IMGH-{id}-Y", []);
                byte[] imgY = SplicerUtils.splicer.ReceiveBinary();
                SaveBMP(imgY, dirname+"/"+id+"-Y.png");

            }
        }
        public static void SaveBMP(byte[] image, string outputPath)
        {
            // VGA is a resolution display standard which is 640 x 480 pixels
            int VGA_WIDTH = 640;
            int VGA_HEIGHT = 480;

            GCHandle handle = GCHandle.Alloc(image, GCHandleType.Pinned);
            
            using (Bitmap bmp = new Bitmap(VGA_WIDTH, VGA_HEIGHT, VGA_WIDTH, 
                PixelFormat.Format8bppIndexed, handle.AddrOfPinnedObject()))
            {
                // creating color palette
                ColorPalette pal = bmp.Palette;
                for (int i = 0; i < 256; i++) pal.Entries[i] = Color.FromArgb(i, i, i);
                bmp.Palette = pal;

                bmp.Save(outputPath, ImageFormat.Png);
            }

            handle.Free();
        }
        static void CreateJSON(string parentDir, int location)
        {
            
               
            Dictionary<string, object> unserializedJSON = new();

            // getting splicer and splice info 
            unserializedJSON["SPLICER_INFO"] = SplicerUtils.GetOutputAsDict("=INF", SPLICER_INFO);
            unserializedJSON["NONVOLATILE_MEM"] = SplicerUtils.GetOutputAsDict($"=MEM-{location}", NONVOLATILE_MEM);
            unserializedJSON["VOLATILE_MEM"] = SplicerUtils.GetOutputAsDict("=DATH", VOLATILE_MEM);


            // getting settings info
            Dictionary<string, object> settings = new();
            unserializedJSON["SETTINGS"] = settings;
            settings["LEFTFIBERINFO"] = SplicerUtils.GetOutputAsDict($"MEMSPL-{location}", LEFTFIBERINFO);
            settings["RIGHTFIBERINFO"] = SplicerUtils.GetOutputAsDict($"MEMSPL-{location}", RIGHTFIBERINFO);
            settings["MAINARC"] = SplicerUtils.GetOutputAsDict($"MEMSPL-{location}", MAINARC);
            settings["ESTIMATION"] = SplicerUtils.GetOutputAsDict($"MEMSPL-{location}", ESTIMATION);
            

            // serializing JSON
            string filename = parentDir+"/spliceData.JSON";
            string serializedJSON = JsonSerializer.Serialize(unserializedJSON);
            File.WriteAllText(filename, serializedJSON);

        }

        static void StartingMessage()
        {   
            Console.Title = "RecordSplicingResults";
            Console.WriteLine("Splice data backup Wizard v0.1.0");
            Console.WriteLine();
            Console.WriteLine(@"This software connects to Fujikura FSM-100 series splicers to migrate splice
            data to the cloud. Note that the keypad will be locked during the backup.");
        }
        
        static void Main(string[] args)
        {

            StartingMessage();

            while (true)
            {   
                Console.WriteLine();
                Console.WriteLine("Hit [ENTER] to continue, [CTRL+C] to quit:");
                Console.ReadLine();

                if (!SplicerConnected()) continue;
                if (!SplicerResting()) continue;
                SplicerUtils.splicer.Command("LOCK");
                string dirname = CreateNewSpliceDirectory();

                int.TryParse(SplicerUtils.QuerySplicer("=MEMLATEST", []), out int location);

                Console.WriteLine("Backing up images...");
                GetImages(dirname);

                Console.WriteLine("Backing up data...");
                CreateJSON(dirname, location);

                Console.WriteLine("Backing up settings...");
                BackupUtils.BackupSpecific(dirname, location);
                SplicerUtils.splicer.Command("UNLOCK");
                Console.WriteLine("Unlocking keypad...");


            }
        }
    }
}
    

    

