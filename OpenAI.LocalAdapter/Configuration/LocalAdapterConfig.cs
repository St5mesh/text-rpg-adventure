using System;

namespace OpenAI.LocalAdapter.Configuration
{
    /// <summary>
    /// Configuration for the OpenAI Local Adapter
    /// </summary>
    public class LocalAdapterConfig
    {
        /// <summary>
        /// Base URL of the local LLM backend or proxy
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:8080";

        /// <summary>
        /// API key (optional, may not be needed for local backends)
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Default model to use if not specified in requests
        /// </summary>
        public string DefaultModel { get; set; } = "llama-3.1-instruct-13b";

        /// <summary>
        /// Timeout for HTTP requests in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Maximum number of retry attempts for failed requests
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in milliseconds
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Whether to validate SSL certificates (set to false for local development)
        /// </summary>
        public bool ValidateSsl { get; set; } = true;

        /// <summary>
        /// Organization ID (optional)
        /// </summary>
        public string? Organization { get; set; }

        /// <summary>
        /// Backend provider type
        /// </summary>
        public BackendProvider Provider { get; set; } = BackendProvider.Proxy;

        /// <summary>
        /// Creates a configuration for LM Studio direct connection
        /// </summary>
        public static LocalAdapterConfig ForLMStudio(string url = "http://10.50.10.14:1234") => new()
        {
            BaseUrl = url,
            Provider = BackendProvider.LMStudio,
            ValidateSsl = false
        };

        /// <summary>
        /// Creates a configuration for the local proxy
        /// </summary>
        public static LocalAdapterConfig ForProxy(string url = "http://localhost:8080") => new()
        {
            BaseUrl = url,
            Provider = BackendProvider.Proxy,
            ValidateSsl = false
        };

        /// <summary>
        /// Creates a configuration for Ollama
        /// </summary>
        public static LocalAdapterConfig ForOllama(string url = "http://localhost:11434") => new()
        {
            BaseUrl = url,
            Provider = BackendProvider.Ollama,
            ValidateSsl = false
        };
    }
}
