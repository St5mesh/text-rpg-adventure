# Migration Guide: Text RPG Adventure to Local LLM

This guide provides step-by-step instructions for migrating the Text RPG Adventure game from OpenAI API to local LLM execution using LM Studio.

## Current State Analysis

The game currently uses:
- **Package**: `OpenAI` v1.7.2 (likely OpenAI_API by OkGoDoIt)
- **API Key**: Read from `apikey.txt` file
- **Usage**: Multiple API instances created throughout the code
- **Locations**: 
  - `AdventureGenerator.cs` (line 121)
  - `StoryObjects.cs` (lines 229, 333, 484, 569)

## Migration Strategy

We have two options:

### Option A: Zero-Code-Change (Using Proxy)
- ⏱️ **Time**: 10 minutes
- 🔧 **Complexity**: Very Low
- 📝 **Code Changes**: Minimal (2 lines per API instance)
- ✅ **Recommended for**: Quick testing

### Option B: Native Library (OpenAI.LocalAdapter)
- ⏱️ **Time**: 30-60 minutes
- 🔧 **Complexity**: Medium
- 📝 **Code Changes**: Moderate refactoring
- ✅ **Recommended for**: Long-term, better performance

## Option A: Zero-Code-Change Migration

### Step 1: Setup (5 minutes)

#### 1.1 Start LM Studio

```bash
# 1. Open LM Studio
# 2. Download and load a model (e.g., llama-3.1-instruct-13b)
# 3. Start the local server on port 1234
# 4. Verify it's running
curl http://localhost:1234/v1/models
```

#### 1.2 Start the Proxy

```bash
cd openai-local-proxy
pip install -r requirements.txt
python proxy_server.py
```

Verify proxy is running:
```bash
curl http://localhost:8080/health
```

### Step 2: Code Changes (5 minutes)

#### 2.1 Update AdventureGenerator.cs

Find this code around **line 121**:

```csharp
string apikeyFilePath = "apikey.txt";
string text = File.ReadAllText(apikeyFilePath);

OpenAIAPI api = new OpenAIAPI(text);
var chat = api.Chat.CreateConversation();
```

Replace with:

```csharp
// Use local proxy instead of OpenAI
OpenAIAPI api = new OpenAIAPI("not-needed");
api.ApiUrlFormat = "http://localhost:8080/{0}/{1}";
var chat = api.Chat.CreateConversation();
```

#### 2.2 Update StoryObjects.cs

Find and update **4 instances** (around lines 229, 333, 484, 569):

**Before:**
```csharp
string apikeyFilePath = "apikey.txt";
string text = File.ReadAllText(apikeyFilePath);
OpenAIAPI api = new OpenAIAPI(text);
```

**After:**
```csharp
OpenAIAPI api = new OpenAIAPI("not-needed");
api.ApiUrlFormat = "http://localhost:8080/{0}/{1}";
```

### Step 3: Test (5 minutes)

```bash
cd simple-console-RPG
dotnet build
dotnet run
```

Test the following:
1. ✅ Character creation works
2. ✅ Adventure generation works
3. ✅ Combat scenarios work
4. ✅ Multiple conversation turns work

### Complete Code Changes Summary

**Files Modified**: 2
- `AdventureGenerator.cs`
- `StoryObjects.cs`

**Lines Changed**: ~10 lines total

**Changes Made**:
1. Removed file reading for API key
2. Added proxy URL configuration
3. Everything else unchanged

---

## Option B: Native Library Migration

### Step 1: Add Project Reference

Edit `simple-console-RPG/simple-console-RPG.csproj`:

```xml
<ItemGroup>
  <!-- Keep existing packages -->
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  <!-- Remove or comment out OpenAI package -->
  <!-- <PackageReference Include="OpenAI" Version="1.7.2" /> -->
  
  <!-- Add local adapter -->
  <ProjectReference Include="../OpenAI.LocalAdapter/OpenAI.LocalAdapter.csproj" />
</ItemGroup>
```

### Step 2: Create Helper Class

Create `simple-console-RPG/AIHelper.cs`:

