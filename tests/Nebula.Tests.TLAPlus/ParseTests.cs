namespace Nebula.Tests.TLAPlus;

using Nebula.Verifiers.TLAPlus;

public class SANYTests
{
    [Fact]
    public void CanParse()
    {
        var s = SANY.Parse(Path.Combine("testfiles", "HourClock.tla"));
        Assert.True(s.IsSuccess);
    }
}
