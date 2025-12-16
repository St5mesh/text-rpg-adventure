using OpenAI.LocalAdapter.Interfaces;
using OpenAI.LocalAdapter.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI.LocalAdapter.Services
{
    /// <summary>
    /// Service for handling chat completion requests
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger? _logger;

        /// <summary>
        /// Initializes a new instance of the ChatService
        /// </summary>
        public ChatService(HttpClient httpClient, ILogger? logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<ChatCompletionResponse> CreateCompletionAsync(
            ChatCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Messages == null || request.Messages.Count == 0)
                throw new ArgumentException("At least one message is required", nameof(request));

            try
            {
                _logger?.LogDebug("Sending chat completion request for model: {Model}", request.Model);

                var response = await _httpClient.PostAsJsonAsync(
                    "/v1/chat/completions",
                    request,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
#if NETSTANDARD2_1
                    var errorContent = await response.Content.ReadAsStringAsync();
#else
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
#endif
                    _logger?.LogError("Chat completion request failed: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);

                    // Try to parse OpenAI error format
                    try
                    {
                        var error = JsonConvert.DeserializeObject<OpenAIError>(errorContent);
                        throw new HttpRequestException(
                            $"API request failed: {error?.Error?.Message ?? errorContent}");
                    }
                    catch (JsonException)
                    {
                        throw new HttpRequestException(
                            $"API request failed with status {response.StatusCode}: {errorContent}");
                    }
                }

                var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
                    cancellationToken: cancellationToken);

                if (result == null)
                    throw new InvalidOperationException("Received null response from API");

                _logger?.LogDebug("Chat completion successful: {ChoiceCount} choices, {TokenCount} total tokens",
                    result.Choices?.Count ?? 0,
                    result.Usage?.TotalTokens ?? 0);

                return result;
            }
            catch (Exception ex) when (ex is not ArgumentException && ex is not ArgumentNullException)
            {
                _logger?.LogError(ex, "Error creating chat completion");
                throw;
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<ChatCompletionResponse> CreateCompletionStreamAsync(
            ChatCompletionRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Messages == null || request.Messages.Count == 0)
                throw new ArgumentException("At least one message is required", nameof(request));

            // Ensure streaming is enabled
            request.Stream = true;

            _logger?.LogDebug("Sending streaming chat completion request for model: {Model}", request.Model);

            var jsonContent = JsonConvert.SerializeObject(request);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            {
                Content = httpContent
            };

            using var response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
#if NETSTANDARD2_1
                var errorContent = await response.Content.ReadAsStringAsync();
#else
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
#endif
                _logger?.LogError("Streaming chat completion request failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException(
                    $"API request failed with status {response.StatusCode}: {errorContent}");
            }

#if NETSTANDARD2_1
            using var stream = await response.Content.ReadAsStreamAsync();
#else
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
#endif
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Handle SSE format
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);

                    if (data.Trim() == "[DONE]")
                    {
                        _logger?.LogDebug("Streaming completion finished");
                        yield break;
                    }

                    ChatCompletionResponse? chunk;
                    try
                    {
                        chunk = JsonConvert.DeserializeObject<ChatCompletionResponse>(data);
                    }
                    catch (JsonException ex)
                    {
                        _logger?.LogWarning(ex, "Failed to parse streaming chunk: {Data}", data);
                        continue;
                    }

                    if (chunk != null)
                    {
                        yield return chunk;
                    }
                }
            }
        }
    }
}