```csharp
using OpenAI.LocalAdapter;
using OpenAI.LocalAdapter.Configuration;
using OpenAI.LocalAdapter.Interfaces;

namespace simple_console_RPG
{
    /// <summary>
    /// Helper class for managing AI client
    /// </summary>
    public static class AIHelper
    {
        private static OpenAILocalClient? _client;

        /// <summary>
        /// Gets or creates the AI client instance
        /// </summary>
        public static OpenAILocalClient GetClient()
        {
            if (_client == null)
            {
                var config = new LocalAdapterConfig
                {
                    BaseUrl = "http://localhost:8080",
                    DefaultModel = "llama-3.1-instruct-13b",
                    TimeoutSeconds = 300,
                    MaxRetries = 3
                };

                _client = new OpenAILocalClient(config);
            }

            return _client;
        }

        /// <summary>
        /// Creates a new conversation
        /// </summary>
        public static IConversationManager CreateConversation(
            double temperature = 0.7,
            int maxTokens = 500)
        {
            return GetClient().CreateConversation(
                temperature: temperature,
                maxTokens: maxTokens
            );
        }
    }
}
```

### Step 3: Update AdventureGenerator.cs

**Before:**
```csharp
using OpenAI_API;

// ... in method ...
string apikeyFilePath = "apikey.txt";
string text = File.ReadAllText(apikeyFilePath);

OpenAIAPI api = new OpenAIAPI(text);
var chat = api.Chat.CreateConversation();

// System message
chat.AppendSystemMessage("You are a dungeon master...");

// User input
chat.AppendUserInput("Begin adventure");

// Get response
string response = await chat.GetResponseFromChatbotAsync();
```

**After:**
```csharp
// Remove: using OpenAI_API;
// Add:
using OpenAI.LocalAdapter;
using OpenAI.LocalAdapter.Models;

// ... in method ...
var conversation = AIHelper.CreateConversation(temperature: 0.7, maxTokens: 500);

// System message
conversation.AppendSystemMessage("You are a dungeon master...");

// User input
conversation.AppendUserInput("Begin adventure");

// Get response
string response = await conversation.GetResponseAsync();
```

### Step 4: Update StoryObjects.cs

Apply similar changes to all 4 API usage locations in `StoryObjects.cs`.

**Pattern to follow:**

```csharp
// Before
OpenAIAPI api = new OpenAIAPI(text);
var chat = api.Chat.CreateConversation();
chat.AppendSystemMessage(...);
chat.AppendUserInput(...);
string response = await chat.GetResponseFromChatbotAsync();

// After
var conversation = AIHelper.CreateConversation();
conversation.AppendSystemMessage(...);
conversation.AppendUserInput(...);
string response = await conversation.GetResponseAsync();
```

### Step 5: Optional - Add Configuration

Create `simple-console-RPG/appsettings.json`:

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

Update `simple-console-RPG.csproj` to include it:

```xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

Update `AIHelper.cs` to read from config:

```csharp
using Microsoft.Extensions.Configuration;

public static class AIHelper
{
    private static OpenAILocalClient? _client;
    private static IConfiguration? _config;

    static AIHelper()
    {
        _config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
    }

