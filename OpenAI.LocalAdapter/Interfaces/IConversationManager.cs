using OpenAI.LocalAdapter.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI.LocalAdapter.Interfaces
{
    /// <summary>
    /// Interface for managing conversations with context
    /// </summary>
    public interface IConversationManager
    {
        /// <summary>
        /// Appends a system message to the conversation
        /// </summary>
        void AppendSystemMessage(string message);

        /// <summary>
        /// Appends a user input to the conversation
        /// </summary>
        void AppendUserInput(string message);

        /// <summary>
        /// Appends an assistant response to the conversation
        /// </summary>
        void AppendAssistantResponse(string message);

        /// <summary>
        /// Gets a response from the assistant based on the current conversation
        /// </summary>
        Task<string> GetResponseAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a streaming response from the assistant
        /// </summary>
        IAsyncEnumerable<string> GetResponseStreamAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears the conversation history
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets all messages in the conversation
        /// </summary>
        List<ChatMessage> GetMessages();
    }
}
