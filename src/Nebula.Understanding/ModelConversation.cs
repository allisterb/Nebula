namespace Nebula;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Amazon;

using Amazon.BedrockRuntime;
using System.Net.Http.Headers;
using Amazon.Runtime;
using Amazon.Util;

public class ModelConversation : Runtime
{
    static ModelConversation()
    {
        if (config is not null)
        {
            Environment.SetEnvironmentVariable("AWS_BEARER_TOKEN_BEDROCK", config["ApiKey"], EnvironmentVariableTarget.Process);
        }
    }
    
    public ModelConversation(string modelId, string? embeddingModelId = null, string[]? systemPrompts = null, params AITool[]? aITools) : base()
    {
        this.modelId = modelId;
        this.embeddingModelId = embeddingModelId;
        this.systemPrompts = systemPrompts;
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.Services.AddLogging(builder =>
            builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddProvider(loggerProvider)
            );
        //var apiKey = config?["Model:ApiKey"] ?? throw new Exception();
        //var cred = new EnvironmentVariablesAWSCredentials();
        bedrockRuntimeClient = new AmazonBedrockRuntimeClient(region: Amazon.RegionEndpoint.USEast2);

        chatClient = bedrockRuntimeClient.AsIChatClient(ModelIds.NovaLite);
        chat = chatClient.AsChatCompletionService();
        if (this.embeddingModelId is not null)
        {
            builder.AddBedrockEmbeddingGenerator(this.embeddingModelId, bedrockRuntimeClient);
        }
        promptExecutionSettings = new PromptExecutionSettings
        {
            ModelId = this.modelId,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: true),                       
        };
        Info("Using Amazon Bedrock model {0}.", this.modelId);
        builder.Services
            .AddChatClient(chatClient)
            .UseFunctionInvocation(loggerFactory)
            .UseKernelFunctionInvocation(loggerFactory);
        kernel = builder.Build();

        if (systemPrompts is not null)
        {
            foreach (var systemPrompt in systemPrompts)
            {
                messages.AddSystemMessage(systemPrompt);
            }
        }

        if (plugins is not null)
        {
            foreach (var plugin in plugins)
            {                
                kernel.Plugins.AddFromObject(plugin, plugin.Name);
                this.plugins.Add(plugin);
            }
        }
    }

    #region Properties
    public IReadOnlyList<IPlugin> Plugins => plugins;
    #endregion

    #region Methods

    public ModelConversation AddPlugin<T>(string pluginName)
    {
        kernel.Plugins.AddFromType<T>(pluginName);
        return this;
    }

    public ModelConversation AddPlugin<T>(T obj, string pluginName)
    {
        kernel.Plugins.AddFromObject<T>(obj, jsonSerializerOptions: new System.Text.Json.JsonSerializerOptions(), pluginName: pluginName);
        return this;
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> StreamingPromptAsync(string prompt)
    {
        var messageItems = new ChatMessageContentItemCollection()
        {
            new Microsoft.SemanticKernel.TextContent(prompt)
        };
        messages.AddUserMessage(messageItems);
        StringBuilder sb = new StringBuilder();
        await foreach (var m in chat.GetStreamingChatMessageContentsAsync(messages, promptExecutionSettings, kernel))
        {
            if (m.Content is not null && !string.IsNullOrEmpty(m.Content))
            {
                sb.Append(m.Content);
                yield return m;
            }
        }
        messages.AddAssistantMessage(sb.ToString());
    }

    public async Task<List<ChatMessageContent>> PromptAsync(string prompt, params object[] content)
    {
        var messageItems = new ChatMessageContentItemCollection()
        {
            new Microsoft.SemanticKernel.TextContent(prompt)
        };
        if (content is not null)
        {
            foreach (var item in content)
            {
                if (item is string s)
                {
                    messageItems.Add(new Microsoft.SemanticKernel.TextContent(s));
                }
                else if (item is byte[] b)
                {
                    messageItems.Add(new ImageContent(b, "image/png"));
                }                
                else
                {
                    throw new ArgumentException($"Unsupported content type {item.GetType()}");
                }
            }
        }
        messages.AddUserMessage(messageItems);
        List<ChatMessageContent> response = new List<ChatMessageContent>();      
        foreach(var m in await chat.GetChatMessageContentsAsync(messages, promptExecutionSettings, kernel))
        {
            response.Add(m);
            messages.Add(m);
        }
        return response;
    }

    public async Task<List<ChatMessageContent>> ImagePromptAsync(string prompt, byte[] imageData, string imageMimeType = "image/png") 
    {
        messages.AddUserMessage([
            new Microsoft.SemanticKernel.TextContent(prompt),
            new ImageContent(imageData, imageMimeType),
        ]);
        List<ChatMessageContent> response = new List<ChatMessageContent>();
        foreach (var m in await chat.GetChatMessageContentsAsync(messages, promptExecutionSettings, kernel))
        {
            response.Add(m);
            messages.Add(m);
        }
        return response;
    }

    public void ResetContext()
    {
        messages.Clear();
        if (systemPrompts is not null)
        {
            foreach (var systemPrompt in systemPrompts)
            {
                messages.AddSystemMessage(systemPrompt);
            }
        }
    }
    #endregion

    #region Fields
    public readonly string modelId;

    public readonly string? embeddingModelId;

    public readonly AmazonBedrockRuntimeClient bedrockRuntimeClient;

    public readonly Kernel kernel; 

    public readonly IChatClient chatClient;

    public readonly IChatCompletionService chat;

    public readonly ChatHistory messages = new ChatHistory();

    public readonly PromptExecutionSettings promptExecutionSettings;

    protected List<IPlugin> plugins = new List<IPlugin>();

    protected string[]? systemPrompts;

    public static IConfigurationRoot? config = null;
    #endregion

    #region Types
    public record ModelIds
    {
        public const string NovaLite = "us.amazon.nova-2-lite-v1:0";
        public const string NovaPro = "us.amazon.nova-pro-v1:0";
    }
    #endregion
}
