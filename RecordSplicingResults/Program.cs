
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixImage = SixLabors.ImageSharp.Image;




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
        

        static int prevSerialNum = -1;
        static int prevTArcCount = -1;

        static void WaitForNewSplice()
        {
            

            // We make the assumption that if the serial number of the splicer or the # of arcs has changed, then
            // this is a new splice. Might be more reasonable to also check the file itself, but for now
            // this is good enough (we might get duplicate data by accident is the worst thing).

            while (true)
            {

                SplicerUtils.WaitForConnection();

                var splicerInfo = SplicerUtils.GetOutputAsDict("=INF", SPLICER_INFO);
                int curSerialNum = (int) splicerInfo["SERNUM"];
                int curTArcCount = (int) splicerInfo["TARCCOUNT"];

                
                if (curSerialNum == prevSerialNum && curTArcCount == prevTArcCount) {
                    Thread.Sleep(SplicerUtils.POLLING_WAIT_TIME);   
                    continue;     
                }

                prevSerialNum = curSerialNum;
                prevTArcCount = curTArcCount;
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

            // VGA is a resolution display standard which is 640 x 480 pixels
            int VGA_WIDTH = 640;
            int VGA_HEIGHT = 480;


            string dirname = parentDir+"/images";
            Directory.CreateDirectory(dirname);

            string[] imageIDs = ["PREARC", "WSI", "CLD", "LIVE", "EV"];

            foreach (string id in imageIDs)
            {

                // getting image in X view
                SplicerUtils.QuerySplicer($"=IMGH-{id}-X", []);
                byte[] imgX = SplicerUtils.splicer.ReceiveBinary();
                using (Image<L8> image = SixImage.LoadPixelData<L8>(imgX, VGA_WIDTH, VGA_HEIGHT))
                {
                    image.SaveAsPng(dirname+"/"+id+"-X");   
                }

                // getting image in Y view
                SplicerUtils.QuerySplicer($"=IMGH-{id}-Y", []);
                byte[] imgY = SplicerUtils.splicer.ReceiveBinary();
                using (Image<L8> image = SixImage.LoadPixelData<L8>(imgY, VGA_WIDTH, VGA_HEIGHT))
                {
                    image.SaveAsPng(dirname+"/"+id+"-Y");   
                }

            }
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
        
        static void CoreLoop()
        {

            SplicerUtils.splicer.InitDriver(Process.GetCurrentProcess().Handle);

            while (true)
            {   

                WaitForNewSplice();
                SplicerUtils.splicer.Command("LOCK");
                string dirname = CreateNewSpliceDirectory();

                int.TryParse(SplicerUtils.QuerySplicer("=MEMLATEST", []), out int location);

                GetImages(dirname);
                CreateJSON(dirname, location);
                BackupUtils.BackupSpecific(dirname, location);
                SplicerUtils.splicer.Command("UNLOCK");
            }
        }
    }
}

