
using Utils;

namespace RestoreSettings
{
    public static class Program
    {
        public const int MAX_MODENO = 300; // splicer has modes numbered 1-300

        

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



