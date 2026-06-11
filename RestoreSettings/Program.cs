
using Utils;

namespace RestoreSettings
{
    public static class Program
    {
        public const int MAX_MODENO = 300; // splicer has modes numbered 1-300

        private static void SetSpliceParameters(int spliceMode, byte[] parameters)
        {

            if (spliceMode < 1 || spliceMode > MAX_MODENO)
            {
                throw new ArgumentOutOfRangeException(nameof(spliceMode), "Splice mode must be between 1 and 300.");
            }

            string response = SplicerUtils.splicer.CommandAndReceiveText($"#SPLH-{spliceMode}");
            SplicerUtils.QuitIfNAK(response);
            SplicerUtils.splicer.SendBinary(parameters);

        }

        public static void Restore(string path)
        {

            // checking backup is formatted correctly
            for (int i=1; i<MAX_MODENO+1; i++)
            {
                string id = $"{i}".PadLeft(3, '0');
                string filePath = path+@"\"+id;
                if (!File.Exists(filePath)) throw new FileNotFoundException($"File for splice mode {i} not found at {filePath}.");
                if (new FileInfo(filePath).Length != 4100) throw new InvalidDataException($"File for splice mode {i} is not 4100 bytes.");
            }

            // restoring splice modes
            for (int i=1; i<MAX_MODENO+1; i++)
            {
                string id = $"{i}".PadLeft(3, '0');
                string filePath = path+@"\"+id;
                byte[] parameters = File.ReadAllBytes(filePath);
                SetSpliceParameters(i, parameters);
            }
        }

        public static void Main(string[] args)
        {
            SplicerUtils.InitializeAndLock();
            
            try { Restore(args[0]); }
            catch (Exception e)
            {
                using (StreamWriter sw = File.AppendText(SplicerUtils.LOG_FILE_LOCATION))
                {
                    sw.WriteLine($"Error during restore at {DateTime.UtcNow}: {e.Message}");
                }
            }

            SplicerUtils.splicer.Command("$UNLOCK");
        }
    }
}



