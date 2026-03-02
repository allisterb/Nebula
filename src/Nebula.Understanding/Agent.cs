namespace Nebula;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;

using Amazon.BedrockRuntime;

public class Agent : Runtime
{
    public Agent(string modelId, string? embeddingModelId = null, string? instructions = null, params AITool[]? tools) : base()
    {
        this.modelId = modelId;
        this.embeddingModelId = embeddingModelId;
        bedrockClient = new AmazonBedrockRuntimeClient(region: Amazon.RegionEndpoint.USEast2);
        ChatClientBuilder builder = new ChatClientBuilder(bedrockClient.AsIChatClient(this.modelId));
        builder
            .UseLogging(loggerFactory)
            .UseFunctionInvocation(loggerFactory);                                              
        chatClient = builder.Build();
        agent = chatClient.AsAIAgent(instructions, loggerFactory: loggerFactory);        
        Info("Using Amazon Bedrock model {0}.", this.modelId);       
    }

    #region Properties
    public IReadOnlyList<IPlugin> Plugins => plugins;
    #endregion

    #region Methods

    
    #endregion

    #region Fields
    public readonly string modelId;

    public readonly string? embeddingModelId;

    public readonly AmazonBedrockRuntimeClient bedrockClient;
    
    public readonly IChatClient chatClient;

    public readonly ChatClientAgent agent;

    protected List<IPlugin> plugins = new List<IPlugin>();

    protected string[]? systemPrompts;

    public static IConfigurationRoot? config = null;
    #endregion

    #region Types
    public record ModelIds
    {
        public const string NovaLite = "amazon.nova-lite-v1:0";
        public const string NovaPro = "amazon.nova-pro-v1:0";
    }
    #endregion
}
