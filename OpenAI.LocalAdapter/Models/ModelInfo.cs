using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpenAI.LocalAdapter.Models
{
    /// <summary>
    /// Represents information about an available model
    /// </summary>
    public class ModelInfo
    {
        /// <summary>
        /// Model identifier
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Object type (always "model")
        /// </summary>
        [JsonProperty("object")]
        public string Object { get; set; } = "model";

        /// <summary>
        /// Unix timestamp of when the model was created
        /// </summary>
        [JsonProperty("created")]
        public long Created { get; set; }

        /// <summary>
        /// Owner of the model
        /// </summary>
        [JsonProperty("owned_by")]
        public string OwnedBy { get; set; } = "local";
    }

    /// <summary>
    /// Response from listing models
    /// </summary>
    public class ModelListResponse
    {
        /// <summary>
        /// Object type (always "list")
        /// </summary>
        [JsonProperty("object")]
        public string Object { get; set; } = "list";

        /// <summary>
        /// List of available models
        /// </summary>
        [JsonProperty("data")]
        public List<ModelInfo> Data { get; set; } = new();
    }
}
