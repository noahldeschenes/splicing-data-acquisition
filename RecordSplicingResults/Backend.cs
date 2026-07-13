
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Spectre.Console;
using Utils;
using System.Threading;



namespace RecordSplicingResults
{
    /*
        Folder: date
        Files inside: JSON, images, errors


        JSON format: Splicer info, Relevant splice settings, Volatile memory splice data, 
        Non-volatile memory splice data

    */


    class Backend
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


        public static readonly string RECORDS_DIRECTORY_PATH = @"C:\Users\noah.deschenes\Documents\Records"; // TODO: find directory
        
        public const char NAK = '\x15'; // ASCII code for NAK
        public static UsbFsm100ServerClass splicer = new();
        

        static bool SplicerResting()
        {
            // <summary>Checks if the splicer is at a valid state for queries.</summary>

            string currentStatus = splicer.CommandAndReceiveText("=FUNCSTAT");
            if (currentStatus != "IDLE" && currentStatus != "ERRFIN" && currentStatus != "NOFIN") 
            {
                AnsiConsole.WriteLine($"ERROR: Splicer is not at the READY or FINISH state (currently at '{currentStatus}' state).");
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
                for (int i = 0; i < 256; i++) pal.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
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

        public static void BackupLastSplice()
        {
            string dirname = CreateNewSpliceDirectory();
            int location = (int) GetOutputAsDict("=MEMLATEST", ["MEMLATEST"], false)["MEMLATEST"];
            AnsiConsole.Status()
                .Start("[blue]Backing up data...[/]", ctx =>
                {
                    CreateJSON(dirname, location);
                    Thread.Sleep(100);
                    AnsiConsole.MarkupLine("Data backed up.");
                });
            AnsiConsole.Status()
                .Start("[blue]Backing up images...[/]", ctx =>
                {
                    GetImages(dirname);
                    Thread.Sleep(500);
                    AnsiConsole.MarkupLine("Images backed up.");
                });

            AnsiConsole.Status()
                .Start("[blue]Backing up settings...[/]", ctx =>
                {
                    BackupUtils.BackupSpecific(dirname, location);
                    Thread.Sleep(500);
                    AnsiConsole.MarkupLine("Settings backed up.");
                });
        }
    }
}
    

    

