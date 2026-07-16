
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Spectre.Console;
using System.Threading;



namespace RecordSplicingResults
{
    /*
        Folder: date
        Files inside: JSON, images, errors


        JSON format: Splicer info, Relevant splice settings, Volatile memory splice data, 
        Non-volatile memory splice data

    */


    class SplicerUtils
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
        public const int NUM_OF_MODES = 300; 
        public const string NAK = "\x15"; // ASCII code for NAK
        public static UsbFsm100ServerClass splicer = new();

        public static bool continuousModeOn = false;

        public static byte[] GetSpliceParameters(int spliceMode)
        {

            // <summary> Gets binary image of a given splice mode's parameters. </summary>

            byte[] parameters = splicer.CommandAndReceiveBinary($"%SPLH-{spliceMode}"); // assuming spliceMode is in range 1-300
            return parameters;
        }

        public static void SetSpliceParameters(int spliceMode, byte[] parameters)
        {

            if (spliceMode < 1 || spliceMode > MAX_MODENO)
            {
                throw new ArgumentOutOfRangeException(nameof(spliceMode), "Splice mode must be between 1 and 300.");
            }

            string response = SplicerUtils.splicer.CommandAndReceiveText($"#SPLH-{spliceMode}");
            splicer.SendBinary(parameters);

        }

        public static bool SplicerResting(bool verbose=true)
        {
            // <summary>Checks if the splicer is at a valid state for queries.</summary>

            string currentStatus = splicer.CommandAndReceiveText("=FUNCSTAT");
            if (currentStatus != "IDLE" && currentStatus != "ERRFIN" && currentStatus != "NOFIN") 
            {
                if (verbose)
                {
                    AnsiConsole.WriteLine($"ERROR: Splicer is not at the READY or FINISH state (currently at '{currentStatus}' state).");
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void SplicerConnected()
        {   
            // <summary> Initializes the driver and checks if the splicer is connected. </summary>

            string message1 = "\n[red]ERROR[/]: Splicer disconnected. Try disconnecting and reconnecting the usb cable between "+
            "the splicer and the computer.";
            string message2 = "\n[red]ERROR[/]: Splicer still disconnected. Try turning the splicer off and back on.";
            string message3 = "\n[red]FATAL ERROR[/]: Splicer repeatedly not connecting. Drivers may be dysfunctional. Exiting...";

            string[] messages = {"", message1, message2, message3};

            foreach (string msg in messages)
            {
                if (SplicerUtils.splicer.ConnectionStatus) 
                {
                    AnsiConsole.MarkupLine("Splicer connected...");
                    return;
                }

                SplicerUtils.splicer.InitDriver(Process.GetCurrentProcess().Handle);
                if (msg == "") continue; // first iteration is just to check if we need to initialize the driver

                AnsiConsole.MarkupLine(msg);
                AnsiConsole.Prompt(
                    new TextPrompt<string>("Press [green][[Enter]][/] to try again...")
                        .AllowEmpty());
            }
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
                BackupUtils.SaveBMP(imgX, dirname+@$"\{id}-X.png");

                // getting image in Y view
                byte[] imgY = splicer.CommandAndReceiveBinary($"=IMGH-{id}-Y");
                BackupUtils.SaveBMP(imgY, dirname+@$"\{id}-Y.png");

            }
        }

        private static object AutoParse(string result)
        {
            if (int.TryParse(result, out int resultAsInt)) return resultAsInt;
            else if (float.TryParse(result, out float resultAsFloat)) return resultAsFloat;
            else return result;
        }

        private static object? GetSpecificResultFromId(string splicerOutput, string id)
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

            foreach (string id in identifiers) pairs[id] = GetSpecificResultFromId(splicerOutput, id);

            return pairs;
        }

        public static object GetSingleResult(string query, string identifier, bool concatenate=false)
        {
            return GetOutputAsDict(query, [identifier], concatenate)[identifier];
        }

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
    }
}
    

    

