using OpenAI.LocalAdapter.Configuration;
using OpenAI.LocalAdapter.Interfaces;
using OpenAI.LocalAdapter.Services;
using OpenAI.LocalAdapter.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace OpenAI.LocalAdapter
{
    /// <summary>
    /// Main client for interacting with local OpenAI-compatible APIs
    /// Provides a simple interface similar to the official OpenAI SDK
    /// </summary>
    public class OpenAILocalClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly LocalAdapterConfig _config;
        private readonly ILogger? _logger;
        private readonly bool _ownsHttpClient;

        /// <summary>
        /// Chat completion service
        /// </summary>
        public IChatService Chat { get; }

        /// <summary>
        /// Initializes a new instance with a configuration
        /// </summary>
        /// <param name="config">Configuration for the client</param>
        /// <param name="httpClient">Optional HTTP client (will be created if not provided)</param>
        /// <param name="logger">Optional logger</param>
        public OpenAILocalClient(
            LocalAdapterConfig config,
            HttpClient? httpClient = null,
            ILogger? logger = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;

            if (httpClient != null)
            {
                _httpClient = httpClient;
                _ownsHttpClient = false;
            }
            else
            {
                _httpClient = CreateHttpClient(config);
                _ownsHttpClient = true;
            }

            // Initialize services
            Chat = new ChatService(_httpClient, _logger);
        }

        /// <summary>
        /// Initializes a new instance with a base URL
        /// </summary>
        /// <param name="baseUrl">Base URL of the local API or proxy</param>
        /// <param name="apiKey">Optional API key</param>
        /// <param name="logger">Optional logger</param>
        public OpenAILocalClient(
            string baseUrl,
            string? apiKey = null,
            ILogger? logger = null)
            : this(new LocalAdapterConfig { BaseUrl = baseUrl, ApiKey = apiKey }, null, logger)
        {
        }

        /// <summary>
        /// Creates a new conversation context
        /// Similar to OpenAI_API's CreateConversation method
        /// </summary>
        /// <param name="model">Model to use for the conversation</param>
        /// <param name="temperature">Temperature setting</param>
        /// <param name="maxTokens">Maximum tokens for responses</param>
        /// <returns>A new conversation context</returns>
        public IConversationManager CreateConversation(
            string? model = null,
            double? temperature = null,
            int? maxTokens = null)
        {
            return new ConversationContext(
                Chat,
                model ?? _config.DefaultModel,
                temperature,
                maxTokens);
        }

        /// <summary>
        /// Creates an HTTP client configured for the local API
        /// </summary>
        private static HttpClient CreateHttpClient(LocalAdapterConfig config)
        {
            var handler = new HttpClientHandler();

            // Allow self-signed certificates for local development
            if (!config.ValidateSsl)
            {
                handler.ServerCertificateCustomValidationCallback =
                    (message, cert, chain, errors) => true;
            }

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(config.BaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds)
            };

            // Add authorization header if API key is provided
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
            }

            // Add organization header if provided
            if (!string.IsNullOrEmpty(config.Organization))
            {
                client.DefaultRequestHeaders.Add("OpenAI-Organization", config.Organization);
            }

            return client;
        }

        /// <summary>
        /// Disposes the client and its resources
        /// </summary>
        public void Dispose()
        {
            if (_ownsHttpClient)
            {
                _httpClient?.Dispose();
            }
        }
    }
}
