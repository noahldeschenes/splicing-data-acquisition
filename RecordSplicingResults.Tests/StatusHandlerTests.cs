

using System.Reflection;
using RecordSplicingResults;
using static RecordSplicingResults.OutputHandler;
using static RecordSplicingResults.StatusHandler;


namespace RecordSplicingResults.Tests;

[Collection("Shared static state")]
public class StatusHandlerTests
{
    [Fact]
    public static void GetSplicerArcCount_EventualReturn_Succeeds()
    {
        string[] crtValidInputs = ["=INF|TARCCOUNT", "=INF|TARCCOUNT", "=INF|TARCCOUNT"];
        string[] crtRetVals = [NAK, NAK, "TARCCOUNT=1"];

        splicer = new MockUsbFsm100ServerClass(crtValidInputs, crtRetVals, [], [], []);

        Assert.Equal(1, GetSplicerArcCount(1));
    }

    [Fact]
    public static void WaitForNewSplice_EventualReturn_Succeeds()
    {
        string[] crtValidInputs = ["=INF|TARCCOUNT", "=INF|TARCCOUNT", "=INF|TARCCOUNT"];
        string[] crtRetVals = ["TARCCOUNT=1", "TARCCOUNT=1", "TARCCOUNT=2"];

        splicer = new MockUsbFsm100ServerClass(crtValidInputs, crtRetVals, [], [], []);

        GetSplicerArcCount(1);
    }


}