"""
OpenAI Local Proxy Server

A FastAPI-based HTTP proxy that forwards OpenAI API requests to local LLM backends
(LM Studio, Ollama, etc.) while maintaining full OpenAI API compatibility.

This enables zero-code-change migration of existing OpenAI applications to local models.
"""

import asyncio
import json
import logging
import time
from typing import Any, Dict, List, Optional, AsyncGenerator
from pathlib import Path

import httpx
import yaml
from fastapi import FastAPI, Request, Response, HTTPException, Header, status
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import StreamingResponse, JSONResponse
from pydantic import BaseModel, Field
import uvicorn


# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


# Configuration Models
class ServerConfig(BaseModel):
    host: str = "0.0.0.0"
    port: int = 8080
    workers: int = 1
    log_level: str = "info"
    cors_enabled: bool = True
    cors_origins: List[str] = ["*"]


class BackendConfig(BaseModel):
    name: str
    url: str
    enabled: bool = True
    timeout: int = 300
    max_retries: int = 3
    retry_delay: int = 1


class AuthConfig(BaseModel):
    enabled: bool = False
    valid_api_keys: List[str] = []


class LoggingConfig(BaseModel):
    level: str = "INFO"
    format: str = "text"
    include_request_body: bool = False
    include_response_body: bool = False


class ProxyConfig(BaseModel):
    server: ServerConfig
    backends: Dict[str, BackendConfig]
    model_mapping: Dict[str, str] = {}
    default_model: str = "llama-3.1-instruct-13b"
    authentication: AuthConfig = AuthConfig()
    logging: LoggingConfig = LoggingConfig()


# Load configuration
def load_config(config_path: str = "config.yaml") -> ProxyConfig:
    """Load configuration from YAML file"""
    try:
        config_file = Path(config_path)
        if not config_file.exists():
            logger.warning(f"Config file {config_path} not found, using defaults")
            return ProxyConfig(
                server=ServerConfig(),
                backends={
                    "primary": BackendConfig(
                        name="LM Studio",
                        url="http://10.50.10.14:1234"
                    )
                }
            )
        
        with open(config_file, 'r') as f:
            config_data = yaml.safe_load(f)
        
        return ProxyConfig(**config_data)
    except Exception as e:
        logger.error(f"Error loading config: {e}")
        raise


# Global configuration
config = load_config()


# Initialize FastAPI app
app = FastAPI(
    title="OpenAI Local Proxy",
    description="Local proxy for OpenAI API compatible with LM Studio and other local LLM backends",
    version="1.0.0"
)


# Add CORS middleware
if config.server.cors_enabled:
    app.add_middleware(
        CORSMiddleware,
        allow_origins=config.server.cors_origins,
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )


# HTTP client for backend requests
http_client: Optional[httpx.AsyncClient] = None


@app.on_event("startup")
async def startup_event():
    """Initialize HTTP client on startup"""
    global http_client
    http_client = httpx.AsyncClient(
        timeout=httpx.Timeout(config.backends["primary"].timeout),
        limits=httpx.Limits(max_keepalive_connections=10, max_connections=100)
    )
    logger.info("OpenAI Local Proxy started")
    logger.info(f"Forwarding requests to: {config.backends['primary'].url}")


@app.on_event("shutdown")
async def shutdown_event():
    """Close HTTP client on shutdown"""
    global http_client
    if http_client:
        await http_client.aclose()
    logger.info("OpenAI Local Proxy stopped")


# Helper Functions
def map_model_name(model: str, reverse: bool = False) -> str:
    """
    Map between OpenAI model names and local model names
    
    Args:
        model: The model name to map
        reverse: If True, map from local to OpenAI names
    
    Returns:
        Mapped model name
    """
    if reverse:
        # Reverse mapping (local -> OpenAI)
        reverse_map = {v: k for k, v in config.model_mapping.items()}
        return reverse_map.get(model, model)
    else:
        # Forward mapping (OpenAI -> local)
        return config.model_mapping.get(model, model)


def validate_api_key(authorization: Optional[str]) -> bool:
    """
    Validate API key if authentication is enabled
    
    Args:
        authorization: Authorization header value
    
    Returns:
        True if valid or authentication disabled, False otherwise
    """
    if not config.authentication.enabled:
        return True
    
    if not authorization:
        return False
    
    # Extract Bearer token
    if authorization.startswith("Bearer "):
        token = authorization[7:]
        return token in config.authentication.valid_api_keys
    
    return False


def transform_request_body(body: Dict[str, Any]) -> Dict[str, Any]:
    """
    Transform OpenAI request to local backend format
    
    Args:
        body: Original request body
    
    Returns:
        Transformed request body
    """
    transformed = body.copy()
    
    # Map model name
    if "model" in transformed:
        transformed["model"] = map_model_name(transformed["model"])
    else:
        transformed["model"] = config.default_model
    
    # Log transformation if enabled
    if config.logging.include_request_body:
        logger.debug(f"Transformed request: {json.dumps(transformed, indent=2)}")
    
    return transformed


