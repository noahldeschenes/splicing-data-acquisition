
//using Utils;


namespace RecordSplicingResults
{
    /*
        Folder: date
        Files inside: JSON, images, settings, errors

    */
    public record Result(
        string FiberType,
        string Error,
        string SpliceMode,
        MainSpliceResults MainResults,
        SecondarySpliceResults SecondaryResults,
        OptionalSpliceData OptionalData
    );

    public record MainSpliceResults(
        float EstimatedLoss,
        float LeftCLeaveAngle,
        float RightCleaveAngle,
        float CoreOffset,
        float CladOffset
    );
    public record SecondarySpliceResults(
        float EstimatedOffsetLoss,
        float EstimatedDeformLoss,
        float EstimatedMfdMismatchLoss,
        float MeasuredGap,
        float CrosstalkPerDegree,
        float CrosstalkPerDB,
        float FiberAngle
    );

    public record OptionalSpliceData( //might not get
        float MainArcPower,
        float ArcTime,
        float AxisMovement
    );



    internal class Program
    {





        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

    }
}

