namespace OpenAI.LocalAdapter.Configuration
{
    /// <summary>
    /// Types of backend providers supported
    /// </summary>
    public enum BackendProvider
    {
        /// <summary>
        /// OpenAI Local Proxy (recommended)
        /// </summary>
        Proxy,

        /// <summary>
        /// LM Studio direct connection
        /// </summary>
        LMStudio,

        /// <summary>
        /// Ollama direct connection
        /// </summary>
        Ollama,

        /// <summary>
        /// LocalAI direct connection
        /// </summary>
        LocalAI,

        /// <summary>
        /// Custom backend
        /// </summary>
        Custom
    }
}
