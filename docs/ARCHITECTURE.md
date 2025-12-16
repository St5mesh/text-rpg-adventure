# Architecture Documentation

## System Overview

The OpenAI Local Replacement System provides a comprehensive solution for running OpenAI-compatible applications entirely locally, using LM Studio or other local LLM backends.

```
┌─────────────────────────────────────────────────────────────────┐
│                        Application Layer                         │
│  ┌──────────────────┐  ┌─────────────────┐  ┌────────────────┐ │
│  │  Text RPG Game   │  │   C# Apps with  │  │  Python Apps   │ │
│  │  (OpenAI_API)    │  │ LocalAdapter    │  │  (OpenAI SDK)  │ │
│  └────────┬─────────┘  └────────┬────────┘  └────────┬───────┘ │
└───────────┼────────────────────┼──────────────────────┼─────────┘
            │                    │                      │
            │ HTTP/JSON          │ HTTP/JSON            │ HTTP/JSON
            │                    │                      │
┌───────────▼────────────────────▼──────────────────────▼─────────┐
│                    OpenAI Local Proxy (FastAPI)                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  • Model Name Mapping                                    │   │
│  │  • Request/Response Translation                          │   │
│  │  • Streaming Support                                     │   │
│  │  • Authentication Handling                               │   │
│  │  • Error Normalization                                   │   │
│  └─────────────────────────────────────────────────────────┘   │
└────────────────────────────────┬─────────────────────────────────┘
                                 │ HTTP/JSON
                                 │
┌────────────────────────────────▼─────────────────────────────────┐
│                         LM Studio Backend                         │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  • Local LLM Model (Llama, etc.)                         │   │
│  │  • GPU Acceleration                                      │   │
│  │  • OpenAI-Compatible API                                 │   │
│  └─────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────┘
```

## Component Architecture

### 1. OpenAI Local Proxy (Python/FastAPI)

**Purpose**: Provides a transparent translation layer between OpenAI API and local backends.

**Key Responsibilities**:
- Accept OpenAI-formatted requests
- Translate model names (gpt-3.5-turbo → llama-3.1-instruct-13b)
- Forward requests to LM Studio
- Normalize responses to match OpenAI format
- Handle streaming responses
- Provide health checks and monitoring

**Technology Stack**:
- FastAPI (web framework)
- httpx (HTTP client)
- Pydantic (data validation)
- uvicorn (ASGI server)

**API Endpoints**:
```
GET  /health                       - Health check
GET  /v1/models                    - List available models
POST /v1/chat/completions          - Chat completions (streaming supported)
POST /v1/completions               - Text completions
POST /v1/embeddings                - Text embeddings
*    /{path}                       - Catch-all for other endpoints
```

**Data Flow**:
```
Client Request
    ↓
Request Validation
    ↓
Model Name Mapping (OpenAI → Local)
    ↓
Forward to Backend (LM Studio)
    ↓
Receive Response
    ↓
Model Name Mapping (Local → OpenAI)
    ↓
Response Normalization
    ↓
Return to Client
```

### 2. OpenAI.LocalAdapter (C# Library)

**Purpose**: Provides a native C# interface for local LLM backends with type safety and modern async patterns.

**Key Components**:

#### 2.1 OpenAILocalClient (Core Client)
```csharp
public class OpenAILocalClient : IDisposable
{
    public IChatService Chat { get; }
    public IConversationManager CreateConversation()
}
```

**Responsibilities**:
- Manage HTTP client lifecycle
- Configure connection to proxy/backend
- Provide service interfaces
- Handle authentication

#### 2.2 ChatService
```csharp
public class ChatService : IChatService
{
    Task<ChatCompletionResponse> CreateCompletionAsync(...)
    IAsyncEnumerable<ChatCompletionResponse> CreateCompletionStreamAsync(...)
}
```

**Responsibilities**:
- Execute chat completion requests
- Handle streaming responses
- Parse and validate responses
- Error handling and logging

#### 2.3 ConversationContext
```csharp
public class ConversationContext : IConversationManager
{
    void AppendSystemMessage(string message)
    void AppendUserInput(string message)
    Task<string> GetResponseAsync()
    IAsyncEnumerable<string> GetResponseStreamAsync()
}
```

**Responsibilities**:
- Maintain conversation history
- Automatically include context in requests
- Simplify multi-turn conversations
- Provide streaming support

