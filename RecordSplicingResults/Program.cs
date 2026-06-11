
using System.Text.Json;
using System.Text.RegularExpressions;
using Utils;
using System.Text.Json.Nodes;


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


        static void CreateJSON(int location, bool mostRecent)
        {
            
            var splicerInfo = GetSplicerOutputAsDict("=INF", SPLICER_INFO);
            var nonVolMem = GetSplicerOutputAsDict($"=MEM-{location}", NONVOLATILE_MEM);
            
            Dictionary<string, object> volMem;
            if (mostRecent) volMem = GetSplicerOutputAsDict("=DATH", VOLATILE_MEM);
            else volMem = new();

            var leftFiberInfo = GetSplicerOutputAsDict($"MEMSPL-{location}", LEFTFIBERINFO);
            var rightFiberInfo = GetSplicerOutputAsDict($"MEMSPL-{location}", RIGHTFIBERINFO);
            var mainArc = GetSplicerOutputAsDict($"MEMSPL-{location}", MAINARC);
            var estimation = GetSplicerOutputAsDict($"MEMSPL-{location}", ESTIMATION);

            
            
        }


    }
}

