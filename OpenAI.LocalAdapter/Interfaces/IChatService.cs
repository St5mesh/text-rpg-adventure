using OpenAI.LocalAdapter.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI.LocalAdapter.Interfaces
{
    /// <summary>
    /// Interface for chat completion operations
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Creates a chat completion
        /// </summary>
        /// <param name="request">The chat completion request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Chat completion response</returns>
        Task<ChatCompletionResponse> CreateCompletionAsync(
            ChatCompletionRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a streaming chat completion
        /// </summary>
        /// <param name="request">The chat completion request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Async enumerable of chat completion chunks</returns>
        IAsyncEnumerable<ChatCompletionResponse> CreateCompletionStreamAsync(
            ChatCompletionRequest request,
            CancellationToken cancellationToken = default);
    }
}
