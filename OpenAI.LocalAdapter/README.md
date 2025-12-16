# OpenAI.LocalAdapter - C# Library

A .NET library that provides OpenAI API compatibility for local LLM backends (LM Studio, Ollama, etc.). Works seamlessly with the OpenAI Local Proxy or directly with local backends.

## Features

- ✅ **OpenAI-Compatible API**: Drop-in replacement for OpenAI SDK
- ✅ **Conversation Management**: Similar to OpenAI_API's CreateConversation
- ✅ **Async/Await Throughout**: Modern async patterns
- ✅ **Streaming Support**: Real-time response streaming
- ✅ **Strongly Typed**: Full type safety with models
- ✅ **Multi-Target**: Supports .NET 7.0, .NET 6.0, and .NET Standard 2.1
- ✅ **Cancellation Support**: Proper cancellation token handling
- ✅ **Comprehensive Documentation**: XML docs on all public APIs

## Installation

### From NuGet (when published)
```bash
dotnet add package OpenAI.LocalAdapter
```

### From Source
```bash
cd OpenAI.LocalAdapter
dotnet build
dotnet pack
```

## Quick Start

### Basic Usage

```csharp
using OpenAI.LocalAdapter;
using OpenAI.LocalAdapter.Configuration;
using OpenAI.LocalAdapter.Models;

// Option 1: Using the proxy (recommended)
var client = new OpenAILocalClient("http://localhost:8080");

// Option 2: Direct LM Studio connection
var client = new OpenAILocalClient(
    LocalAdapterConfig.ForLMStudio("http://10.50.10.14:1234")
);

// Option 3: Custom configuration
var config = new LocalAdapterConfig
{
    BaseUrl = "http://localhost:8080",
    DefaultModel = "llama-3.1-instruct-13b",
    TimeoutSeconds = 300,
    MaxRetries = 3
};
var client = new OpenAILocalClient(config);
```

### Chat Completion

```csharp
// Simple chat completion
var request = new ChatCompletionRequest
{
    Model = "gpt-3.5-turbo",
    Messages = new List<ChatMessage>
    {
        ChatMessage.System("You are a helpful assistant"),
        ChatMessage.User("What is the capital of France?")
    },
    Temperature = 0.7,
    MaxTokens = 150
};

var response = await client.Chat.CreateCompletionAsync(request);
var answer = response.Choices[0].Message.Content;
Console.WriteLine(answer);
```

### Streaming Chat Completion

```csharp
var request = new ChatCompletionRequest
{
    Model = "gpt-3.5-turbo",
    Messages = new List<ChatMessage>
    {
        ChatMessage.User("Count to 10")
    },
    Stream = true
};

await foreach (var chunk in client.Chat.CreateCompletionStreamAsync(request))
{
    if (chunk.Choices?.Count > 0)
    {
        var content = chunk.Choices[0].Delta?.Content;
        if (content != null)
        {
            Console.Write(content);
        }
    }
}
```

### Conversation Management

```csharp
// Create a conversation (similar to OpenAI_API's CreateConversation)
var conversation = client.CreateConversation(
    model: "gpt-3.5-turbo",
    temperature: 0.7,
    maxTokens: 500
);

// Set up the conversation
conversation.AppendSystemMessage("You are a dungeon master for a D&D game.");
conversation.AppendUserInput("I want to start an adventure.");

// Get response
var response = await conversation.GetResponseAsync();
Console.WriteLine(response);

// Continue the conversation
conversation.AppendUserInput("I open the door.");
response = await conversation.GetResponseAsync();
Console.WriteLine(response);

// Get streaming response
conversation.AppendUserInput("What do I see?");
await foreach (var chunk in conversation.GetResponseStreamAsync())
{
    Console.Write(chunk);
}
Console.WriteLine();
```

## Migrating from OpenAI_API (OkGoDoIt)

### Before (OpenAI_API)
```csharp
using OpenAI_API;

string apiKeyFilePath = "apikey.txt";
string text = File.ReadAllText(apiKeyFilePath);
OpenAIAPI api = new OpenAIAPI(text);
var chat = api.Chat.CreateConversation();

chat.AppendSystemMessage("You are a dungeon master...");
chat.AppendUserInput("Begin the adventure");
string response = await chat.GetResponseFromChatbotAsync();
```

### After (OpenAI.LocalAdapter)
```csharp
using OpenAI.LocalAdapter;
using OpenAI.LocalAdapter.Configuration;

// Use the proxy (no API key needed)
var client = new OpenAILocalClient("http://localhost:8080");
var conversation = client.CreateConversation();

conversation.AppendSystemMessage("You are a dungeon master...");
conversation.AppendUserInput("Begin the adventure");
string response = await conversation.GetResponseAsync();
```

The API is almost identical! Main changes:
1. Change initialization to use `OpenAILocalClient`
2. Replace `GetResponseFromChatbotAsync()` with `GetResponseAsync()`
3. No API key file needed

## API Reference

### OpenAILocalClient

Main client class for interacting with local OpenAI-compatible APIs.

#### Constructors

```csharp
// Simple constructor with URL
OpenAILocalClient(string baseUrl, string? apiKey = null, ILogger? logger = null)

// Constructor with full configuration
OpenAILocalClient(LocalAdapterConfig config, HttpClient? httpClient = null, ILogger? logger = null)
```

#### Properties

- `IChatService Chat` - Chat completion service

#### Methods

- `IConversationManager CreateConversation(...)` - Creates a new conversation context

### ChatService

Service for chat completions.

#### Methods