**Class Diagram**:
```
┌─────────────────────────────┐
│    OpenAILocalClient        │
├─────────────────────────────┤
│ - HttpClient                │
│ - LocalAdapterConfig        │
├─────────────────────────────┤
│ + Chat: IChatService        │
│ + CreateConversation()      │
└──────────┬──────────────────┘
           │ creates
           ↓
┌─────────────────────────────┐     ┌──────────────────────────┐
│      ChatService            │────▶│  ConversationContext     │
├─────────────────────────────┤     ├──────────────────────────┤
│ - HttpClient                │     │ - IChatService           │
│ - ILogger                   │     │ - List<ChatMessage>      │
├─────────────────────────────┤     ├──────────────────────────┤
│ + CreateCompletionAsync()   │     │ + AppendSystemMessage()  │
│ + CreateCompletionStream()  │     │ + AppendUserInput()      │
└─────────────────────────────┘     │ + GetResponseAsync()     │
                                    │ + GetResponseStreamAsync()│
                                    └──────────────────────────┘
```

### 3. Configuration System

**Proxy Configuration (config.yaml)**:
```yaml
server:
  host: "0.0.0.0"
  port: 8080
  
backends:
  primary:
    url: "http://10.50.10.14:1234"
    timeout: 300
    
model_mapping:
  "gpt-3.5-turbo": "llama-3.1-instruct-13b"
  "gpt-4": "llama-3.1-instruct-13b"
```

**C# Configuration (LocalAdapterConfig)**:
```csharp
var config = new LocalAdapterConfig
{
    BaseUrl = "http://localhost:8080",
    DefaultModel = "llama-3.1-instruct-13b",
    TimeoutSeconds = 300,
    MaxRetries = 3
};
```

## Request/Response Flow

### Non-Streaming Request

```
┌──────────┐                                                  ┌──────────┐
│  Client  │                                                  │  LM      │
│  (C#)    │                                                  │  Studio  │
└─────┬────┘                                                  └─────┬────┘
      │                                                             │
      │ 1. POST /v1/chat/completions                              │
      │    {"model": "gpt-3.5-turbo", ...}                        │
      ├──────────────────────────────────────────────────────┐   │
      │                                                       │   │
      │                                           ┌───────────▼───▼────┐
      │                                           │   Proxy             │
      │                                           ├─────────────────────┤
      │                                           │ 2. Map model name   │
      │                                           │    gpt-3.5-turbo    │
      │                                           │    → llama-3.1...   │
      │                                           └───────────┬─────────┘
      │                                                       │
      │                                                       │ 3. Forward
      │                                                       │
      │                                                       ├─────────▶
      │                                                       │
      │                                                       │ 4. Response
      │                                           ┌───────────◀─────────┐
      │                                           │   Proxy             │
      │                                           ├─────────────────────┤
      │                                           │ 5. Map model back   │
      │                                           │    llama-3.1...     │
      │ 6. Response                               │    → gpt-3.5-turbo  │
      │    {"model": "gpt-3.5-turbo", ...}        └───────────┬─────────┘
      ◀──────────────────────────────────────────────────────┘
      │
```

### Streaming Request

```
┌──────────┐                                                  ┌──────────┐
│  Client  │                                                  │  LM      │
│  (C#)    │                                                  │  Studio  │
└─────┬────┘                                                  └─────┬────┘
      │                                                             │
      │ 1. POST /v1/chat/completions                              │
      │    {"model": "gpt-3.5-turbo", "stream": true}             │
      ├──────────────────────────────────────────────────────┐   │
      │                                                       │   │
      │                                           ┌───────────▼───▼────┐
      │                                           │   Proxy             │
      │                                           ├─────────────────────┤
      │                                           │ 2. Map model name   │
      │                                           │ 3. Forward with     │
      │                                           │    streaming        │
      │                                           └───────────┬─────────┘
      │                                                       │
      │                                                       ├─────────▶
      │                                                       │
      │ 4. Stream chunks                                     │ SSE Stream
      │    data: {"choices":[{"delta":{"content":"H"}}]}     ◀─────────┤
      ◀──────────────────────────────────────────────────────┤         │
      │                                                       │         │
      │    data: {"choices":[{"delta":{"content":"e"}}]}     ◀─────────┤
      ◀──────────────────────────────────────────────────────┤         │
      │                                                       │         │
      │    data: {"choices":[{"delta":{"content":"llo"}}]}   ◀─────────┤
      ◀──────────────────────────────────────────────────────┤         │
      │                                                       │         │
      │    data: [DONE]                                      ◀─────────┤
      ◀──────────────────────────────────────────────────────┘         │
      │
```