    public static OpenAILocalClient GetClient()
    {
        if (_client == null)
        {
            var baseUrl = _config?["OpenAI:BaseUrl"] ?? "http://localhost:8080";
            var defaultModel = _config?["OpenAI:DefaultModel"] ?? "llama-3.1-instruct-13b";
            var timeout = int.Parse(_config?["OpenAI:TimeoutSeconds"] ?? "300");

            var config = new LocalAdapterConfig
            {
                BaseUrl = baseUrl,
                DefaultModel = defaultModel,
                TimeoutSeconds = timeout
            };

            _client = new OpenAILocalClient(config);
        }

        return _client;
    }
}
```

### Step 6: Build and Test

```bash
cd simple-console-RPG
dotnet restore
dotnet build
dotnet run
```

---

## Testing Checklist

After migration, test these scenarios:

### Basic Functionality
- [ ] Game starts without errors
- [ ] Character creation works
- [ ] Name, class, race selection works
- [ ] Stats are generated correctly

### Adventure Generation
- [ ] Adventure begins successfully
- [ ] Setting is generated
- [ ] Enemy and goals are created
- [ ] Story is coherent

### Combat System
- [ ] Combat initiates properly
- [ ] Dice rolls work
- [ ] Combat descriptions are generated
- [ ] Win/loss conditions work

### Conversation Flow
- [ ] Multiple turns work
- [ ] Context is maintained
- [ ] Responses are relevant
- [ ] Story progresses logically

### Performance
- [ ] Responses arrive within acceptable time (< 10 seconds)
- [ ] No timeouts occur
- [ ] Memory usage is reasonable

### Error Handling
- [ ] Game handles connection errors gracefully
- [ ] Timeout errors don't crash the game
- [ ] Invalid inputs are handled

---

## Troubleshooting

### Issue: Cannot Find OpenAI.LocalAdapter

**Error**: `The type or namespace name 'OpenAI.LocalAdapter' could not be found`

**Solution**:
```bash
# Build the library first
cd OpenAI.LocalAdapter
dotnet build

# Then build the game
cd ../simple-console-RPG
dotnet restore
dotnet build
```

### Issue: Slow Response Times

**Symptoms**: Game feels sluggish, long waits for responses

**Solutions**:
1. Check GPU is being used in LM Studio (Settings > Hardware)
2. Try a smaller model
3. Reduce max_tokens in AIHelper:
   ```csharp
   maxTokens: 300  // Instead of 500
   ```
4. Check system resources (GPU, CPU, RAM usage)

### Issue: Poor Quality Responses

**Symptoms**: Nonsensical responses, incomplete stories

**Solutions**:
1. Adjust temperature:
   ```csharp
   temperature: 0.8  // Higher for more creativity
   temperature: 0.5  // Lower for more consistency
   ```
2. Improve system prompts
3. Try a different model in LM Studio
4. Increase max_tokens for longer responses

### Issue: Context Lost

**Symptoms**: AI doesn't remember previous conversation

**Solutions**:
- Make sure you're using the same conversation instance
- Don't create new conversation for each request
- Check conversation history with:
  ```csharp
  var messages = conversation.GetMessages();
  Console.WriteLine($"History has {messages.Count} messages");
  ```

---

## Performance Comparison

### Before (OpenAI API)
- Average response time: 2-5 seconds
- Cost: $0.002 per 1K tokens
- Network dependent
- Rate limited

### After (Local LLM)
- Average response time: 1-10 seconds (depending on GPU)
- Cost: $0 (free!)
- Works offline
- No rate limits

---

## Rollback Plan

If you need to rollback to OpenAI:

### Option A Migration
Simply revert the code changes:
```csharp
// Restore original code
string apikeyFilePath = "apikey.txt";
string text = File.ReadAllText(apikeyFilePath);
OpenAIAPI api = new OpenAIAPI(text);
```

### Option B Migration
1. Remove OpenAI.LocalAdapter project reference
2. Restore OpenAI package reference
3. Restore original using statements and API calls

---

## Next Steps

1. **Play test extensively** - Try different scenarios
2. **Tune parameters** - Adjust temperature, max_tokens
3. **Try different models** - Experiment with model selection
4. **Document learnings** - Note what works best
5. **Share feedback** - Report issues and improvements

---

## Additional Resources

- **General Migration Guide**: `/docs/MIGRATION_GUIDE.md`
- **Proxy Documentation**: `/openai-local-proxy/README.md`
- **C# Library Documentation**: `/OpenAI.LocalAdapter/README.md`
- **Architecture**: `/docs/ARCHITECTURE.md`

---

## Success Metrics

After successful migration, you should have:
- ✅ Game running 100% locally
- ✅ No OpenAI API costs
- ✅ Complete data privacy
- ✅ Similar or better response quality
- ✅ Acceptable response times
- ✅ Working offline capability

Congratulations on going fully local! 🎉
