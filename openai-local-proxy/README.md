# OpenAI Local Proxy

A FastAPI-based HTTP proxy that enables zero-code-change migration of OpenAI API applications to local LLM backends (LM Studio, Ollama, etc.).

## Features

- ✅ **Zero Code Changes**: Works with existing OpenAI client libraries
- ✅ **Full API Compatibility**: Supports all OpenAI v1 endpoints
- ✅ **Model Mapping**: Automatic translation between OpenAI and local model names
- ✅ **Streaming Support**: Real-time streaming responses
- ✅ **Multi-Backend**: Support for multiple LLM backends with fallback
- ✅ **Docker Ready**: Easy deployment with Docker Compose
- ✅ **Production Ready**: Health checks, logging, CORS support

## Quick Start (5 Minutes)

### Prerequisites

- Python 3.11+ or Docker
- LM Studio running at `http://10.50.10.14:1234` (or configure your own backend)

### Option 1: Run with Python

```bash
# Install dependencies
pip install -r requirements.txt

# Run the server
python proxy_server.py
```

The proxy will start on `http://localhost:8080`

### Option 2: Run with Docker

```bash
# Build and run with Docker Compose
docker-compose up -d

# Check logs
docker-compose logs -f

# Stop
docker-compose down
```

### Option 3: Run with Docker manually

```bash
# Build image
docker build -t openai-local-proxy .

# Run container
docker run -d -p 8080:8080 --name openai-proxy openai-local-proxy

# Check health
curl http://localhost:8080/health
```

## Configuration

Edit `config.yaml` to customize the proxy behavior:

### Backend Configuration

```yaml
backends:
  primary:
    name: "LM Studio"
    url: "http://10.50.10.14:1234"  # Change to your LM Studio URL
    enabled: true
    timeout: 300
```

### Model Mapping

Map OpenAI model names to your local models:

```yaml
model_mapping:
  "gpt-3.5-turbo": "llama-3.1-instruct-13b"
  "gpt-4": "llama-3.1-instruct-13b"
```

### Authentication (Optional)

Enable API key validation:

```yaml
authentication:
  enabled: true
  valid_api_keys:
    - "sk-local-development-key"
```

## Usage

### With Existing Applications

Simply change the base URL in your application:

**Before:**
```python
from openai import OpenAI
client = OpenAI(api_key="sk-...")
```

**After:**
```python
from openai import OpenAI
client = OpenAI(
    api_key="not-needed",
    base_url="http://localhost:8080/v1"
)
```

### With the Text RPG Adventure Game

The game uses the OpenAI_API package. To use the proxy:

1. Start the proxy: `python proxy_server.py`
2. Modify the game to use proxy URL (see migration guide)
3. Run the game normally

### Testing the Proxy

```bash
# Check health
curl http://localhost:8080/health

# List models
curl http://localhost:8080/v1/models \
  -H "Authorization: Bearer not-needed"

# Chat completion
curl http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer not-needed" \
  -d '{
    "model": "gpt-3.5-turbo",
    "messages": [
      {"role": "user", "content": "Hello!"}
    ]
  }'

# Streaming chat completion
curl http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer not-needed" \
  -d '{
    "model": "gpt-3.5-turbo",
    "messages": [
      {"role": "user", "content": "Count to 5"}
    ],
    "stream": true
  }'
```

## Supported Endpoints

### Core Endpoints (Fully Supported)
- ✅ `POST /v1/chat/completions` - Chat completions (streaming supported)
- ✅ `POST /v1/completions` - Text completions (streaming supported)
- ✅ `POST /v1/embeddings` - Text embeddings
- ✅ `GET /v1/models` - List available models

### Additional Endpoints (Pass-through)
All other OpenAI API endpoints are forwarded to the backend if supported:
- `POST /v1/audio/transcriptions`
- `POST /v1/audio/translations`
- `POST /v1/images/generations`
- `POST /v1/moderations`
- And more...

### Utility Endpoints
- ✅ `GET /health` - Health check

## Performance

- **Overhead**: < 50ms for non-streaming requests
- **Streaming**: Real-time with minimal buffering
- **Throughput**: Limited by backend LLM performance

