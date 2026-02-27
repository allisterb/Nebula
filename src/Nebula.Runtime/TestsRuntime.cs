namespace Nebula;

using Microsoft.Extensions.Configuration;

using Serilog;
using Serilog.Extensions.Logging;

public class TestsRuntime : Runtime
{
    static TestsRuntime()
    {
        Runtime.WithFileAndConsoleLogging("Nebula", "Tests", true);
        config = LoadConfigFile("testappsettings.json");        
    }    
    static protected IConfigurationRoot config;
}

