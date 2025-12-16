using OpenAI.LocalAdapter;
using OpenAI.LocalAdapter.Configuration;
using OpenAI.LocalAdapter.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenAI.LocalAdapter.Examples
{
    /// <summary>
    /// Quick start example demonstrating basic usage of OpenAI.LocalAdapter
    /// </summary>
    public class QuickStart
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("OpenAI Local Adapter - Quick Start Example\n");

            // ========================================
            // Example 1: Simple Chat Completion
            // ========================================
            await Example1_SimpleChatCompletion();

            // ========================================
            // Example 2: Conversation Management
            // ========================================
            await Example2_ConversationManagement();

            // ========================================
            // Example 3: Streaming Response
            // ========================================
            await Example3_StreamingResponse();

            // ========================================
            // Example 4: Different Configuration Options
            // ========================================
            await Example4_ConfigurationOptions();

            Console.WriteLine("\nAll examples completed!");
        }

        /// <summary>
        /// Example 1: Simple one-off chat completion
        /// </summary>
        static async Task Example1_SimpleChatCompletion()
        {
            Console.WriteLine("=== Example 1: Simple Chat Completion ===\n");

            // Create client (assumes proxy running on localhost:8080)
            using var client = new OpenAILocalClient("http://localhost:8080");

            // Create a simple request
            var request = new ChatCompletionRequest
            {
                Model = "gpt-3.5-turbo",
                Messages = new List<ChatMessage>
                {
                    ChatMessage.System("You are a helpful assistant."),
                    ChatMessage.User("What is 2 + 2?")
                },
                Temperature = 0.7,
                MaxTokens = 100
            };

            // Get response
            var response = await client.Chat.CreateCompletionAsync(request);
            var answer = response.Choices[0].Message?.Content ?? "";

            Console.WriteLine($"User: What is 2 + 2?");
            Console.WriteLine($"Assistant: {answer}\n");
        }

        /// <summary>
        /// Example 2: Multi-turn conversation with context
        /// </summary>
        static async Task Example2_ConversationManagement()
        {
            Console.WriteLine("=== Example 2: Conversation Management ===\n");

            using var client = new OpenAILocalClient("http://localhost:8080");

            // Create a conversation (maintains context automatically)
            var conversation = client.CreateConversation(
                model: "gpt-3.5-turbo",
                temperature: 0.8,
                maxTokens: 150
            );

            // Set system message
            conversation.AppendSystemMessage("You are a friendly tour guide.");

            // First exchange
            conversation.AppendUserInput("Tell me about Paris.");
            var response1 = await conversation.GetResponseAsync();
            Console.WriteLine($"User: Tell me about Paris.");
            Console.WriteLine($"Guide: {response1}\n");

            // Second exchange (context is maintained)
            conversation.AppendUserInput("What's the best time to visit?");
            var response2 = await conversation.GetResponseAsync();
            Console.WriteLine($"User: What's the best time to visit?");
            Console.WriteLine($"Guide: {response2}\n");

            // Third exchange
            conversation.AppendUserInput("What about food recommendations?");
            var response3 = await conversation.GetResponseAsync();
            Console.WriteLine($"User: What about food recommendations?");
            Console.WriteLine($"Guide: {response3}\n");
        }

        /// <summary>
        /// Example 3: Streaming response for real-time output
        /// </summary>
        static async Task Example3_StreamingResponse()
        {
            Console.WriteLine("=== Example 3: Streaming Response ===\n");

            using var client = new OpenAILocalClient("http://localhost:8080");

            var conversation = client.CreateConversation();
            conversation.AppendSystemMessage("You are a storyteller.");
            conversation.AppendUserInput("Tell me a very short story about a brave knight.");

            Console.WriteLine("User: Tell me a very short story about a brave knight.");
            Console.Write("Storyteller: ");

            // Stream the response token by token
            await foreach (var chunk in conversation.GetResponseStreamAsync())
            {
                Console.Write(chunk);
                await Task.Delay(10); // Small delay to visualize streaming
            }

            Console.WriteLine("\n");
        }

        /// <summary>
        /// Example 4: Different configuration options
        /// </summary>
        static async Task Example4_ConfigurationOptions()
        {
            Console.WriteLine("=== Example 4: Configuration Options ===\n");

            // Option 1: Use proxy (recommended)
            Console.WriteLine("Configuration Option 1: Using Proxy");
            var config1 = LocalAdapterConfig.ForProxy("http://localhost:8080");
            using var client1 = new OpenAILocalClient(config1);
            Console.WriteLine($"  Base URL: {config1.BaseUrl}");
            Console.WriteLine($"  Provider: {config1.Provider}\n");

            // Option 2: Direct LM Studio connection
            Console.WriteLine("Configuration Option 2: Direct LM Studio");
            var config2 = LocalAdapterConfig.ForLMStudio("http://10.50.10.14:1234");
            using var client2 = new OpenAILocalClient(config2);
            Console.WriteLine($"  Base URL: {config2.BaseUrl}");
            Console.WriteLine($"  Provider: {config2.Provider}\n");

            // Option 3: Custom configuration
            Console.WriteLine("Configuration Option 3: Custom Configuration");
            var config3 = new LocalAdapterConfig
            {
                BaseUrl = "http://localhost:8080",
                DefaultModel = "llama-3.1-instruct-13b",
                TimeoutSeconds = 600,
                MaxRetries = 3,
                RetryDelayMs = 2000,
                ValidateSsl = false
            };
            using var client3 = new OpenAILocalClient(config3);
            Console.WriteLine($"  Base URL: {config3.BaseUrl}");
            Console.WriteLine($"  Default Model: {config3.DefaultModel}");
            Console.WriteLine($"  Timeout: {config3.TimeoutSeconds}s");
            Console.WriteLine($"  Max Retries: {config3.MaxRetries}\n");

            // Make a quick test request with client1
            var conversation = client1.CreateConversation();
            conversation.AppendSystemMessage("You are helpful.");
            conversation.AppendUserInput("Hi!");
            var response = await conversation.GetResponseAsync();
            Console.WriteLine($"Test Message: {response}\n");
        }
    }
}
