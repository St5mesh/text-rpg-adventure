using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpenAI.LocalAdapter.Models
{
    /// <summary>
    /// Request for creating a chat completion
    /// </summary>
    public class ChatCompletionRequest
    {
        /// <summary>
        /// ID of the model to use
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; } = "gpt-3.5-turbo";

        /// <summary>
        /// List of messages in the conversation
        /// </summary>
        [JsonProperty("messages")]
        public List<ChatMessage> Messages { get; set; } = new();

        /// <summary>
        /// Sampling temperature (0-2). Higher values make output more random.
        /// </summary>
        [JsonProperty("temperature", NullValueHandling = NullValueHandling.Ignore)]
        public double? Temperature { get; set; }

        /// <summary>
        /// Nucleus sampling parameter. Alternative to temperature.
        /// </summary>
        [JsonProperty("top_p", NullValueHandling = NullValueHandling.Ignore)]
        public double? TopP { get; set; }

        /// <summary>
        /// Maximum number of tokens to generate
        /// </summary>
        [JsonProperty("max_tokens", NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Number of completions to generate
        /// </summary>
        [JsonProperty("n", NullValueHandling = NullValueHandling.Ignore)]
        public int? N { get; set; }

        /// <summary>
        /// Whether to stream the response
        /// </summary>
        [JsonProperty("stream", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Stream { get; set; }

        /// <summary>
        /// Sequences where the API will stop generating
        /// </summary>
        [JsonProperty("stop", NullValueHandling = NullValueHandling.Ignore)]
        public object? Stop { get; set; }

        /// <summary>
        /// Penalize new tokens based on their frequency in the text so far
        /// </summary>
        [JsonProperty("frequency_penalty", NullValueHandling = NullValueHandling.Ignore)]
        public double? FrequencyPenalty { get; set; }

        /// <summary>
        /// Penalize new tokens based on whether they appear in the text so far
        /// </summary>
        [JsonProperty("presence_penalty", NullValueHandling = NullValueHandling.Ignore)]
        public double? PresencePenalty { get; set; }

        /// <summary>
        /// User identifier for abuse monitoring
        /// </summary>
        [JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)]
        public string? User { get; set; }
    }
}
