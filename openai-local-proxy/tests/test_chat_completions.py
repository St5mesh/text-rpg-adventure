"""
Tests for chat completions endpoint
"""

import pytest
import json
from httpx import AsyncClient
from unittest.mock import patch, AsyncMock


@pytest.mark.asyncio
async def test_chat_completion_basic():
    """Test basic chat completion request"""
    from proxy_server import app
    
    async with AsyncClient(app=app, base_url="http://test") as client:
        # Mock the backend response
        with patch("proxy_server.http_client") as mock_client:
            mock_response = AsyncMock()
            mock_response.status_code = 200
            mock_response.json.return_value = {
                "id": "chatcmpl-123",
                "object": "chat.completion",
                "created": 1677652288,
                "model": "llama-3.1-instruct-13b",
                "choices": [{
                    "index": 0,
                    "message": {
                        "role": "assistant",
                        "content": "Hello! How can I help you?"
                    },
                    "finish_reason": "stop"
                }],
                "usage": {
                    "prompt_tokens": 10,
                    "completion_tokens": 9,
                    "total_tokens": 19
                }
            }
            mock_client.post.return_value = mock_response
            
            response = await client.post(
                "/v1/chat/completions",
                json={
                    "model": "gpt-3.5-turbo",
                    "messages": [
                        {"role": "user", "content": "Hello"}
                    ]
                }
            )
            
            assert response.status_code == 200
            data = response.json()
            assert data["model"] == "gpt-3.5-turbo"  # Should be mapped back
            assert "choices" in data
            assert len(data["choices"]) > 0
            assert data["choices"][0]["message"]["content"]


@pytest.mark.asyncio
async def test_chat_completion_with_stream():
    """Test streaming chat completion request"""
    from proxy_server import app
    
    async with AsyncClient(app=app, base_url="http://test") as client:
        with patch("proxy_server.http_client") as mock_client:
            # Mock streaming response
            mock_response = AsyncMock()
            mock_response.status_code = 200
            
            async def mock_iter_lines():
                yield "data: " + json.dumps({
                    "id": "chatcmpl-123",
                    "object": "chat.completion.chunk",
                    "created": 1677652288,
                    "model": "llama-3.1-instruct-13b",
                    "choices": [{
                        "index": 0,
                        "delta": {"content": "Hello"},
                        "finish_reason": None
                    }]
                })
                yield "data: [DONE]"
            
            mock_response.aiter_lines = mock_iter_lines
            mock_client.post.return_value = mock_response
            
            response = await client.post(
                "/v1/chat/completions",
                json={
                    "model": "gpt-3.5-turbo",
                    "messages": [{"role": "user", "content": "Hello"}],
                    "stream": True
                }
            )
            
            assert response.status_code == 200
            assert "text/event-stream" in response.headers.get("content-type", "")


@pytest.mark.asyncio
async def test_chat_completion_with_parameters():
    """Test chat completion with various parameters"""
    from proxy_server import app
    
    async with AsyncClient(app=app, base_url="http://test") as client:
        with patch("proxy_server.http_client") as mock_client:
            mock_response = AsyncMock()
            mock_response.status_code = 200
            mock_response.json.return_value = {
                "id": "chatcmpl-123",
                "object": "chat.completion",
                "created": 1677652288,
                "model": "llama-3.1-instruct-13b",
                "choices": [{
                    "index": 0,
                    "message": {
                        "role": "assistant",
                        "content": "Test response"
                    },
                    "finish_reason": "stop"
                }],
                "usage": {
                    "prompt_tokens": 10,
                    "completion_tokens": 5,
                    "total_tokens": 15
                }
            }
            mock_client.post.return_value = mock_response
            
            response = await client.post(
                "/v1/chat/completions",
                json={
                    "model": "gpt-3.5-turbo",
                    "messages": [
                        {"role": "system", "content": "You are a helpful assistant"},
                        {"role": "user", "content": "Hello"}
                    ],
                    "temperature": 0.7,
                    "max_tokens": 500,
                    "top_p": 0.9,
                    "frequency_penalty": 0.0,
                    "presence_penalty": 0.0
                }
            )
            
            assert response.status_code == 200
            data = response.json()
            assert data["model"] == "gpt-3.5-turbo"


@pytest.mark.asyncio
async def test_chat_completion_invalid_request():
    """Test chat completion with invalid request"""
    from proxy_server import app
    
    async with AsyncClient(app=app, base_url="http://test") as client:
        # Missing messages field
        response = await client.post(
            "/v1/chat/completions",
            json={
                "model": "gpt-3.5-turbo"
            }
        )
        
        # Should still forward and let backend handle validation
        # or return error
        assert response.status_code in [400, 422, 500]
