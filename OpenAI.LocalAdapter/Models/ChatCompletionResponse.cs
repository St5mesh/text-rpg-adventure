using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpenAI.LocalAdapter.Models
{
    /// <summary>
    /// Response from a chat completion request
    /// </summary>
    public class ChatCompletionResponse
    {
        /// <summary>
        /// Unique identifier for the completion
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Object type (always "chat.completion")
        /// </summary>
        [JsonProperty("object")]
        public string Object { get; set; } = "chat.completion";

        /// <summary>
        /// Unix timestamp of when the completion was created
        /// </summary>
        [JsonProperty("created")]
        public long Created { get; set; }

        /// <summary>
        /// Model used for the completion
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// List of completion choices
        /// </summary>
        [JsonProperty("choices")]
        public List<ChatChoice> Choices { get; set; } = new();

        /// <summary>
        /// Token usage statistics
        /// </summary>
        [JsonProperty("usage", NullValueHandling = NullValueHandling.Ignore)]
        public UsageInfo? Usage { get; set; }
    }

    /// <summary>
    /// A single choice in a chat completion
    /// </summary>
    public class ChatChoice
    {
        /// <summary>
        /// Index of this choice
        /// </summary>
        [JsonProperty("index")]
        public int Index { get; set; }

        /// <summary>
        /// The chat message generated
        /// </summary>
        [JsonProperty("message")]
        public ChatMessage? Message { get; set; }

        /// <summary>
        /// Delta for streaming responses
        /// </summary>
        [JsonProperty("delta", NullValueHandling = NullValueHandling.Ignore)]
        public ChatMessage? Delta { get; set; }

        /// <summary>
        /// Reason the completion finished
        /// </summary>
        [JsonProperty("finish_reason")]
        public string? FinishReason { get; set; }
    }

    /// <summary>
    /// Token usage information
    /// </summary>
    public class UsageInfo
    {
        /// <summary>
        /// Number of tokens in the prompt
        /// </summary>
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// Number of tokens in the completion
        /// </summary>
        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Total number of tokens used
        /// </summary>
        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
