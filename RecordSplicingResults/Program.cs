
using System.Text.Json;
using System.Text.RegularExpressions;
using Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixImage = SixLabors.ImageSharp.Image;


namespace RecordSplicingResults
{
    /*
        Folder: date
        Files inside: JSON, images, settings, errors


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

        static int VGA_WIDTH = 640;
        static int VGA_HEIGHT = 480;


        static Dictionary<string, object> GetSplicerOutputAsDict(string query, string[] identifiers)
        {
            // <summary> Takes the output of the splicer and formats it into a dict, for eventual
            // conversion into a JSON. </summary>

            string splicerOutput = SplicerUtils.QuerySplicer(query, identifiers);

            Dictionary<string, object> pairs = new();

            string pattern = @"(?<identifier>[^|=]+)=(?<result>[^|]*)";  // input form: IDENTIFIER1=RESULT1|IDENTIFIER2=RESULT2|...   

            foreach (Match match in Regex.Matches(splicerOutput, pattern))
            {
                string id = match.Groups["identifier"].Value;
                string result = match.Groups["result"].Value; 

                // result can be a string, an int, or a float, so we auto-parse 
                // it for the sake of convenience/flexibility
                if (int.TryParse(result, out int resultAsInt)) pairs[id] = resultAsInt;
                else if (float.TryParse(result, out float resultAsFloat)) pairs[id] = resultAsFloat;
                else pairs[id] = result;

            }

            return pairs;
        }


        static void CreateJSON(int location, bool mostRecent, string parentDir)
        {
            
            Dictionary<string, object> unserializedJSON = new();

            // getting splicer and splice info 
            unserializedJSON["SPLICER_INFO"] = GetSplicerOutputAsDict("=INF", SPLICER_INFO);
            unserializedJSON["NONVOLATILE_MEM"] = GetSplicerOutputAsDict($"=MEM-{location}", NONVOLATILE_MEM);
            // can only get volatile memory data if this splice was the most recent splice
            if (mostRecent) unserializedJSON["VOLATILE_MEM"] = GetSplicerOutputAsDict("=DATH", VOLATILE_MEM);


            // getting settings info
            Dictionary<string, object> settings = new();
            unserializedJSON["SETTINGS"] = settings;
            settings["LEFTFIBERINFO"] = GetSplicerOutputAsDict($"MEMSPL-{location}", LEFTFIBERINFO);
            settings["RIGHTFIBERINFO"] = GetSplicerOutputAsDict($"MEMSPL-{location}", RIGHTFIBERINFO);
            settings["MAINARC"] = GetSplicerOutputAsDict($"MEMSPL-{location}", MAINARC);
            settings["ESTIMATION"] = GetSplicerOutputAsDict($"MEMSPL-{location}", ESTIMATION);
            

            // serializing JSON
            string filename = parentDir+"/spliceData.JSON";
            string serializedJSON = JsonSerializer.Serialize(unserializedJSON);
            File.WriteAllText(filename, serializedJSON);

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

        static void GetSettings(string parentDir)
        {
            // need to test if "=MEMSPLH" works for backing up
            // actually, maybe don't get settings but just check diffs as you go?
        }



    }
}