## Model Mapping Strategy

### Forward Mapping (Request)

When a request comes in with an OpenAI model name, it's translated to the local model:

```python
def map_model_name(model: str) -> str:
    mapping = {
        "gpt-3.5-turbo": "llama-3.1-instruct-13b",
        "gpt-4": "llama-3.1-instruct-13b",
        "gpt-4-turbo": "llama-3.1-instruct-13b",
    }
    return mapping.get(model, model)
```

### Reverse Mapping (Response)

Responses from the backend have the model name mapped back to OpenAI format:

```python
def map_model_name_reverse(model: str) -> str:
    # Map back to the first matching OpenAI model
    reverse_mapping = {
        "llama-3.1-instruct-13b": "gpt-3.5-turbo"
    }
    return reverse_mapping.get(model, model)
```

This ensures that applications receive responses in the exact format they expect.

## Error Handling

### Error Flow

```
┌──────────┐
│  Client  │
└─────┬────┘
      │
      │ Request
      ↓
┌─────────────┐
│   Proxy     │
├─────────────┤
│ Try:        │
│   Forward   │──────────┐
│ Except:     │          │
│   Log       │          │
│   Transform │          │ Network Error
│   Return    │          │ Timeout
└─────┬───────┘          │ Invalid Response
      │                  │
      │ Error Response   │
      │ (OpenAI format)  │
      ↓                  ↓
┌──────────┐      ┌──────────┐
│  Client  │      │  Backend │
│  Receives│      │  Error   │
│  Error   │      └──────────┘
└──────────┘
```

### Error Types and Handling

| Error Type | Proxy Handling | Client Handling |
|-----------|----------------|-----------------|
| Connection Refused | Return 502 Bad Gateway | Retry with backoff |
| Timeout | Return 504 Gateway Timeout | Increase timeout / Retry |
| Invalid Model | Return 400 Bad Request | Check model mapping |
| Backend Error | Forward with normalization | Parse error message |
| Rate Limit | Return 429 Too Many Requests | Implement backoff |

## Performance Characteristics

### Latency Breakdown

```
Total Request Time = Client → Proxy + Proxy Processing + Proxy → Backend + 
                     Backend Processing + Backend → Proxy + Proxy → Client

Typical values:
- Client → Proxy: 1-10ms (local network)
- Proxy Processing: 5-20ms
- Proxy → Backend: 1-10ms (local network)
- Backend Processing: 100-5000ms (depends on model/GPU)
- Backend → Proxy: 1-10ms
- Proxy → Client: 1-10ms

Total Overhead: ~10-60ms (non-LLM time)
```

### Optimization Strategies

1. **Connection Pooling**: Reuse HTTP connections
2. **Request Batching**: Group multiple requests when possible
3. **Streaming**: Reduce time-to-first-token
4. **Caching**: Cache repeated requests (optional)
5. **Load Balancing**: Multiple backend instances

## Security Considerations

### Threat Model

**Assumptions**:
- System runs on trusted local network
- No exposure to internet
- Users are trusted

**Threats**:
- ❌ Unauthorized access (Low risk - local only)
- ❌ Data exfiltration (Not applicable - no external connections)
- ⚠️  Resource exhaustion (Possible with large requests)
- ⚠️  Model prompt injection (Inherent to LLMs)

### Security Measures

1. **Network Isolation**: Run on localhost or private network only
2. **Optional Authentication**: API key validation if needed
3. **Request Validation**: Validate all inputs
4. **Rate Limiting**: Prevent resource exhaustion (optional)
5. **Logging**: Monitor for unusual activity

### Security Best Practices

```yaml
# config.yaml - Production settings
authentication:
  enabled: true
  valid_api_keys:
    - "sk-secure-random-key-123"

request:
  max_content_length: 1048576  # 1MB limit
  default_timeout: 300

logging:
  level: "INFO"
  include_request_body: false  # Don't log sensitive data
  include_response_body: false
```

## Scalability

### Vertical Scaling

Single instance with powerful hardware:
- Increase GPU memory
- Add more GPUs
- Increase CPU cores
- Increase RAM

### Horizontal Scaling

Multiple backend instances:

```yaml
backends:
  primary:
    url: "http://backend1:1234"
  secondary:
    url: "http://backend2:1234"
  tertiary:
    url: "http://backend3:1234"
```

Load balancing strategies:
- Round-robin
- Least connections
- Weighted distribution
- Health-based routing