## Logging

Configure logging in `config.yaml`:

```yaml
logging:
  level: "INFO"  # DEBUG, INFO, WARNING, ERROR
  format: "text"  # text or json
  include_request_body: false  # Enable for debugging
  include_response_body: false  # Enable for debugging
```

View logs:
```bash
# Python
python proxy_server.py

# Docker
docker-compose logs -f openai-proxy
```

## Troubleshooting

### Connection Refused

**Problem**: Cannot connect to backend LM Studio

**Solution**: 
1. Verify LM Studio is running: `curl http://10.50.10.14:1234/v1/models`
2. Check firewall rules
3. Update `config.yaml` with correct backend URL

### Model Not Found

**Problem**: Backend returns "model not found" error

**Solution**:
1. Check model is loaded in LM Studio
2. Verify model mapping in `config.yaml`
3. Use exact model name from backend

### Timeout Errors

**Problem**: Requests timeout

**Solution**:
1. Increase timeout in `config.yaml`:
   ```yaml
   backends:
     primary:
       timeout: 600  # 10 minutes
   ```
2. Check backend performance
3. Reduce max_tokens in requests

### Streaming Not Working

**Problem**: Streaming responses not received

**Solution**:
1. Verify `stream: true` in request
2. Check backend supports streaming
3. Ensure client handles SSE format
4. Check CORS settings for cross-origin requests

## Development

### Running Tests

```bash
# Install dev dependencies
pip install -r requirements.txt

# Run tests
pytest tests/ -v

# Run with coverage
pytest tests/ --cov=proxy_server --cov-report=html
```

### Project Structure

```
openai-local-proxy/
├── proxy_server.py       # Main FastAPI application
├── config.yaml           # Configuration file
├── requirements.txt      # Python dependencies
├── Dockerfile           # Container image
├── docker-compose.yml   # Docker Compose config
├── README.md            # This file
└── tests/               # Test suite
    ├── test_chat_completions.py
    ├── test_model_mapping.py
    ├── test_streaming.py
    └── test_error_handling.py
```

## Production Deployment

### Systemd Service (Linux)

Create `/etc/systemd/system/openai-proxy.service`:

```ini
[Unit]
Description=OpenAI Local Proxy
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=/opt/openai-local-proxy
ExecStart=/usr/bin/python3 /opt/openai-local-proxy/proxy_server.py
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl enable openai-proxy
sudo systemctl start openai-proxy
sudo systemctl status openai-proxy
```

### Reverse Proxy (Nginx)

```nginx
server {
    listen 80;
    server_name openai-proxy.local;

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_buffering off;  # Important for streaming
    }
}
```

### Environment Variables

Override config with environment variables:

```bash
export BACKEND_URL=http://10.50.10.14:1234
export SERVER_PORT=8080
export LOG_LEVEL=INFO

python proxy_server.py
```

## Security Considerations

### Local Deployment Only

This proxy is designed for LOCAL deployment on trusted networks:

- ⚠️ **Do NOT expose to the internet** without proper security
- ⚠️ **API key validation is optional** and basic
- ⚠️ **No rate limiting** by default
- ⚠️ **No request sanitization** beyond basic validation

### Recommended Security Measures

1. **Network isolation**: Run on local network only
2. **Firewall**: Restrict access to trusted IPs
3. **Reverse proxy**: Use Nginx/Apache with SSL
4. **API keys**: Enable authentication in production
5. **Logging**: Monitor for unusual activity

## Contributing

Contributions welcome! Please ensure:

1. Tests pass: `pytest tests/`
2. Code is formatted: `black proxy_server.py`
3. Type hints are used
4. Documentation is updated

## License

MIT License - See repository root for details

## Support

- **Issues**: Open an issue on GitHub
- **Documentation**: See `/docs/` directory
- **Examples**: See `/examples/` directory

## Related Components

- **C# Library**: See `/OpenAI.LocalAdapter/` for native C# client
- **Migration Guide**: See `/docs/MIGRATION_GUIDE.md`
- **Configuration Templates**: See `/config-templates/`
