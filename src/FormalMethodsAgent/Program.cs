
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using FormalMethodsAgent;


var builder = Kernel.CreateBuilder();

builder.AddAzureOpenAIChatCompletion(
    deploymentName: "gpt-4.1-mini",        
    endpoint: Environment.GetEnvironmentVariable("model-endpoint"),
    apiKey: Environment.GetEnvironmentVariable("model-key")
);

builder.Plugins.AddFromType<CoyoteSkill>();
builder.Plugins.AddFromType<DiskIOSkill>();
builder.Plugins.AddFromType<DotnetSkill>();

var kernel = builder.Build();

IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

ChatHistory chatMessages = new ChatHistory("""
You are a friendly assistant who likes to follow the rules. You will complete required steps
and request approval before taking any consequential actions. If the user doesn't provide
enough information for you to complete a task, you will keep asking questions until you have
enough information to complete the task.

Your primary task is to help the user find issues in his/her programs using formal methods tools.
When an issue is found, you will provide a detailed explanation of the issue from the formal method tool output.
""");

while (true)
{
    Console.Write("User > ");
    chatMessages.AddUserMessage(Console.ReadLine()!);

    OpenAIPromptExecutionSettings settings = new() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };
    var result = chatCompletionService.GetStreamingChatMessageContentsAsync(
        chatMessages,
        executionSettings: settings,
        kernel: kernel);

    Console.Write("Assistant > ");
    // Stream the results
    string fullMessage = "";
    await foreach (var content in result)
    {
        Console.Write(content.Content);
        fullMessage += content.Content;
    }
    Console.WriteLine("\n--------------------------------------------------------------");

    // Add the message from the agent to the chat history
    chatMessages.AddAssistantMessage(fullMessage);
}



