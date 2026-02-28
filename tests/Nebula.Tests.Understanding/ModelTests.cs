namespace Nebula.Tests.Understanding;

using Nebula;
public class ModelTests : TestsRuntime
{
    [Fact]
    public async Task CanConnectToNovaLite()
    {
        var mc = new ModelConversation(modelId: ModelConversation.ModelIds.NovaLite);
        var r = await mc.PromptAsync("Hello who are you?");
        Assert.NotEmpty(r);
    }
}
