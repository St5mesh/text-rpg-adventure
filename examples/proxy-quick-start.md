# Quick Start Guide: Using the Proxy with Zero Code Changes

This guide shows you how to use the OpenAI Local Proxy to run your existing OpenAI applications locally with **zero code changes** (or minimal URL changes).

## Prerequisites

- Python 3.11+
- LM Studio installed and running
- Your existing application that uses OpenAI API

## Setup (5 Minutes)

### Step 1: Install and Configure LM Studio

1. **Download and install** LM Studio from https://lmstudio.ai/
2. **Download a model** (recommended: llama-3.1-instruct-13b or similar)
3. **Start the local server**:
   - Open LM Studio
   - Click "Local Server" tab
   - Click "Start Server"
   - Note the URL (usually `http://localhost:1234`)
4. **Load your model** in the server

### Step 2: Install and Start the Proxy

```bash
# Navigate to proxy directory
cd openai-local-proxy

# Install dependencies
pip install -r requirements.txt

# Edit config.yaml if your LM Studio is on a different URL
# (default is http://10.50.10.14:1234)

# Start the proxy
python proxy_server.py
```

You should see:
```
INFO:     OpenAI Local Proxy started
INFO:     Forwarding requests to: http://10.50.10.14:1234
INFO:     Uvicorn running on http://0.0.0.0:8080
```

### Step 3: Test the Proxy

```bash
# Test health check
curl http://localhost:8080/health

# Test models endpoint
curl http://localhost:8080/v1/models

# Test chat completion
curl http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer not-needed" \
  -d '{
    "model": "gpt-3.5-turbo",
    "messages": [
      {"role": "user", "content": "Say hello!"}
    ]
  }'
```

## Using with Your Application

### Option 1: Change Base URL Only (Recommended)

Most OpenAI libraries support changing the base URL:

#### Python (Official OpenAI SDK)

```python
import openai

# Before
# openai.api_key = "sk-..."

# After - just change these two lines
openai.api_key = "not-needed"
openai.api_base = "http://localhost:8080/v1"

# Rest of your code stays the same!
response = openai.ChatCompletion.create(
    model="gpt-3.5-turbo",
    messages=[{"role": "user", "content": "Hello!"}]
)
```

#### C# (OpenAI_API - OkGoDoIt)

```csharp
using OpenAI_API;

// Before
// OpenAIAPI api = new OpenAIAPI(apiKey);

// After - just change these lines
OpenAIAPI api = new OpenAIAPI("not-needed");
api.ApiUrlFormat = "http://localhost:8080/{0}/{1}";

// Rest of your code stays the same!
var chat = api.Chat.CreateConversation();
chat.AppendUserInput("Hello!");
string response = await chat.GetResponseFromChatbotAsync();
```

#### C# (Betalgo.OpenAI)

```csharp
using OpenAI.GPT3;

// Before
// var openAiService = new OpenAIService(new OpenAiOptions() { ApiKey = "sk-..." });

// After - just change these lines
var openAiService = new OpenAIService(new OpenAiOptions() 
{ 
    ApiKey = "not-needed",
    BaseDomain = "http://localhost:8080"
});

// Rest of your code stays the same!
var completionResult = await openAiService.ChatCompletion.CreateCompletion(...);
```

#### Node.js (OpenAI SDK)

```javascript
import OpenAI from 'openai';

// Before
// const openai = new OpenAI({ apiKey: 'sk-...' });

// After - just change these lines
const openai = new OpenAI({
  apiKey: 'not-needed',
  baseURL: 'http://localhost:8080/v1'
});

// Rest of your code stays the same!
const completion = await openai.chat.completions.create({
  model: "gpt-3.5-turbo",
  messages: [{ role: "user", content: "Hello!" }]
});
```

### Option 2: Environment Variables

Set environment variables before running your app:

```bash
# For Python
export OPENAI_API_KEY="not-needed"
export OPENAI_API_BASE="http://localhost:8080/v1"

# Run your app
python your_app.py
```

