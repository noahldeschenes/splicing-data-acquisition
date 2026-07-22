

using RecordSplicingResults;
using static RecordSplicingResults.OutputHandler;
using static RecordSplicingResults.ParamService;
using static RecordSplicingResults.SpliceBackupService;


namespace RecordSplicingResults.Tests;


public class ParamServiceTests
{
    [Fact]
    public static void GetNewParameterBackupPath_MinDateTime_Succeeds()
    {

        string[] crtValidInputs = ["=INF|SERNUM"];
        string[] crtRetVals = ["SERNUM=1"];

        splicer = new MockUsbFsm100ServerClass(crtValidInputs, crtRetVals, [], [], []);
        MAIN_BACKUP_DIRECTORY = "~";

        string validPath = @"~\Splice mode parameter backups\00001 (UNKNOWN)\0001-01-01";

        Assert.Equal(validPath, GetNewParameterBackupPath(DateTime.MinValue));
        
    }
}