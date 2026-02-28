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
        Environment.SetEnvironmentVariable("AWS_BEARER_TOKEN_BEDROCK", GetRequiredValue(config, "Model:ApiKey"), EnvironmentVariableTarget.Process);
    }    
    static protected IConfigurationRoot config;
}

