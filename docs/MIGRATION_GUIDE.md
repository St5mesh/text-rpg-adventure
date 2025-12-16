# Migration Guide: OpenAI to Local LLM Backend

This guide helps you migrate existing applications from OpenAI API to local LLM backends using the OpenAI Local Proxy and/or the OpenAI.LocalAdapter C# library.

## Table of Contents

1. [Quick Start (5 Minutes)](#quick-start-5-minutes)
2. [Migration Strategies](#migration-strategies)
3. [Text RPG Adventure Migration](#text-rpg-adventure-migration)
4. [General C# Application Migration](#general-c-application-migration)
5. [Python Application Migration](#python-application-migration)
6. [Common Patterns](#common-patterns)
7. [Troubleshooting](#troubleshooting)

---

## Quick Start (5 Minutes)

### Step 1: Start the Proxy

```bash
cd openai-local-proxy
pip install -r requirements.txt
python proxy_server.py
```

The proxy will start on `http://localhost:8080`

### Step 2: Start LM Studio

1. Start LM Studio on your machine
2. Load your preferred model (e.g., llama-3.1-instruct-13b)
3. Enable the local server at `http://10.50.10.14:1234`

### Step 3: Test the Setup

```bash
curl http://localhost:8080/health
curl http://localhost:8080/v1/models
```

### Step 4: Update Your Application

**C# with OpenAI_API package:**
```csharp
// Before
OpenAIAPI api = new OpenAIAPI(apiKey);

// After
OpenAIAPI api = new OpenAIAPI("not-needed");
api.ApiUrlFormat = "http://localhost:8080/{0}/{1}";
```

**Or use OpenAI.LocalAdapter:**
```csharp
using OpenAI.LocalAdapter;
var client = new OpenAILocalClient("http://localhost:8080");
```

---

## Migration Strategies

### Strategy 1: Zero-Code-Change (Using Proxy Only)

**Best for:** Existing applications with minimal time for refactoring

**Steps:**
1. Start the proxy server
2. Change only the base URL in your application
3. Keep existing OpenAI API calls unchanged

**Pros:**
- No code changes required
- Works with any OpenAI SDK
- Easy rollback

**Cons:**
- Extra network hop (minimal overhead)
- Limited control over backend behavior

### Strategy 2: Direct Backend (Minimal Proxy)

**Best for:** Performance-critical applications

**Steps:**
1. Configure application to connect directly to LM Studio
2. Handle model name mapping in application
3. Implement error handling for backend differences

**Pros:**
- Lowest latency
- Direct control

**Cons:**
- May require code changes
- Less abstraction

### Strategy 3: Native Library (OpenAI.LocalAdapter)

**Best for:** New C# projects or major refactors

**Steps:**
1. Install OpenAI.LocalAdapter package
2. Refactor to use new API (similar to OpenAI_API)
3. Use conversation management features

**Pros:**
- Type-safe
- Better performance
- Built-in conversation management
- No external dependencies

**Cons:**
- More code changes required
- C# only

---

## Text RPG Adventure Migration

### Current Architecture

The game currently uses:
- OpenAI_API (OkGoDoIt) package
- File-based API key storage
- Conversation-based chat
- Multiple concurrent API calls

### Migration Path: Zero-Code-Change

**Before:**
```csharp
using OpenAI_API;

string apikeyFilePath = "apikey.txt";
string text = File.ReadAllText(apikeyFilePath);
OpenAIAPI api = new OpenAIAPI(text);
var chat = api.Chat.CreateConversation();

chat.AppendSystemMessage("You are a dungeon master for a D&D game...");
chat.AppendUserInput("Begin the adventure");
string response = await chat.GetResponseFromChatbotAsync();
```

**After (Option A - Using Proxy with OpenAI_API):**
```csharp
using OpenAI_API;

// Create API instance with proxy URL
OpenAIAPI api = new OpenAIAPI("not-needed");
api.ApiUrlFormat = "http://localhost:8080/{0}/{1}";
var chat = api.Chat.CreateConversation();

chat.AppendSystemMessage("You are a dungeon master for a D&D game...");
chat.AppendUserInput("Begin the adventure");
string response = await chat.GetResponseFromChatbotAsync();
```

**Changes Required:**
1. Replace API key with "not-needed"
2. Set `api.ApiUrlFormat` to proxy URL
3. Start proxy before running game

**After (Option B - Using OpenAI.LocalAdapter):**
```csharp
using OpenAI.LocalAdapter;

// Create client
var client = new OpenAILocalClient("http://localhost:8080");
var conversation = client.CreateConversation();

conversation.AppendSystemMessage("You are a dungeon master for a D&D game...");
conversation.AppendUserInput("Begin the adventure");
string response = await conversation.GetResponseAsync();
```

**Changes Required:**
1. Add OpenAI.LocalAdapter reference
2. Replace OpenAIAPI with OpenAILocalClient
3. Change `GetResponseFromChatbotAsync()` to `GetResponseAsync()`
4. Start proxy before running game

### Step-by-Step Migration

#### Phase 1: Preparation (5 minutes)

1. **Install and test proxy:**
   ```bash
   cd openai-local-proxy
   pip install -r requirements.txt
   python proxy_server.py
   ```

2. **Verify LM Studio is running:**
   ```bash
   curl http://10.50.10.14:1234/v1/models
   ```

3. **Test proxy:**
   ```bash
   curl http://localhost:8080/health
   curl http://localhost:8080/v1/models
   ```

#### Phase 2: Code Changes (10 minutes)

**Option A: Minimal Changes (Recommended for quick migration)**

Edit `AdventureGenerator.cs`:

```csharp
// Find this code (around line 121):
OpenAIAPI api = new OpenAIAPI(text);

// Replace with:
OpenAIAPI api = new OpenAIAPI("not-needed");
api.ApiUrlFormat = "http://localhost:8080/{0}/{1}";
```

Edit `StoryObjects.cs` (similar changes at lines 229, 333, 484, 569):

```csharp
// Find:
OpenAIAPI api = new OpenAIAPI(text);

// Replace with:
OpenAIAPI api = new OpenAIAPI("not-needed");
api.ApiUrlFormat = "http://localhost:8080/{0}/{1}";
```

**Option B: Use OpenAI.LocalAdapter**

1. Add reference to OpenAI.LocalAdapter:
   ```xml
   <ItemGroup>
     <ProjectReference Include="../OpenAI.LocalAdapter/OpenAI.LocalAdapter.csproj" />
   </ItemGroup>
   ```

2. Create a helper class:
   ```csharp
   public static class AIHelper
   {
       private static OpenAILocalClient? _client;
       
       public static OpenAILocalClient GetClient()
       {
           if (_client == null)
           {
               _client = new OpenAILocalClient("http://localhost:8080");
           }
           return _client;
       }
   }
   ```

3. Replace OpenAI_API calls with OpenAI.LocalAdapter calls

#### Phase 3: Testing (15 minutes)

1. **Start the proxy:**
   ```bash
   cd openai-local-proxy
   python proxy_server.py
   ```

2. **Run the game:**
   ```bash
   cd simple-console-RPG
   dotnet run
   ```

3. **Test scenarios:**
   - Character creation
   - Adventure generation
   - Combat scenarios
   - Multiple conversation turns

#### Phase 4: Configuration (5 minutes)

Create `appsettings.json` in the game directory:

```json
{
  "OpenAI": {
    "BaseUrl": "http://localhost:8080",
    "Model": "llama-3.1-instruct-13b",
    "Temperature": 0.7,
    "MaxTokens": 500
  }
}
```

Update code to read from config instead of hardcoding URLs.

---

## General C# Application Migration

### Official OpenAI SDK (Betalgo.OpenAI)

**Before:**
```csharp
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;

var openAiService = new OpenAIService(new OpenAiOptions()
{
    ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
});

var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
{
    Messages = new List<ChatMessage>
    {
        ChatMessage.FromSystem("You are helpful assistant."),
        ChatMessage.FromUser("Hello!")
    },
    Model = Models.ChatGpt3_5Turbo
});
```

**After (With Proxy):**
```csharp
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;

var openAiService = new OpenAIService(new OpenAiOptions()
{
    ApiKey = "not-needed",
    BaseDomain = "http://localhost:8080"
});

var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
{
    Messages = new List<ChatMessage>
    {
        ChatMessage.FromSystem("You are helpful assistant."),
        ChatMessage.FromUser("Hello!")
    },
    Model = Models.ChatGpt3_5Turbo
});
```

**After (With OpenAI.LocalAdapter):**
```csharp
using OpenAI.LocalAdapter;
using OpenAI.LocalAdapter.Models;

var client = new OpenAILocalClient("http://localhost:8080");

var request = new ChatCompletionRequest
{
    Model = "gpt-3.5-turbo",
    Messages = new List<ChatMessage>
    {
        ChatMessage.System("You are helpful assistant."),
        ChatMessage.User("Hello!")
    }
};

var response = await client.Chat.CreateCompletionAsync(request);
```

---

## Python Application Migration

### Official OpenAI SDK (Python)

**Before:**
```python
import openai

openai.api_key = "sk-..."

response = openai.ChatCompletion.create(
    model="gpt-3.5-turbo",
    messages=[
        {"role": "system", "content": "You are helpful."},
        {"role": "user", "content": "Hello!"}
    ]
)
```

**After (With Proxy):**
```python
import openai

openai.api_key = "not-needed"
openai.api_base = "http://localhost:8080/v1"

response = openai.ChatCompletion.create(
    model="gpt-3.5-turbo",
    messages=[
        {"role": "system", "content": "You are helpful."},
        {"role": "user", "content": "Hello!"}
    ]
)
```

### LangChain

**Before:**
```python
from langchain.chat_models import ChatOpenAI
from langchain.schema import HumanMessage, SystemMessage

chat = ChatOpenAI(model_name="gpt-3.5-turbo", openai_api_key="sk-...")

messages = [
    SystemMessage(content="You are helpful."),
    HumanMessage(content="Hello!")
]

response = chat(messages)
```

**After (With Proxy):**
```python
from langchain.chat_models import ChatOpenAI
from langchain.schema import HumanMessage, SystemMessage

chat = ChatOpenAI(
    model_name="gpt-3.5-turbo",
    openai_api_key="not-needed",
    openai_api_base="http://localhost:8080/v1"
)

messages = [
    SystemMessage(content="You are helpful."),
    HumanMessage(content="Hello!")
]

response = chat(messages)
```

---

## Common Patterns

### Pattern 1: Conversation State Management

**OpenAI_API (Before):**
```csharp
var chat = api.Chat.CreateConversation();
chat.AppendSystemMessage("You are an assistant");
chat.AppendUserInput("Hello");
var response1 = await chat.GetResponseFromChatbotAsync();
chat.AppendUserInput("How are you?");
var response2 = await chat.GetResponseFromChatbotAsync();
```

**OpenAI.LocalAdapter (After):**
```csharp
var conversation = client.CreateConversation();
conversation.AppendSystemMessage("You are an assistant");
conversation.AppendUserInput("Hello");
var response1 = await conversation.GetResponseAsync();
conversation.AppendUserInput("How are you?");
var response2 = await conversation.GetResponseAsync();
```

### Pattern 2: Streaming Responses

**OpenAI SDK (Before):**
```csharp
await foreach (var completion in api.Chat.StreamChatAsync(...))
{
    Console.Write(completion.Choices[0].Delta.Content);
}
```

**OpenAI.LocalAdapter (After):**
```csharp
await foreach (var chunk in conversation.GetResponseStreamAsync())
{
    Console.Write(chunk);
}
```

### Pattern 3: Error Handling

**Add retry logic and timeout handling:**
```csharp
var config = new LocalAdapterConfig
{
    BaseUrl = "http://localhost:8080",
    TimeoutSeconds = 300,
    MaxRetries = 3,
    RetryDelayMs = 1000
};

var client = new OpenAILocalClient(config);

try
{
    var response = await client.Chat.CreateCompletionAsync(request);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"API Error: {ex.Message}");
    // Handle retry or fallback
}
```

---

## Troubleshooting

### Issue: Connection Refused

**Symptoms:**
- Cannot connect to proxy or backend
- "Connection refused" errors

**Solutions:**
1. Verify proxy is running: `curl http://localhost:8080/health`
2. Verify LM Studio is running: `curl http://10.50.10.14:1234/v1/models`
3. Check firewall rules
4. Verify URLs in config

### Issue: Model Not Found

**Symptoms:**
- "Model not found" errors
- Backend rejects requests

**Solutions:**
1. Check model is loaded in LM Studio
2. Verify model mapping in `config.yaml`
3. Use exact model name from backend
4. Check proxy logs for model name translation

### Issue: Slow Responses

**Symptoms:**
- Requests take very long
- Timeouts

**Solutions:**
1. Check LM Studio GPU usage
2. Reduce max_tokens in requests
3. Increase timeout in config
4. Consider smaller model
5. Check CPU/RAM usage

### Issue: Streaming Not Working

**Symptoms:**
- No streaming output
- Full response arrives at once

**Solutions:**
1. Ensure `stream: true` in request
2. Verify backend supports streaming
3. Check proxy streaming endpoint
4. Use proper async enumerable handling

### Issue: Inconsistent Responses

**Symptoms:**
- Responses different from OpenAI
- Lower quality outputs

**Solutions:**
1. Adjust temperature (lower = more consistent)
2. Improve system prompts
3. Try different models
4. Add more context in messages
5. Fine-tune prompts for local models

---

## Best Practices

### 1. Use Configuration Files

Don't hardcode URLs and settings:

```json
{
  "OpenAI": {
    "BaseUrl": "http://localhost:8080",
    "DefaultModel": "llama-3.1-instruct-13b",
    "Temperature": 0.7,
    "MaxTokens": 500,
    "TimeoutSeconds": 300
  }
}
```

### 2. Implement Health Checks

Check backend availability before making requests:

```csharp
var healthCheck = await httpClient.GetAsync("http://localhost:8080/health");
if (!healthCheck.IsSuccessStatusCode)
{
    throw new Exception("Proxy is not healthy");
}
```

### 3. Use Dependency Injection

Register client as a singleton:

```csharp
services.AddSingleton<OpenAILocalClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["OpenAI:BaseUrl"];
    return new OpenAILocalClient(baseUrl);
});
```

### 4. Log Requests and Responses

Enable logging for debugging:

```csharp
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
});

var logger = loggerFactory.CreateLogger<OpenAILocalClient>();
var client = new OpenAILocalClient(config, logger: logger);
```

### 5. Handle Cancellation

Support cancellation tokens:

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try
{
    var response = await client.Chat.CreateCompletionAsync(request, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Request cancelled");
}
```

---

## Performance Considerations

### Latency Comparison

| Setup | Typical Latency |
|-------|----------------|
| OpenAI API | 500-2000ms |
| Direct LM Studio | 50-500ms (depending on model/GPU) |
| Via Proxy | +10-50ms overhead |

### Optimization Tips

1. **Use GPU acceleration** in LM Studio
2. **Reuse HttpClient** instances
3. **Enable connection pooling**
4. **Use streaming** for long responses
5. **Batch requests** when possible
6. **Choose appropriate model size** (smaller = faster)

---

## Next Steps

1. **Test thoroughly** with your specific use cases
2. **Monitor performance** and adjust settings
3. **Implement proper error handling**
4. **Add health checks and monitoring**
5. **Consider scaling** (multiple LM Studio instances)
6. **Document** your specific migration patterns

---

## Additional Resources

- **Proxy Documentation**: `/openai-local-proxy/README.md`
- **C# Library Documentation**: `/OpenAI.LocalAdapter/README.md`
- **Examples**: `/examples/` directory
- **Architecture**: `/docs/ARCHITECTURE.md`
- **Troubleshooting**: `/docs/TROUBLESHOOTING.md`

---

## Support

If you encounter issues:

1. Check the troubleshooting section above
2. Review proxy logs: `python proxy_server.py` (with debug logging)
3. Check LM Studio logs
4. Open an issue on GitHub with:
   - Error messages
   - Configuration
   - Steps to reproduce