def transform_response_body(body: Dict[str, Any], original_model: str) -> Dict[str, Any]:
    """
    Transform local backend response to OpenAI format
    
    Args:
        body: Backend response body
        original_model: Original OpenAI model name from request
    
    Returns:
        Transformed response body
    """
    transformed = body.copy()
    
    # Map model name back to OpenAI format
    if "model" in transformed:
        transformed["model"] = original_model
    
    # Ensure required OpenAI fields exist
    if "object" not in transformed:
        if "choices" in transformed:
            transformed["object"] = "chat.completion"
    
    if "created" not in transformed:
        transformed["created"] = int(time.time())
    
    # Add usage stats if missing and enabled
    if config.response.get("add_usage_stats", True) and "usage" not in transformed:
        if "choices" in transformed and len(transformed["choices"]) > 0:
            # Estimate token usage (rough approximation)
            content = ""
            if "message" in transformed["choices"][0]:
                content = transformed["choices"][0]["message"].get("content", "")
            elif "text" in transformed["choices"][0]:
                content = transformed["choices"][0]["text"]
            
            tokens = len(content.split())  # Very rough estimate
            transformed["usage"] = {
                "prompt_tokens": 0,
                "completion_tokens": tokens,
                "total_tokens": tokens
            }
    
    # Log transformation if enabled
    if config.logging.include_response_body:
        logger.debug(f"Transformed response: {json.dumps(transformed, indent=2)}")
    
    return transformed


async def forward_request(
    method: str,
    path: str,
    body: Optional[Dict[str, Any]] = None,
    headers: Optional[Dict[str, str]] = None,
    stream: bool = False
) -> httpx.Response:
    """
    Forward request to backend server
    
    Args:
        method: HTTP method
        path: API endpoint path
        body: Request body
        headers: Request headers
        stream: Whether to stream the response
    
    Returns:
        Backend response
    """
    backend_url = config.backends["primary"].url
    url = f"{backend_url}{path}"
    
    # Remove authorization header when forwarding
    forward_headers = headers.copy() if headers else {}
    forward_headers.pop("authorization", None)
    forward_headers.pop("Authorization", None)
    
    logger.info(f"Forwarding {method} {path} to {url}")
    
    try:
        if method == "GET":
            response = await http_client.get(url, headers=forward_headers)
        elif method == "POST":
            if stream:
                response = await http_client.post(
                    url,
                    json=body,
                    headers=forward_headers,
                    timeout=None  # No timeout for streaming
                )
            else:
                response = await http_client.post(url, json=body, headers=forward_headers)
        elif method == "DELETE":
            response = await http_client.delete(url, headers=forward_headers)
        else:
            raise HTTPException(status_code=405, detail=f"Method {method} not supported")
        
        return response
    
    except httpx.TimeoutException:
        logger.error(f"Timeout forwarding request to {url}")
        raise HTTPException(status_code=504, detail="Backend timeout")
    except httpx.RequestError as e:
        logger.error(f"Error forwarding request to {url}: {e}")
        raise HTTPException(status_code=502, detail=f"Backend error: {str(e)}")


async def stream_response(response: httpx.Response, original_model: str) -> AsyncGenerator[str, None]:
    """
    Stream response from backend, transforming each chunk
    
    Args:
        response: Backend response
        original_model: Original OpenAI model name from request
    
    Yields:
        Transformed SSE chunks
    """
    async for line in response.aiter_lines():
        if not line.strip():
            continue
        
        # Handle SSE format
        if line.startswith("data: "):
            data = line[6:]
            
            if data.strip() == "[DONE]":
                yield f"data: [DONE]\n\n"
                continue
            
            try:
                chunk = json.loads(data)
                # Transform model name in chunk
                if "model" in chunk:
                    chunk["model"] = original_model
                
                yield f"data: {json.dumps(chunk)}\n\n"
            except json.JSONDecodeError:
                # Pass through if not valid JSON
                yield f"data: {data}\n\n"


# API Endpoints

@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {"status": "healthy", "backend": config.backends["primary"].url}


@app.get("/v1/models")
async def list_models(authorization: Optional[str] = Header(None)):
    """List available models"""
    if not validate_api_key(authorization):
        raise HTTPException(status_code=401, detail="Invalid API key")
    
    try:
        response = await forward_request("GET", "/v1/models", headers={"authorization": authorization or ""})
        
        if response.status_code == 200:
            data = response.json()
            # Transform model names in response
            if "data" in data:
                for model in data["data"]:
                    if "id" in model:
                        model["id"] = map_model_name(model["id"], reverse=True)
            return data
        else:
            raise HTTPException(status_code=response.status_code, detail=response.text)
    
    except Exception as e:
        logger.error(f"Error listing models: {e}")
        # Fallback: return default model list
        return {
            "object": "list",
            "data": [
                {
                    "id": "gpt-3.5-turbo",
                    "object": "model",
                    "created": int(time.time()),
                    "owned_by": "local"
                }
            ]
        }


