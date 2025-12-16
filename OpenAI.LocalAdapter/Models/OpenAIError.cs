using Newtonsoft.Json;

namespace OpenAI.LocalAdapter.Models
{
    /// <summary>
    /// Represents an error from the OpenAI API
    /// </summary>
    public class OpenAIError
    {
        /// <summary>
        /// Error details
        /// </summary>
        [JsonProperty("error")]
        public ErrorDetails? Error { get; set; }
    }

    /// <summary>
    /// Detailed error information
    /// </summary>
    public class ErrorDetails
    {
        /// <summary>
        /// Error message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Error type
        /// </summary>
        [JsonProperty("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Error code
        /// </summary>
        [JsonProperty("code")]
        public string? Code { get; set; }

        /// <summary>
        /// Parameter that caused the error
        /// </summary>
        [JsonProperty("param")]
        public string? Param { get; set; }
    }
}
