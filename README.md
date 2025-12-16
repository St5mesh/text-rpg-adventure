# AI Dungeons & Dragons: text-rpg-adventure

This is a procedurally generated text based RPG, featuring a character creator and a custom language model designed for D&D environments. The game generates unique adventures based on player choices and actions.

**🆕 Now with 100% Local LLM Support!** Run the game completely offline with LM Studio, Ollama, or other local backends using our OpenAI API compatibility layer.

## Features

- 🎲 **Procedural Generation**: Every adventure is unique
- 🧙 **Character Creator**: Create custom D&D characters
- ⚔️ **Dynamic Combat**: Dice-based combat system
- 🤖 **AI-Powered**: Uses LLMs for storytelling
- 🏠 **100% Local Option**: Run entirely offline with local LLMs
- 💰 **Zero API Costs**: No OpenAI API charges when running locally

---

## Quick Start (Local LLM)

### Prerequisites
- .NET 7.0+
- Python 3.11+ (for proxy)
- [LM Studio](https://lmstudio.ai/) or [Ollama](https://ollama.ai/)

### Option 1: Using OpenAI API (Original)
1. Get an OpenAI API key
2. Create `apikey.txt` with your key
3. Run: `dotnet run --project simple-console-RPG`

### Option 2: Using Local LLM (New! Recommended)

**Step 1: Start LM Studio**
```bash
# 1. Download and open LM Studio
# 2. Download a model (e.g., llama-3.1-instruct-13b)
# 3. Start the local server (default: http://localhost:1234)
```

**Step 2: Start the Proxy**
```bash
cd openai-local-proxy
pip install -r requirements.txt
python proxy_server.py
```

**Step 3: Run the Game**
```bash
cd simple-console-RPG
dotnet run
```

See [Quick Start Guide](examples/proxy-quick-start.md) for detailed instructions.

---

## Repository Structure

```
text-rpg-adventure/
├── simple-console-RPG/          # Main game application
│   ├── AdventureGenerator.cs    # Core adventure logic
│   ├── StoryObjects.cs          # Story elements
│   └── ...
│
├── openai-local-proxy/          # HTTP proxy for local LLMs
│   ├── proxy_server.py          # FastAPI proxy server
│   ├── config.yaml              # Configuration
│   ├── Dockerfile               # Container image
│   └── README.md                # Proxy documentation
│
├── OpenAI.LocalAdapter/         # C# library for local LLMs
│   ├── OpenAILocalClient.cs     # Main client
│   ├── Services/                # Service implementations
│   ├── Models/                  # Data models
│   └── README.md                # Library documentation
│
├── docs/                        # Comprehensive documentation
│   ├── MIGRATION_GUIDE.md       # Migration instructions
│   └── ARCHITECTURE.md          # System architecture
│
├── examples/                    # Usage examples
│   ├── proxy-quick-start.md     # Quick start guide
│   └── migration-text-rpg-adventure.md  # Game migration guide
│
└── config-templates/            # Configuration templates
```

---

## Documentation

### Getting Started
- 📖 [Quick Start Guide](examples/proxy-quick-start.md) - Get running in 5 minutes
- 🎮 [Game Migration Guide](examples/migration-text-rpg-adventure.md) - Migrate the game to local LLM
- 🚀 [General Migration Guide](docs/MIGRATION_GUIDE.md) - Migrate any OpenAI app

### Technical Documentation
- 🏗️ [Architecture](docs/ARCHITECTURE.md) - System design and components
- 🔌 [Proxy Documentation](openai-local-proxy/README.md) - HTTP proxy details
- 📚 [C# Library Documentation](OpenAI.LocalAdapter/README.md) - .NET library guide

### Configuration
- ⚙️ [Configuration Templates](config-templates/) - Sample configurations
- 🐳 [Docker Deployment](openai-local-proxy/docker-compose.yml) - Container setup

---

## Local OpenAI Replacement System

This repository includes a complete local replacement for OpenAI API:

### Components

#### 1. OpenAI Local Proxy (Python/FastAPI)
Zero-code-change solution for existing applications.

**Features:**
- ✅ Full OpenAI API compatibility
- ✅ Model name mapping (gpt-3.5-turbo → llama-3.1)
- ✅ Streaming support
- ✅ Docker deployment
- ✅ Health checks and monitoring

**Usage:**
```bash
cd openai-local-proxy
python proxy_server.py
```

#### 2. OpenAI.LocalAdapter (C# Library)
Native C# library with type safety and modern async patterns.

**Features:**
- ✅ Strongly typed API
- ✅ Conversation management
- ✅ Async/await throughout
- ✅ Streaming support
- ✅ Multi-target (.NET 7.0, 6.0, Standard 2.1)

**Usage:**
```csharp
using OpenAI.LocalAdapter;

var client = new OpenAILocalClient("http://localhost:8080");
var conversation = client.CreateConversation();
conversation.AppendSystemMessage("You are a dungeon master...");
var response = await conversation.GetResponseAsync();
```

---

## Migration Strategies

### Strategy 1: Zero-Code-Change (Proxy Only)

Change only the API initialization:

**Before:**
```csharp
OpenAIAPI api = new OpenAIAPI(apiKey);
```

**After:**
```csharp
OpenAIAPI api = new OpenAIAPI("not-needed");
api.ApiUrlFormat = "http://localhost:8080/{0}/{1}";
```

### Strategy 2: Native Library

Use the OpenAI.LocalAdapter for better performance:

**Before:**
```csharp
using OpenAI_API;
var api = new OpenAIAPI(apiKey);
var chat = api.Chat.CreateConversation();
string response = await chat.GetResponseFromChatbotAsync();
```

**After:**
```csharp
using OpenAI.LocalAdapter;
var client = new OpenAILocalClient("http://localhost:8080");
var conversation = client.CreateConversation();
string response = await conversation.GetResponseAsync();
```

---

## Supported Backends

- ✅ **LM Studio** - Primary recommendation (GPU accelerated)
- ✅ **Ollama** - Alternative local backend
- ✅ **LocalAI** - Multi-model support
- ✅ **Any OpenAI-compatible API** - Custom backends

---

## Performance

### Latency Comparison
| Method | Typical Response Time | Cost |
|--------|---------------------|------|
| OpenAI API | 2-5 seconds | $0.002/1K tokens |
| Local LLM (GPU) | 1-10 seconds | Free |
| Local LLM (CPU) | 5-30 seconds | Free |

### Proxy Overhead
- HTTP proxy adds: **10-50ms**
- Direct library adds: **5-20ms**
- Negligible compared to LLM processing time

---

## Benefits of Local LLM

### 💰 Cost Savings
- **Zero API costs** - No per-token charges
- **No rate limits** - Use as much as you want
- **No subscription fees** - One-time hardware cost only

### 🔒 Privacy & Security
- **100% local** - Data never leaves your machine
- **No internet required** - Works completely offline
- **Full control** - Own your data and model

### ⚡ Performance
- **Faster for some models** - Especially with good GPU
- **No network latency** - Direct local access
- **Consistent performance** - No API throttling

### 🎯 Customization
- **Choose your model** - Use any compatible model
- **Fine-tune locally** - Train on your data
- **Full parameter control** - Adjust temperature, etc.

---

## Requirements

### For Running the Game
- .NET 7.0 or later
- Windows, macOS, or Linux

### For Local LLM Support
- Python 3.11+ (for proxy)
- LM Studio or Ollama
- Recommended: NVIDIA GPU with 8GB+ VRAM
- Minimum: 16GB RAM for CPU-only mode

---

## Installation

### 1. Clone the Repository
```bash
git clone https://github.com/St5mesh/text-rpg-adventure.git
cd text-rpg-adventure
```

### 2. Setup Local LLM (Optional but Recommended)

**Option A: LM Studio**
```bash
# 1. Download LM Studio from https://lmstudio.ai/
# 2. Download a model (llama-3.1-instruct-13b recommended)
# 3. Start the local server
```

**Option B: Ollama**
```bash
# Install Ollama
curl -fsSL https://ollama.ai/install.sh | sh

# Pull a model
ollama pull llama3.1

# Start server
ollama serve
```

### 3. Start the Proxy
```bash
cd openai-local-proxy
pip install -r requirements.txt
python proxy_server.py
```

### 4. Run the Game
```bash
cd simple-console-RPG
dotnet run
```

---

## Troubleshooting

### Connection Refused
**Solution:** Verify proxy is running
```bash
curl http://localhost:8080/health
```

### Slow Responses
**Solutions:**
1. Enable GPU acceleration in LM Studio
2. Use smaller model
3. Reduce max_tokens parameter

### Model Not Found
**Solution:** Check model name mapping in `openai-local-proxy/config.yaml`

See [Migration Guide](docs/MIGRATION_GUIDE.md) for more troubleshooting.

---

## Development

### Building the C# Library
```bash
cd OpenAI.LocalAdapter
dotnet build
dotnet pack
```

### Running Tests
```bash
# Python proxy tests
cd openai-local-proxy
pytest tests/

# C# library tests (when available)
cd OpenAI.LocalAdapter.Tests
dotnet test
```

---

## Contributing

Contributions welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

---

## License

MIT License - See LICENSE file for details

---

## Acknowledgments

- Original game concept and implementation
- OpenAI for the original API design
- LM Studio team for excellent local LLM platform
- Ollama team for easy local LLM deployment
- Open source LLM community

---

## Support

- 📖 [Documentation](docs/)
- 🐛 [Report Issues](https://github.com/St5mesh/text-rpg-adventure/issues)
- 💬 [Discussions](https://github.com/St5mesh/text-rpg-adventure/discussions)

---

## Roadmap

- [x] Python HTTP proxy
- [x] C# LocalAdapter library
- [x] Comprehensive documentation
- [x] Docker deployment
- [ ] CI/CD pipelines
- [ ] More backend providers
- [ ] Web interface for proxy monitoring
- [ ] Model performance benchmarks

---

## Star History

If you find this project useful, please consider giving it a star! ⭐

---

Made with ❤️ for local LLM enthusiasts
