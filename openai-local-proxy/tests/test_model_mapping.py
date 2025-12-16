"""
Tests for model mapping functionality
"""

import pytest
from proxy_server import map_model_name, load_config


def test_forward_model_mapping():
    """Test OpenAI to local model name mapping"""
    assert map_model_name("gpt-3.5-turbo") == "llama-3.1-instruct-13b"
    assert map_model_name("gpt-4") == "llama-3.1-instruct-13b"
    assert map_model_name("gpt-4-turbo") == "llama-3.1-instruct-13b"


def test_reverse_model_mapping():
    """Test local to OpenAI model name mapping"""
    assert map_model_name("llama-3.1-instruct-13b", reverse=True) == "gpt-3.5-turbo"


def test_unmapped_model():
    """Test behavior with unmapped model names"""
    # Should return the original name
    assert map_model_name("some-random-model") == "some-random-model"
    assert map_model_name("some-random-model", reverse=True) == "some-random-model"


def test_model_mapping_bidirectional():
    """Test bidirectional model mapping"""
    openai_model = "gpt-4-turbo-preview"
    local_model = map_model_name(openai_model)
    mapped_back = map_model_name(local_model, reverse=True)
    
    # Should map to local and back to an OpenAI model
    assert local_model == "llama-3.1-instruct-13b"
    assert mapped_back in ["gpt-3.5-turbo", "gpt-4", "gpt-4-turbo", "gpt-4-turbo-preview"]


def test_config_loading():
    """Test configuration loading"""
    config = load_config("config.yaml")
    
    assert config.server.port == 8080
    assert "primary" in config.backends
    assert config.backends["primary"].url == "http://10.50.10.14:1234"
    assert len(config.model_mapping) > 0
    assert "gpt-3.5-turbo" in config.model_mapping
