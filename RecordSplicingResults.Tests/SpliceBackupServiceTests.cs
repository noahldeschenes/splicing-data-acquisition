


using RecordSplicingResults;
using static RecordSplicingResults.OutputHandler;
using static RecordSplicingResults.ParamService;
using static RecordSplicingResults.SpliceBackupService;


namespace RecordSplicingResults.Tests;


[Collection("Shared static state")]
public class SpliceBackupServiceTests
{
    [Fact]
    public static void GetNewSpliceDirectoryPath_MinDateTime_Succeeds()
    {
        string[] crtValidInputs = ["=INF|SERNUM", "%SPL-1|MODETITLE1"];
        string[] crtRetVals = ["SERNUM=1", "MODETITLE1=FLEX-SMF V2"];

        splicer = new MockUsbFsm100ServerClass(crtValidInputs, crtRetVals);
        MAIN_BACKUP_DIRECTORY = "~";

        string correctPath = @"~\Splice data backups\00001 (UNKNOWN)\FLEX-SMF V2\0001-01-01\00h00";

        Assert.Equal(correctPath, GetNewSpliceDirectoryPath(1, DateTime.MinValue));

    }


    [Fact]
    public static void GetNewSpliceDirectoryPath_MaxDateTime_Succeeds()
    {
        string[] crtValidInputs = ["=INF|SERNUM", "%SPL-12|MODETITLE1"];
        string[] crtRetVals = ["SERNUM=123", "MODETITLE1=XAN: RCBI-SMF V1"];

        splicer = new MockUsbFsm100ServerClass(crtValidInputs, crtRetVals);
        MAIN_BACKUP_DIRECTORY = "~";

        string correctPath = @"~\Splice data backups\00123 (UNKNOWN)\XAN RCBI-SMF V1\9999-12-31\23h59";

        Assert.Equal(correctPath, GetNewSpliceDirectoryPath(12, DateTime.MaxValue));

    }
}