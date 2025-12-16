using OpenAI.LocalAdapter.Interfaces;
using OpenAI.LocalAdapter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI.LocalAdapter.Utilities
{
    /// <summary>
    /// Manages conversation context and provides a simple interface for chat interactions
    /// Similar to OpenAI_API's CreateConversation functionality
    /// </summary>
    public class ConversationContext : IConversationManager
    {
        private readonly IChatService _chatService;
        private readonly List<ChatMessage> _messages;
        private readonly string _model;
        private readonly double? _temperature;
        private readonly int? _maxTokens;

        /// <summary>
        /// Initializes a new conversation context
        /// </summary>
        /// <param name="chatService">The chat service to use for completions</param>
        /// <param name="model">The model to use for this conversation</param>
        /// <param name="temperature">Temperature setting for responses</param>
        /// <param name="maxTokens">Maximum tokens for responses</param>
        public ConversationContext(
            IChatService chatService,
            string model = "gpt-3.5-turbo",
            double? temperature = null,
            int? maxTokens = null)
        {
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _model = model;
            _temperature = temperature;
            _maxTokens = maxTokens;
            _messages = new List<ChatMessage>();
        }

        /// <inheritdoc/>
        public void AppendSystemMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty", nameof(message));

            _messages.Add(ChatMessage.System(message));
        }

        /// <inheritdoc/>
        public void AppendUserInput(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty", nameof(message));

            _messages.Add(ChatMessage.User(message));
        }

        /// <inheritdoc/>
        public void AppendAssistantResponse(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty", nameof(message));

            _messages.Add(ChatMessage.Assistant(message));
        }

        /// <inheritdoc/>
        public async Task<string> GetResponseAsync(CancellationToken cancellationToken = default)
        {
            if (_messages.Count == 0)
                throw new InvalidOperationException("No messages in conversation");

            var request = new ChatCompletionRequest
            {
                Model = _model,
                Messages = new List<ChatMessage>(_messages),
                Temperature = _temperature,
                MaxTokens = _maxTokens
            };

            var response = await _chatService.CreateCompletionAsync(request, cancellationToken);

            if (response.Choices == null || response.Choices.Count == 0)
                throw new InvalidOperationException("No choices returned from API");

            var assistantMessage = response.Choices[0].Message?.Content ?? string.Empty;

            // Automatically add the assistant's response to the conversation
            if (!string.IsNullOrWhiteSpace(assistantMessage))
            {
                _messages.Add(ChatMessage.Assistant(assistantMessage));
            }

            return assistantMessage;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<string> GetResponseStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_messages.Count == 0)
                throw new InvalidOperationException("No messages in conversation");

            var request = new ChatCompletionRequest
            {
                Model = _model,
                Messages = new List<ChatMessage>(_messages),
                Temperature = _temperature,
                MaxTokens = _maxTokens,
                Stream = true
            };

            var fullResponse = new System.Text.StringBuilder();

            await foreach (var chunk in _chatService.CreateCompletionStreamAsync(request, cancellationToken))
            {
                if (chunk.Choices != null && chunk.Choices.Count > 0)
                {
                    var delta = chunk.Choices[0].Delta;
                    if (delta?.Content != null)
                    {
                        fullResponse.Append(delta.Content);
                        yield return delta.Content;
                    }
                }
            }

            // Add the complete response to conversation history
            var completeResponse = fullResponse.ToString();
            if (!string.IsNullOrWhiteSpace(completeResponse))
            {
                _messages.Add(ChatMessage.Assistant(completeResponse));
            }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _messages.Clear();
        }

        /// <inheritdoc/>
        public List<ChatMessage> GetMessages()
        {
            return new List<ChatMessage>(_messages);
        }

        /// <summary>
        /// Gets the number of messages in the conversation
        /// </summary>
        public int MessageCount => _messages.Count;

        /// <summary>
        /// Removes the last message from the conversation
        /// </summary>
        public void RemoveLastMessage()
        {
            if (_messages.Count > 0)
            {
                _messages.RemoveAt(_messages.Count - 1);
            }
        }

        /// <summary>
        /// Removes the last N messages from the conversation
        /// </summary>
        public void RemoveLastMessages(int count)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be positive", nameof(count));

            var toRemove = Math.Min(count, _messages.Count);
            _messages.RemoveRange(_messages.Count - toRemove, toRemove);
        }
    }
}
