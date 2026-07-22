
using RecordSplicingResults;
using static RecordSplicingResults.OutputHandler;


namespace RecordSplicingResults.Tests;


[Collection("Shared static state")]
public class OutputHandlerTests
{
    [Fact]
    public void AutoParse_WithFloatValue_ParsesFloat()
    {
        object x1 = AutoParse("0.1");
        object x2 = AutoParse("-1.5");
        object x3 = AutoParse("0.3 dB");

        Assert.True(x1 is float);
        Assert.True(x2 is float);
        Assert.True(x3 is float);
        Assert.True(Math.Abs(0.1-(float) x1)<0.00001);
        Assert.True(Math.Abs(-1.5-(float) x2)<0.00001);
        Assert.True(Math.Abs(0.3-(float)x3)<0.00001);
    
    }

    [Fact]
    public void AutoParse_WithIntValue_ParsesInt()
    {
        object x1 = AutoParse("1");
        object x2 = AutoParse("-1");
        object x3 = AutoParse("300 bit");

        Assert.True(x1 is int);
        Assert.True(x2 is int);
        Assert.True(x3 is int);
        Assert.Equal(1, (int) x1);
        Assert.Equal(-1, (int) x2);
        Assert.Equal(300, (int) x3);
    }

    [Fact]
    public void AutoParse_WithStringValue_ParsesString()
    {
        object x1 = AutoParse("abcd");
        object x2 = AutoParse("");
        object x3 = AutoParse("a1b2.3");

        Assert.True(x1 is string);
        Assert.True(x2 is string);
        Assert.True(x3 is string);
        Assert.Equal("abcd", (string) x1);
        Assert.Equal("", (string) x2);
        Assert.Equal("a1b2.3", (string) x3);
    }

    [Fact]
    public void GetSpecificResultFromId_WithNAK_ReturnsNull()
    {
        object? x = GetSpecificResultFromId(NAK, "MODETITLE1");
        Assert.Null(x);
    }
    [Fact]
    public void GetSpecificResultFromId_NoMatches_ReturnsNull()
    {
        object? x1 = GetSpecificResultFromId("MODETITLE2=SMF-28|SERNUM=12345", "MODETITLE1");
        object? x2 = GetSpecificResultFromId("", "MODETITLE1");
        object? x3 = GetSpecificResultFromId("MODETITLE2=MODETITLE1", "MODETITLE1");
        
        Assert.Null(x1);
        Assert.Null(x2);
        Assert.Null(x3);
    }

    [Fact]
    public void GetSpecificResultFromId_WithMatches_ReturnsMatch()
    {
        object? x1 = GetSpecificResultFromId("SERNUM=12345|MODETITLE1=SMF-28", "MODETITLE1");
        object? x2 = GetSpecificResultFromId("MODETITLE1=", "MODETITLE1");

        Assert.Equal("SMF-28", (string?) x1);
        Assert.Equal("", (string?) x2);
    }

    [Fact]
    public void GetSingleResult_NeedsConcatenate_Succeeds()
    {   

        string[] crtValidInputs = ["=INF|SERNUM"];
        string[] crtRetVals = ["SERNUM=1"];

        splicer = new MockUsbFsm100ServerClass(crtValidInputs, crtRetVals);
        
        int serialNum = (int) GetSingleResult("=INF", "SERNUM", true);

        Assert.Equal(1, serialNum);
        
    }

    [Fact]
    public void GetSingleResult_NeedsConcatenate_RaisesException()
    {   

        string[] crtValidInputs = ["=INF"];
        string[] crtRetVals = [NAK];

        splicer = new MockUsbFsm100ServerClass(crtValidInputs, crtRetVals);

        Assert.Throws<SplicerQueryFailedException>(() => GetSingleResult("=INF", "SERNUM", false));
        
    }

    [Fact]
    public void GetOutputAsDict_NeedsConcatenate_Succeeds()
    {

        string[] splicerInfo = ["MODELNAME", "SERNUM", "TARCCOUNT"];

        string[] crtValidInputs = ["=INF|MODELNAME|SERNUM|TARCCOUNT"];
        string[] crtRetVals = ["MODELNAME=FOO|SERNUM=1|TARCCOUNT=2"];

        splicer = new MockUsbFsm100ServerClass(crtValidInputs, crtRetVals);


        var dict = new Dictionary<string, object>
        {
            ["MODELNAME"] = "FOO",
            ["SERNUM"] = 1,
            ["TARCCOUNT"] = 2
        };

        Assert.Equivalent(dict, GetOutputAsDict("=INF", splicerInfo, true));
    }
    
    [Fact]
    public void GetOutputAsDict_NeedsConcatenate_Fails()
    {
        string[] splicerInfo = ["MODELNAME", "SERNUM", "TARCCOUNT"];

        string[] crtValidInputs = ["=INF"];
        string[] crtRetVals = [NAK];

        splicer = new MockUsbFsm100ServerClass(crtValidInputs, crtRetVals);


        var dict = new Dictionary<string, object>
        {
            ["MODELNAME"] = "",
            ["SERNUM"] = "",
            ["TARCCOUNT"] = ""
        };

        Assert.Equivalent(dict, GetOutputAsDict("=INF", splicerInfo, false));
    }

    [Fact]
    public void GetOutputAsDict_NeedsNoConcatenate_Succeeds()
    {
        string[] mem = ["ESTLOSS", "ESTOFFSETLOSS", "ESTDEFORMLOSS"];

        string[] crtValidInputs = ["=MEM-1"];
        string[] crtRetVals = ["ESTLOSS=1.1|ESTOFFSETLOSS=0.5|GAP=4|ESTDEFORMLOSS=0.6"];

        splicer = new MockUsbFsm100ServerClass(crtValidInputs, crtRetVals);
        var dict = new Dictionary<string, object>
        {
            ["ESTLOSS"] = 1.1,
            ["ESTOFFSETLOSS"] = 0.5,
            ["ESTDEFORMLOSS"] = 0.6
        };

        Assert.Equivalent(dict, GetOutputAsDict("=MEM-1", mem, false));
    }

    [Fact]
    public void GetOutputAsDict_NeedsNoConcatenate_Fails()
    {
        string[] mem = ["ESTLOSS", "ESTOFFSETLOSS", "ESTDEFORMLOSS"];

        string[] crtValidInputs = ["=MEM-1|ESTLOSS|ESTOFFSETLOSS|ESTDEFORMLOSS"];
        string[] crtRetVals = [NAK];

        splicer = new MockUsbFsm100ServerClass(crtValidInputs, crtRetVals);
        var dict = new Dictionary<string, object>
        {
            ["ESTLOSS"] = "",
            ["ESTOFFSETLOSS"] = "",
            ["ESTDEFORMLOSS"] = ""
        };

        Assert.Equivalent(dict, GetOutputAsDict("=MEM-1", mem, true));
    }
}