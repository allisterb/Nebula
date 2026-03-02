using Nebula.Verifiers.TLAPlus;

namespace Nebula.Tests.TLAPlus;

public class UnitTest1
{
    [Fact]
    public void CanParse()
    {
        var s = SANY.Parse(Path.Combine("testfiles", "HourClock.tla"));
        Assert.True(s.IsSuccess);
    }
}
