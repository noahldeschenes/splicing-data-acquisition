using RecordSplicingResults;

namespace RecordSplicingResults.Tests;

public class ParamServiceTests
{
    [Fact]
    public void SetSpliceParameters_WithOutOfRangeMode_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ParamService.SetSpliceParameters(0, []));

        Assert.Equal("spliceMode", ex.ParamName);
    }
}
