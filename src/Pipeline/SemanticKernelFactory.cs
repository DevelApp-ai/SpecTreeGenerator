using System;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.MistralAI;

namespace GeneticSpecOptimizer.Pipeline
{
    /// <summary>
    /// Describes which AI backend the pipeline will use.
    /// </summary>
    public enum SemanticKernelMode
    {
        /// <summary>Mistral AI API (via the Semantic Kernel MistralAI connector).</summary>
        Mistral,

        /// <summary>Azure AI Foundry / Azure OpenAI endpoint.</summary>
        AzureAIFoundry,

        /// <summary>No AI credentials configured; local deterministic extraction is used.</summary>
        LocalFallback
    }

    /// <summary>
    /// Builds a Semantic Kernel <see cref="Kernel"/> from the pipeline configuration.
    ///
    /// Priority: Mistral API key first, then Azure AI Foundry endpoint + key.
    /// When neither is configured, no kernel is created and the pipeline falls
    /// back to the local, non-AI semantic extraction — no credentials are ever
    /// required for the happy path.
    /// </summary>
    public static class SemanticKernelFactory
    {
        /// <summary>
        /// Attempts to create a Kernel from the given configuration.
        /// Returns null when no AI backend is configured (local fallback).
        /// </summary>
        public static Kernel? TryCreate(OptimizationPipelineConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);

            if (!string.IsNullOrWhiteSpace(config.MistralApiKey))
            {
                var builder = Kernel.CreateBuilder();
                builder.AddMistralChatCompletion(
                    modelId: config.MistralModel,
                    apiKey: config.MistralApiKey,
                    endpoint: string.IsNullOrWhiteSpace(config.MistralEndpoint)
                        ? null
                        : new Uri(config.MistralEndpoint));
                return builder.Build();
            }

            if (!string.IsNullOrWhiteSpace(config.AzureAIFoundryEndpoint) &&
                !string.IsNullOrWhiteSpace(config.AzureAIFoundryApiKey))
            {
                var builder = Kernel.CreateBuilder();
                builder.AddAzureOpenAIChatClient(
                    deploymentName: config.AzureAIFoundryDeploymentName,
                    endpoint: config.AzureAIFoundryEndpoint,
                    apiKey: config.AzureAIFoundryApiKey);
                return builder.Build();
            }

            return null;
        }

        /// <summary>
        /// Determines which mode the pipeline will run in for the given configuration.
        /// </summary>
        public static SemanticKernelMode GetMode(OptimizationPipelineConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config.MistralApiKey))
            {
                return SemanticKernelMode.Mistral;
            }

            if (!string.IsNullOrWhiteSpace(config.AzureAIFoundryEndpoint) &&
                !string.IsNullOrWhiteSpace(config.AzureAIFoundryApiKey))
            {
                return SemanticKernelMode.AzureAIFoundry;
            }

            return SemanticKernelMode.LocalFallback;
        }
    }
}