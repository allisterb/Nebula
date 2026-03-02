namespace Nebula.Tests.TLAPlus;

using Nebula.Verifiers.TLAPlus;
using System;
using System.Collections.Generic;
using System.Text;



public class ToolsTests
{
    [Fact]
    public void CanParse()
    {
        TLA2Tools.SANY(Path.Combine("testfiles", "HourClock.tla"));
    }
}
