

namespace SplicingDataAcquisition
{
    
    class BackupUtils
    {
        public static byte[] GetSpliceParameters(int spliceMode)
        {
            if (spliceMode < 0 || spliceMode > 300)
            {
                throw new ArgumentOutOfRangeException(nameof(spliceMode), "Splice mode must be between 0 and 300.");
            }

            // TODO: add logging and error handling for communication issues
            return SplicerUtils.splicer.CommandAndReceiveBinary($"%SPLH-{spliceMode}");
        }

        public static void SendSpliceParameters(int spliceMode, byte[] parameters)
        {
            if (spliceMode < 0 || spliceMode > 300)
            {
                throw new ArgumentOutOfRangeException(nameof(spliceMode), "Splice mode must be between 0 and 300.");
            }

            if (parameters == null || parameters.Length == 0)
            {
                throw new ArgumentException("Parameters cannot be null or empty.", nameof(parameters));
            }

            // TODO: add logging and error handling for communication issues
            string response = SplicerUtils.splicer.CommandAndReceiveText($"#SPLH-{spliceMode}");
            SplicerUtils.splicer.SendBinary(ref parameters, parameters.Length, SplicerUtils.STD_TIMEOUT);
        }

    }
}