```csharp
// Non-streaming completion
Task<ChatCompletionResponse> CreateCompletionAsync(
    ChatCompletionRequest request,
    CancellationToken cancellationToken = default)

// Streaming completion
IAsyncEnumerable<ChatCompletionResponse> CreateCompletionStreamAsync(
    ChatCompletionRequest request,
    CancellationToken cancellationToken = default)
```

### ConversationContext

Manages conversation state and provides a simple interface for multi-turn chats.

#### Methods

```csharp
void AppendSystemMessage(string message)
void AppendUserInput(string message)
void AppendAssistantResponse(string message)
Task<string> GetResponseAsync(CancellationToken cancellationToken = default)
IAsyncEnumerable<string> GetResponseStreamAsync(CancellationToken cancellationToken = default)
void Clear()
List<ChatMessage> GetMessages()
void RemoveLastMessage()
void RemoveLastMessages(int count)
```

### Models

#### ChatMessage

```csharp
public class ChatMessage
{
    public string Role { get; set; }
    public string? Content { get; set; }
    public string? Name { get; set; }
    
    // Helper methods
    static ChatMessage System(string content)
    static ChatMessage User(string content)
    static ChatMessage Assistant(string content)
}
```

#### ChatCompletionRequest

```csharp
public class ChatCompletionRequest
{
    public string Model { get; set; }
    public List<ChatMessage> Messages { get; set; }
    public double? Temperature { get; set; }
    public double? TopP { get; set; }
    public int? MaxTokens { get; set; }
    public bool? Stream { get; set; }
    public double? FrequencyPenalty { get; set; }
    public double? PresencePenalty { get; set; }
    // ... more properties
}
```

#### ChatCompletionResponse

```csharp
public class ChatCompletionResponse
{
    public string Id { get; set; }
    public string Object { get; set; }
    public long Created { get; set; }
    public string Model { get; set; }
    public List<ChatChoice> Choices { get; set; }
    public UsageInfo? Usage { get; set; }
}
```

## Configuration

### LocalAdapterConfig

```csharp
public class LocalAdapterConfig
{
    public string BaseUrl { get; set; } = "http://localhost:8080";
    public string? ApiKey { get; set; }
    public string DefaultModel { get; set; } = "llama-3.1-instruct-13b";
    public int TimeoutSeconds { get; set; } = 300;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public bool ValidateSsl { get; set; } = true;
    public BackendProvider Provider { get; set; } = BackendProvider.Proxy;
    
    // Helper factory methods
    static LocalAdapterConfig ForLMStudio(string url = "http://10.50.10.14:1234")
    static LocalAdapterConfig ForProxy(string url = "http://localhost:8080")
    static LocalAdapterConfig ForOllama(string url = "http://localhost:11434")
}
```

### BackendProvider Enum

```csharp
public enum BackendProvider
{
    Proxy,      // OpenAI Local Proxy (recommended)
    LMStudio,   // Direct LM Studio connection
    Ollama,     // Direct Ollama connection
    LocalAI,    // Direct LocalAI connection
    Custom      // Custom backend
}
```

## Advanced Usage

### Cancellation

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var response = await client.Chat.CreateCompletionAsync(request, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Request was cancelled");
}
```

### Custom HTTP Client

```csharp
var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(10)
};

var client = new OpenAILocalClient(config, httpClient);
```

### Logging

```csharp
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
});

var logger = loggerFactory.CreateLogger<OpenAILocalClient>();
var client = new OpenAILocalClient(config, logger: logger);
```

### Error Handling

```csharp
try
{
    var response = await client.Chat.CreateCompletionAsync(request);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"API request failed: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Invalid operation: {ex.Message}");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid argument: {ex.Message}");
}
```

## Examples

See the `/Examples` directory for complete examples:

- **QuickStart.cs** - Basic usage
- **ConversationExample.cs** - Multi-turn conversations
- **StreamingExample.cs** - Streaming responses
- **TextRPGMigration.cs** - Migration guide for text-rpg-adventure

## Performance Tips

1. **Reuse HttpClient**: The library creates an HttpClient by default, but you can pass your own for better connection pooling
2. **Use Streaming**: For long responses, streaming provides better user experience
3. **Set Appropriate Timeouts**: Large models may take longer; adjust `TimeoutSeconds` accordingly
4. **Connection Pooling**: The proxy handles this automatically

## Troubleshooting

### Connection Refused

**Problem**: Cannot connect to backend

**Solution**:
```csharp
// Verify the URL is correct
var config = LocalAdapterConfig.ForProxy("http://localhost:8080");

// Check proxy is running
// curl http://localhost:8080/health
```

### Timeout Errors

**Problem**: Requests timeout

**Solution**:
```csharp
var config = new LocalAdapterConfig
{
    BaseUrl = "http://localhost:8080",
    TimeoutSeconds = 600  // Increase timeout
};
```

### SSL Certificate Errors

**Problem**: SSL validation fails for local development

**Solution**:
```csharp
var config = new LocalAdapterConfig
{
    BaseUrl = "https://localhost:8443",
    ValidateSsl = false  // Only for local development!
};
```

## Contributing

Contributions welcome! Please ensure:

1. Code follows C# conventions
2. XML documentation on public APIs
3. Tests pass: `dotnet test`
4. No breaking changes to public API

## Requirements

- .NET 7.0, .NET 6.0, or .NET Standard 2.1
- OpenAI Local Proxy running (or direct backend access)

## License

MIT License - See repository root for details

## Related Components

- **Python Proxy**: See `/openai-local-proxy/` for the HTTP proxy
- **Migration Guide**: See `/docs/MIGRATION_GUIDE.md`
- **Examples**: See `/examples/` directory
