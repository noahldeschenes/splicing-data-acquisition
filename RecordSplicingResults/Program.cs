
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Utils;
using System.Linq.Expressions;
using System.Data.Common;



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


        static readonly string RECORDS_DIRECTORY_PATH = @"C:\Users\noah.deschenes\Documents\Records"; // TODO: find directory
        
        public const char NAK = '\x15'; // ASCII code for NAK
        public static UsbFsm100ServerClass splicer = new();
        static bool SplicerConnected()
        {   
            // <summary> Initializes the driver and checks if the splicer is connected. </summary>
            
            splicer.InitDriver(Process.GetCurrentProcess().Handle); 

            if (!splicer.ConnectionStatus)
            {
                Console.WriteLine("ERROR: Splicer disconnected. To troubleshoot:");
                Console.WriteLine("    1. Check that the splicer has a USB cable at the back connected to the computer.");
                Console.WriteLine("    2. Turn the splicer off and back on again.");
                Console.WriteLine("Press [ENTER] to continue:");
                Console.ReadLine();
                return false;
            }

            Console.WriteLine("Splicer connected...");
            return true;
                     
        }

        static bool SplicerResting()
        {
            // <summary>Checks if the splicer is at a valid state for queries.</summary>

            string currentStatus = splicer.CommandAndReceiveText("=FUNCSTAT");
            if (currentStatus != "IDLE" && currentStatus != "ERRFIN" && currentStatus != "NOFIN") 
            {
                Console.WriteLine($"ERROR: Splicer is not at the READY or FINISH state (currently at '{currentStatus}' state).");
                return false;
            }
            else
            {
                return true;
            }
        }

        static string CreateNewSpliceDirectory()
        {
            // <summary>Creates a new directory with structure [serial number]\[date]\[time]<\summary>
            DateTime currentTime = DateTime.Now;
            string date = currentTime.ToString("yyyy-MM-dd");
            string time = currentTime.ToString("HHmm");

            int serialNum = (int) GetOutputAsDict("=INF", ["SERNUM"], true)["SERNUM"];

            string dirname = RECORDS_DIRECTORY_PATH+@$"\{serialNum}\{date}\{time}";
            Directory.CreateDirectory(dirname);

            return dirname;
                
        }

        static void GetImages(string parentDir)
        {
            // <summary>Gets the prearc, warm splice, and cold splice images from the splicer.<\summary>
            string dirname = parentDir+@"\images";

            Directory.CreateDirectory(dirname);

            string[] imageIDs = ["PREARC", "WSI", "CLD"];

            foreach (string id in imageIDs)
            {

                // getting image in X view
                byte[] imgX = splicer.CommandAndReceiveBinary($"=IMGH-{id}-X");
                SaveBMP(imgX, dirname+@$"\{id}-X.png");

                // getting image in Y view
                byte[] imgY = splicer.CommandAndReceiveBinary($"=IMGH-{id}-Y");
                SaveBMP(imgY, dirname+@$"\{id}-Y.png");

            }
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
                for (int i = 0; i < 256; i++) pal.Entries[i] = Color.FromArgb(i, i, i);
                bmp.Palette = pal;

                bmp.Save(outputPath, ImageFormat.Png);
            }

            handle.Free();
        }
        public static Dictionary<string, object> GetOutputAsDict(string query, string[] identifiers, bool concatenate)
        {
            // <summary> Takes the output of the splicer and formats it into a dict </summary>


            Dictionary<string, object> pairs = new();
            string splicerOutput;

            if (concatenate) 
            {
                string newQuery = query;
                foreach (string id in identifiers) newQuery+=$"|{id}";
                splicerOutput = splicer.CommandAndReceiveText(newQuery);
            }
            else splicerOutput = splicer.CommandAndReceiveText(query);
            

            foreach (string id in identifiers) pairs[id] = GetResultFromId(splicerOutput, id)!;


            return pairs;
        }

        private static object? GetResultFromId(string splicerOutput, string id)
        {

            // input form: IDENTIFIER1=RESULT1|IDENTIFIER2=RESULT2|...   
            string pattern = @"(?<identifier>[^|=]+)=(?<result>[^|]*)";  
            
            foreach (Match match in Regex.Matches(splicerOutput, pattern))
            {
                string matchedId = match.Groups["identifier"].Value;
                string matchedResult = match.Groups["result"].Value; 
                             
                if (matchedId == id)
                {
                    return AutoParse(matchedResult);
                }
            }   

            return null;
        }

        private static object AutoParse(string result)
        {
            if (int.TryParse(result, out int resultAsInt)) return resultAsInt;
            else if (float.TryParse(result, out float resultAsFloat)) return resultAsFloat;
            else return result;
        }

        static void CreateJSON(string parentDir, int location)
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
            string filename = parentDir+@"\spliceData.JSON";
            var options = new JsonSerializerOptions { WriteIndented = true };
            string serializedJSON = JsonSerializer.Serialize(unserializedJSON, options);
            File.WriteAllText(filename, serializedJSON);

        }

        static void StartingMessage()
        {   
            Console.Title = "RecordSplicingResults";
            Console.WriteLine("Splice data backup Wizard v1.0.0");
            Console.WriteLine();
            Console.WriteLine("This software connects to Fujikura FSM-100 series splicers to migrate splice "+
            "data to the cloud. Please do not press any buttons on the splicer or open/close the cover while backups are in process.");
        }

        static bool HandleUserInput()
        {
            // <summary>Returns true if the user wants to do the standard operation of backing up splice data,
            // otherwise handling other requests/invalid requests and returning false.<\summary>
            
            Console.WriteLine();
            Console.WriteLine("Enter [1] to backup splice data, [2] to backup splice mode settings, and [q] to quit:");
            string? response = Console.ReadLine();
            
            if (!SplicerResting()) return false;

            if (response == "1")
            {
                return true;
            }
            else if (response == "2")
            {
                
                Console.WriteLine("Backing up splice mode settings...");
                BackupUtils.Backup();
                Console.WriteLine("Backup successful.");
            }
            else if (response == "q")
            {
                Console.WriteLine("Quitting...");
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine($"Please enter [1], [2], or [q]. You entered: {response}.");
            }

            return false;
        }
        
        static void Main(string[] args)
        {
            
        
            StartingMessage();   
            while (true)
            {   
                if (!SplicerConnected()) continue;

                if (!HandleUserInput()) continue;

                // Splice data backup section
                string dirname = CreateNewSpliceDirectory();
                int location = (int) GetOutputAsDict("=MEMLATEST", ["MEMLATEST"], false)["MEMLATEST"];

                Console.WriteLine("Backing up images...");
                GetImages(dirname);

                Console.WriteLine("Backing up data...");
                CreateJSON(dirname, location);

                Console.WriteLine("Backing up settings...");
                BackupUtils.BackupSpecific(dirname, location);

            }
            
            /*
            catch (Exception e)
            {
                Console.WriteLine($"FATAL ERROR: {e.Message}. Please try again or contact support.");
            }
            */
        }
    }
}
    

    