```bash
# For .NET
export OpenAI__ApiKey="not-needed"
export OpenAI__BaseUrl="http://localhost:8080"

# Run your app
dotnet run
```

### Option 3: Configuration Files

**Python (.env file)**:
```
OPENAI_API_KEY=not-needed
OPENAI_API_BASE=http://localhost:8080/v1
```

**C# (appsettings.json)**:
```json
{
  "OpenAI": {
    "ApiKey": "not-needed",
    "BaseUrl": "http://localhost:8080"
  }
}
```

## Verification

Your application should now work exactly as before, but using your local LLM!

### Check the Logs

**Proxy logs** (in the terminal where you ran `python proxy_server.py`):
```
INFO: Forwarding POST /v1/chat/completions to http://10.50.10.14:1234/v1/chat/completions
```

**LM Studio** (check the "Local Server" tab for request logs)

## Troubleshooting

### Issue: Connection Refused

**Error**: `Connection refused` or `Cannot connect to host`

**Solutions**:
1. Verify proxy is running: `curl http://localhost:8080/health`
2. Verify LM Studio is running: `curl http://10.50.10.14:1234/v1/models`
3. Check firewall settings
4. Verify URLs in your application

### Issue: Timeout

**Error**: Request times out

**Solutions**:
1. Check LM Studio has model loaded
2. Verify GPU is available (check LM Studio settings)
3. Try smaller model
4. Increase timeout in your application

### Issue: Wrong Model

**Error**: Model not found or wrong responses

**Solutions**:
1. Check model is loaded in LM Studio
2. Verify model name mapping in `config.yaml`
3. Use model name from LM Studio directly

### Issue: Slow Responses

**Symptoms**: Responses take very long

**Solutions**:
1. Enable GPU acceleration in LM Studio
2. Use smaller model
3. Reduce `max_tokens` in requests
4. Check system resources (CPU/RAM/GPU)

## Advanced Configuration

### Custom Model Mapping

Edit `openai-local-proxy/config.yaml`:

```yaml
model_mapping:
  "gpt-3.5-turbo": "your-local-model-name"
  "gpt-4": "your-larger-model-name"
```

### Multiple Backends

```yaml
backends:
  primary:
    url: "http://10.50.10.14:1234"
    enabled: true
  
  fallback:
    url: "http://localhost:11434"  # Ollama
    enabled: true
```

### Enable Authentication

```yaml
authentication:
  enabled: true
  valid_api_keys:
    - "sk-local-dev-key-123"
```

Then use this key in your application:
```python
openai.api_key = "sk-local-dev-key-123"
```

## Running in Production

### Docker

```bash
cd openai-local-proxy
docker-compose up -d
```

### Systemd (Linux)

```bash
# Copy files
sudo cp -r openai-local-proxy /opt/
sudo cp openai-local-proxy/openai-proxy.service /etc/systemd/system/

# Enable and start
sudo systemctl enable openai-proxy
sudo systemctl start openai-proxy
sudo systemctl status openai-proxy
```

### Check Logs

```bash
# Docker
docker-compose logs -f

# Systemd
sudo journalctl -u openai-proxy -f
```

## Next Steps

1. ✅ **Test with your application** - Make sure everything works
2. ✅ **Adjust settings** - Tune temperature, max_tokens, etc.
3. ✅ **Monitor performance** - Check response times and quality
4. ✅ **Optimize** - Try different models, adjust settings
5. ✅ **Scale** - Add more backends if needed

## Getting Help

- **Documentation**: See `/docs/MIGRATION_GUIDE.md` for detailed migration info
- **Examples**: See `/examples/` for code examples
- **Troubleshooting**: See `/docs/TROUBLESHOOTING.md` for common issues
- **Issues**: Open an issue on GitHub

## Success! 🎉

Your application should now be running completely locally, with no OpenAI API costs and full control over your data!