### Performance Metrics

Monitor these metrics:
- Request rate (req/s)
- Average response time (ms)
- P95/P99 latency
- Error rate (%)
- Backend utilization (%)
- GPU utilization (%)
- Memory usage

## Deployment Architectures

### Development (Single Machine)

```
┌─────────────────────────────────────┐
│         Developer Machine           │
│  ┌────────────┐  ┌───────────────┐ │
│  │ Application│  │  Proxy        │ │
│  │ :3000      │  │  :8080        │ │
│  └────────────┘  └───────────────┘ │
│         │               │           │
│         └───────┬───────┘           │
│                 │                   │
│         ┌───────▼───────┐           │
│         │  LM Studio    │           │
│         │  :1234        │           │
│         └───────────────┘           │
└─────────────────────────────────────┘
```

### Production (Docker Compose)

```
┌─────────────────────────────────────┐
│         Docker Network              │
│  ┌────────────┐  ┌───────────────┐ │
│  │ app:3000   │  │ proxy:8080    │ │
│  └────────────┘  └───────────────┘ │
│         │               │           │
│         └───────┬───────┘           │
│                 │                   │
│         ┌───────▼───────┐           │
│         │  LM Studio    │           │
│         │  (external)   │           │
│         └───────────────┘           │
└─────────────────────────────────────┘
```

### Enterprise (Kubernetes)

```
┌────────────────────────────────────────────┐
│            Kubernetes Cluster              │
│  ┌──────────────────────────────────────┐ │
│  │          Ingress Controller          │ │
│  └────────────────┬─────────────────────┘ │
│                   │                        │
│  ┌────────────────▼─────────────────────┐ │
│  │      OpenAI Proxy Service            │ │
│  │      (LoadBalancer)                  │ │
│  │  ┌──────┐ ┌──────┐ ┌──────┐         │ │
│  │  │ Pod1 │ │ Pod2 │ │ Pod3 │         │ │
│  │  └──────┘ └──────┘ └──────┘         │ │
│  └────────────────┬─────────────────────┘ │
│                   │                        │
│  ┌────────────────▼─────────────────────┐ │
│  │      LM Studio Backend Service       │ │
│  │  ┌──────┐ ┌──────┐ ┌──────┐         │ │
│  │  │ GPU1 │ │ GPU2 │ │ GPU3 │         │ │
│  │  └──────┘ └──────┘ └──────┘         │ │
│  └──────────────────────────────────────┘ │
└────────────────────────────────────────────┘
```

## Monitoring and Observability

### Metrics to Track

**Proxy Metrics**:
- Request count by endpoint
- Response time distribution
- Error rate by type
- Active connections
- Throughput (MB/s)

**Backend Metrics**:
- GPU utilization
- GPU memory usage
- Model inference time
- Queue depth
- Temperature/power

**Application Metrics**:
- API call success rate
- Average response time
- Cache hit rate (if applicable)
- User session duration

### Logging Strategy

```python
# Proxy logging levels
DEBUG: Request/response bodies, full traces
INFO:  Request metadata, status codes
WARN:  Slow requests, retry attempts
ERROR: Failed requests, exceptions
```

### Health Checks

```python
# Proxy health check endpoint
@app.get("/health")
async def health_check():
    return {
        "status": "healthy",
        "backend": backend_url,
        "uptime": get_uptime(),
        "version": "1.0.0"
    }
```

## Future Enhancements

### Planned Features

1. **Multi-Backend Routing**: Intelligent routing based on model type
2. **Response Caching**: Cache identical requests
3. **Request Queuing**: Queue management for high load
4. **A/B Testing**: Route requests to different models
5. **Fine-tuning Support**: Support for fine-tuned models
6. **Function Calling**: Full function calling support
7. **Embeddings**: Dedicated embedding service
8. **Batch Processing**: Efficient batch request handling

### Extensibility Points

1. **Custom Backends**: Plugin system for new backends
2. **Custom Middleware**: Request/response transformations
3. **Custom Auth**: Pluggable authentication
4. **Custom Monitoring**: Exporters for Prometheus, DataDog, etc.

## Related Documentation

- **User Guide**: `/docs/MIGRATION_GUIDE.md`
- **API Reference**: `/docs/API_REFERENCE.md`
- **Troubleshooting**: `/docs/TROUBLESHOOTING.md`
- **Security**: `/docs/SECURITY.md`
- **Performance**: `/docs/PERFORMANCE.md`
