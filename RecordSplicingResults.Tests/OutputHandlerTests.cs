using System.Net.Mail;
using RecordSplicingResults;
using static RecordSplicingResults.OutputHandler;


namespace RecordSplicingResults.Tests;



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
        Assert.Equal(0.1, (float) x1);
        Assert.Equal(-1.5, (float) x2);
        Assert.Equal(0.3, (float) x3);
    
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
        Assert.Equal(1, (float) x1);
        Assert.Equal(-1, (float) x2);
        Assert.Equal(300, (float) x3);
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






}