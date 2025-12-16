using Newtonsoft.Json;

namespace OpenAI.LocalAdapter.Models
{
    /// <summary>
    /// Represents a chat message in a conversation
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// The role of the message author (system, user, assistant, function)
        /// </summary>
        [JsonProperty("role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// The content of the message
        /// </summary>
        [JsonProperty("content")]
        public string? Content { get; set; }

        /// <summary>
        /// The name of the author (optional)
        /// </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }

        /// <summary>
        /// Function call information (for function calling)
        /// </summary>
        [JsonProperty("function_call", NullValueHandling = NullValueHandling.Ignore)]
        public object? FunctionCall { get; set; }

        /// <summary>
        /// Creates a system message
        /// </summary>
        public static ChatMessage System(string content) => new() { Role = "system", Content = content };

        /// <summary>
        /// Creates a user message
        /// </summary>
        public static ChatMessage User(string content) => new() { Role = "user", Content = content };

        /// <summary>
        /// Creates an assistant message
        /// </summary>
        public static ChatMessage Assistant(string content) => new() { Role = "assistant", Content = content };
    }
}