@app.post("/v1/chat/completions")
async def chat_completions(request: Request, authorization: Optional[str] = Header(None)):
    """
    Handle chat completion requests
    
    This is the CRITICAL endpoint used by the text-rpg-adventure game
    """
    if not validate_api_key(authorization):
        raise HTTPException(status_code=401, detail="Invalid API key")
    
    try:
        # Parse request body
        body = await request.json()
        original_model = body.get("model", "gpt-3.5-turbo")
        stream = body.get("stream", False)
        
        # Transform request
        transformed_body = transform_request_body(body)
        
        # Forward request
        response = await forward_request(
            "POST",
            "/v1/chat/completions",
            body=transformed_body,
            headers={"authorization": authorization or ""},
            stream=stream
        )
        
        # Handle streaming response
        if stream:
            return StreamingResponse(
                stream_response(response, original_model),
                media_type="text/event-stream"
            )
        
        # Handle non-streaming response
        if response.status_code == 200:
            response_data = response.json()
            transformed_response = transform_response_body(response_data, original_model)
            return JSONResponse(content=transformed_response)
        else:
            raise HTTPException(status_code=response.status_code, detail=response.text)
    
    except json.JSONDecodeError:
        raise HTTPException(status_code=400, detail="Invalid JSON in request body")
    except Exception as e:
        logger.error(f"Error in chat completions: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/v1/completions")
async def completions(request: Request, authorization: Optional[str] = Header(None)):
    """Handle text completion requests"""
    if not validate_api_key(authorization):
        raise HTTPException(status_code=401, detail="Invalid API key")
    
    try:
        body = await request.json()
        original_model = body.get("model", "text-davinci-003")
        stream = body.get("stream", False)
        
        transformed_body = transform_request_body(body)
        
        response = await forward_request(
            "POST",
            "/v1/completions",
            body=transformed_body,
            headers={"authorization": authorization or ""},
            stream=stream
        )
        
        if stream:
            return StreamingResponse(
                stream_response(response, original_model),
                media_type="text/event-stream"
            )
        
        if response.status_code == 200:
            response_data = response.json()
            transformed_response = transform_response_body(response_data, original_model)
            return JSONResponse(content=transformed_response)
        else:
            raise HTTPException(status_code=response.status_code, detail=response.text)
    
    except Exception as e:
        logger.error(f"Error in completions: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/v1/embeddings")
async def embeddings(request: Request, authorization: Optional[str] = Header(None)):
    """Handle embedding requests"""
    if not validate_api_key(authorization):
        raise HTTPException(status_code=401, detail="Invalid API key")
    
    try:
        body = await request.json()
        original_model = body.get("model", "text-embedding-ada-002")
        
        transformed_body = transform_request_body(body)
        
        response = await forward_request(
            "POST",
            "/v1/embeddings",
            body=transformed_body,
            headers={"authorization": authorization or ""}
        )
        
        if response.status_code == 200:
            response_data = response.json()
            transformed_response = transform_response_body(response_data, original_model)
            return JSONResponse(content=transformed_response)
        else:
            raise HTTPException(status_code=response.status_code, detail=response.text)
    
    except Exception as e:
        logger.error(f"Error in embeddings: {e}")
        raise HTTPException(status_code=500, detail=str(e))


# Catch-all for other endpoints
@app.api_route("/{path:path}", methods=["GET", "POST", "PUT", "DELETE", "PATCH"])
async def catch_all(request: Request, path: str, authorization: Optional[str] = Header(None)):
    """
    Catch-all handler for other OpenAI API endpoints
    
    Forwards any unhandled endpoint to the backend
    """
    if not validate_api_key(authorization):
        raise HTTPException(status_code=401, detail="Invalid API key")
    
    try:
        method = request.method
        full_path = f"/{path}"
        
        # Get request body if present
        body = None
        if method in ["POST", "PUT", "PATCH"]:
            try:
                body = await request.json()
                if "model" in body:
                    body = transform_request_body(body)
            except:
                pass
        
        response = await forward_request(
            method,
            full_path,
            body=body,
            headers={"authorization": authorization or ""}
        )
        
        if response.status_code == 200:
            try:
                return response.json()
            except:
                return Response(content=response.content, status_code=response.status_code)
        else:
            raise HTTPException(status_code=response.status_code, detail=response.text)
    
    except Exception as e:
        logger.error(f"Error in catch-all handler for {path}: {e}")
        raise HTTPException(status_code=500, detail=str(e))


def main():
    """Run the proxy server"""
    uvicorn.run(
        "proxy_server:app",
        host=config.server.host,
        port=config.server.port,
        workers=config.server.workers,
        log_level=config.server.log_level,
        reload=False
    )


if __name__ == "__main__":
    main()
