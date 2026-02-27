namespace Nebula;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Amazon;
using Amazon.BedrockRuntime;

public class ModelConversation : Runtime
{
    public ModelConversation(string modelId, string? embeddingModelId = null, string[]? systemPrompts = null, params IPlugin[]? plugins) : base()
    {
        this.modelId = modelId;
        this.embeddingModelId = embeddingModelId;
        this.systemPrompts = systemPrompts;                    
        ChatClientBuilder builder = new ChatClientBuilder(bedrockClient.AsIChatClient("ll"));
        builder
            .UseLogging(loggerFactory)
            .UseFunctionInvocation(loggerFactory);                                  
        chatClient = builder.Build();
       
        Info("Using Google Gemini model {0}.", this.modelId);
       
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
                else if (item is Image image)
                {
                    if (image.ImageBytes is not null)
                    {
                        messageItems.Add(new ImageContent(image.ImageBytes, image.MimeType ?? "image/png"));
                    }
                    else if (image.GcsUri is not null)
                    {
#pragma warning disable SYSLIB0014 // Type or member is obsolete
                        var wc = new System.Net.WebClient();
#pragma warning restore SYSLIB0014 // Type or member is obsolete
                        var data = wc.DownloadData(image.GcsUri);
                        {
                            messageItems.Add(new ImageContent(image.ImageBytes, image.MimeType ?? "image/png"));
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Image content must have either ImageBytes or GcsUri.");
                    }
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

    public readonly AmazonBedrockRuntimeClient bedrockClient = new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1);

    public readonly IChatClient chatClient ;

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
