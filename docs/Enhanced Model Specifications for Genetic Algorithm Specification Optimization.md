# Enhanced Model Specifications for Genetic Algorithm Specification Optimization

**Document Version:** 1.0  
**Status:** Final  
**Date:** May 26, 2025

## 1. Introduction

This document provides detailed specifications for all data models used in the Genetic Algorithm Specification Optimization system. It addresses implementation challenges identified during development and ensures proper alignment between models and their usage throughout the codebase.

The system uses a hierarchical approach to optimize specification files by pushing knowledge up the hierarchy, leveraging genetic algorithms to find the most efficient parent-child relationships and minimize specification size.

## 2. Core Model Specifications

### 2.1 CodeComponent

The `CodeComponent` class represents a discrete unit of code (class, method, property, etc.) extracted from source code.

```csharp
namespace SpecTreeGenerator.Common.Models
{
    /// <summary>
    /// Type of code component
    /// </summary>
    public enum ComponentType
    {
        Namespace,
        Class,
        Interface,
        Struct,
        Enum,
        Method,
        Property,
        Field,
        Event,
        Delegate,
        Unknown
    }

    /// <summary>
    /// Represents a code component
    /// </summary>
    public class CodeComponent
    {
        /// <summary>
        /// Unique identifier for this component
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the component
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Type of the component
        /// </summary>
        public ComponentType Type { get; set; }
        
        /// <summary>
        /// Full path of the component (e.g., namespace:class:method)
        /// </summary>
        public string FullPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Namespace containing this component
        /// </summary>
        public string Namespace { get; set; } = string.Empty;
        
        /// <summary>
        /// Parent component ID (if any)
        /// </summary>
        public string ParentId { get; set; } = string.Empty;
        
        /// <summary>
        /// Source file path
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;
        
        /// <summary>
        /// Absolute path to the source file
        /// </summary>
        public string SourceFilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Source code of the component
        /// </summary>
        public string SourceCode { get; set; } = string.Empty;
        
        /// <summary>
        /// Documentation comments
        /// </summary>
        public string Documentation { get; set; } = string.Empty;
        
        /// <summary>
        /// Method or type signature (if applicable)
        /// </summary>
        public string Signature { get; set; } = string.Empty;
        
        /// <summary>
        /// Starting line number in source file
        /// </summary>
        public int StartLine { get; set; }
        
        /// <summary>
        /// Ending line number in source file
        /// </summary>
        public int EndLine { get; set; }
        
        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a deep clone of this component
        /// </summary>
        public CodeComponent Clone()
        {
            return new CodeComponent
            {
                Id = this.Id,
                Name = this.Name,
                Type = this.Type,
                FullPath = this.FullPath,
                Namespace = this.Namespace,
                ParentId = this.ParentId,
                SourceFile = this.SourceFile,
                SourceFilePath = this.SourceFilePath,
                SourceCode = this.SourceCode,
                Documentation = this.Documentation,
                Signature = this.Signature,
                StartLine = this.StartLine,
                EndLine = this.EndLine,
                Metadata = new Dictionary<string, object>(this.Metadata)
            };
        }
        
        /// <summary>
        /// Returns a string representation of the component
        /// </summary>
        public override string ToString()
        {
            return $"{Type}: {Name} ({FullPath})";
        }
    }
}
```

### 2.2 CodeRelationship

The `CodeRelationship` class represents a relationship between two code components, such as inheritance, implementation, or dependency.

```csharp
namespace SpecTreeGenerator.Common.Models
{
    /// <summary>
    /// Type of relationship between components
    /// </summary>
    public enum RelationshipType
    {
        Inheritance,
        Implementation,
        Dependency,
        Composition,
        Aggregation,
        Usage,
        Unknown
    }

    /// <summary>
    /// Represents a relationship between code components
    /// </summary>
    public class CodeRelationship
    {
        /// <summary>
        /// Unique identifier for this relationship
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Type of relationship
        /// </summary>
        public RelationshipType Type { get; set; }
        
        /// <summary>
        /// Source component ID
        /// </summary>
        public string SourceComponentId { get; set; } = string.Empty;
        
        /// <summary>
        /// Target component ID
        /// </summary>
        public string TargetComponentId { get; set; } = string.Empty;
        
        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a deep clone of this relationship
        /// </summary>
        public CodeRelationship Clone()
        {
            return new CodeRelationship
            {
                Id = this.Id,
                Type = this.Type,
                SourceComponentId = this.SourceComponentId,
                TargetComponentId = this.TargetComponentId,
                Metadata = new Dictionary<string, object>(this.Metadata)
            };
        }
        
        /// <summary>
        /// Returns a string representation of the relationship
        /// </summary>
        public override string ToString()
        {
            return $"{Type}: {SourceComponentId} -> {TargetComponentId}";
        }
    }
}
```

### 2.3 HierarchyNode and CodeHierarchy

The `HierarchyNode` and `CodeHierarchy` classes represent a hierarchical structure of code components.

```csharp
namespace SpecTreeGenerator.Common.Models
{
    /// <summary>
    /// Represents a node in a code hierarchy
    /// </summary>
    public class HierarchyNode
    {
        /// <summary>
        /// Unique identifier for this node
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the node
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Type of the node (namespace, class, method, etc.)
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Full path of the node (e.g., namespace:class:method)
        /// </summary>
        public string FullPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Component ID this node represents
        /// </summary>
        public string ComponentId { get; set; } = string.Empty;
        
        /// <summary>
        /// Parent node (if any)
        /// </summary>
        public HierarchyNode? Parent { get; set; }
        
        /// <summary>
        /// Child nodes
        /// </summary>
        public List<HierarchyNode> Children { get; set; } = new List<HierarchyNode>();
        
        /// <summary>
        /// Additional metadata for the node
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a deep clone of this node (without parent or children)
        /// </summary>
        public HierarchyNode Clone()
        {
            var clone = new HierarchyNode
            {
                Id = this.Id,
                Name = this.Name,
                Type = this.Type,
                FullPath = this.FullPath,
                ComponentId = this.ComponentId,
                Metadata = new Dictionary<string, object>(this.Metadata)
            };
            
            return clone;
        }
        
        /// <summary>
        /// Creates a deep clone of this node and its descendants
        /// </summary>
        public HierarchyNode CloneWithDescendants()
        {
            var clone = this.Clone();
            
            foreach (var child in this.Children)
            {
                var childClone = child.CloneWithDescendants();
                childClone.Parent = clone;
                clone.Children.Add(childClone);
            }
            
            return clone;
        }
        
        /// <summary>
        /// Returns a string representation of the node
        /// </summary>
        public override string ToString()
        {
            return $"{Type}: {Name} ({FullPath})";
        }
    }
    
    /// <summary>
    /// Represents a code hierarchy
    /// </summary>
    public class CodeHierarchy
    {
        /// <summary>
        /// Root nodes of the hierarchy
        /// </summary>
        public List<HierarchyNode> RootNodes { get; set; } = new List<HierarchyNode>();
        
        /// <summary>
        /// Lookup dictionary for quick node access by ID
        /// </summary>
        public Dictionary<string, HierarchyNode> NodeLookup { get; set; } = new Dictionary<string, HierarchyNode>();
        
        /// <summary>
        /// Creates a deep clone of this hierarchy
        /// </summary>
        public CodeHierarchy Clone()
        {
            var clone = new CodeHierarchy();
            
            foreach (var rootNode in this.RootNodes)
            {
                clone.RootNodes.Add(rootNode.CloneWithDescendants());
            }
            
            // Rebuild node lookup
            clone.RebuildNodeLookup();
            
            return clone;
        }
        
        /// <summary>
        /// Finds a node by its full path
        /// </summary>
        public HierarchyNode? FindNodeByPath(string fullPath)
        {
            foreach (var rootNode in RootNodes)
            {
                var node = FindNodeByPathRecursive(rootNode, fullPath);
                if (node != null)
                {
                    return node;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Adds a component to the hierarchy
        /// </summary>
        public void AddComponent(CodeComponent component)
        {
            // Create a node for the component
            var node = new HierarchyNode
            {
                Id = component.Id,
                Name = component.Name,
                Type = component.Type.ToString(),
                FullPath = component.FullPath,
                ComponentId = component.Id
            };
            
            // Add metadata
            node.Metadata["Component"] = component;
            
            // Add to lookup
            NodeLookup[node.Id] = node;
            
            // If this is a root component, add to root nodes
            if (string.IsNullOrEmpty(component.ParentId))
            {
                RootNodes.Add(node);
            }
            else
            {
                // Find parent node
                if (NodeLookup.TryGetValue(component.ParentId, out var parentNode))
                {
                    // Add as child
                    parentNode.Children.Add(node);
                    node.Parent = parentNode;
                }
                else
                {
                    // Parent not found, add as root
                    RootNodes.Add(node);
                }
            }
        }
        
        /// <summary>
        /// Reorganizes the hierarchy based on component relationships
        /// </summary>
        public void ReorganizeHierarchy()
        {
            // Clear existing parent-child relationships
            foreach (var node in NodeLookup.Values)
            {
                node.Parent = null;
                node.Children.Clear();
            }
            
            // Rebuild root nodes
            RootNodes.Clear();
            
            // Rebuild parent-child relationships
            foreach (var node in NodeLookup.Values)
            {
                if (node.Metadata.TryGetValue("Component", out var componentObj) && 
                    componentObj is CodeComponent component)
                {
                    if (string.IsNullOrEmpty(component.ParentId))
                    {
                        // This is a root node
                        RootNodes.Add(node);
                    }
                    else if (NodeLookup.TryGetValue(component.ParentId, out var parentNode))
                    {
                        // Add as child to parent
                        parentNode.Children.Add(node);
                        node.Parent = parentNode;
                    }
                    else
                    {
                        // Parent not found, add as root
                        RootNodes.Add(node);
                    }
                }
            }
        }
        
        /// <summary>
        /// Rebuilds the node lookup dictionary
        /// </summary>
        public void RebuildNodeLookup()
        {
            NodeLookup.Clear();
            
            foreach (var rootNode in RootNodes)
            {
                AddToLookupRecursive(rootNode);
            }
        }
        
        private void AddToLookupRecursive(HierarchyNode node)
        {
            NodeLookup[node.Id] = node;
            
            foreach (var child in node.Children)
            {
                AddToLookupRecursive(child);
            }
        }
        
        private HierarchyNode? FindNodeByPathRecursive(HierarchyNode node, string fullPath)
        {
            if (node.FullPath == fullPath)
            {
                return node;
            }
            
            foreach (var child in node.Children)
            {
                var result = FindNodeByPathRecursive(child, fullPath);
                if (result != null)
                {
                    return result;
                }
            }
            
            return null;
        }
    }
}
```

### 2.4 SpecificationFile

The `SpecificationFile` class represents a specification file that will be generated or optimized.

```csharp
namespace SpecTreeGenerator.Common.Models
{
    /// <summary>
    /// Represents a specification file
    /// </summary>
    public class SpecificationFile
    {
        /// <summary>
        /// Unique identifier for this specification file
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the specification file
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Component ID this specification is for
        /// </summary>
        public string ComponentId { get; set; } = string.Empty;
        
        /// <summary>
        /// Content of the specification file
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Parent specification ID (if any)
        /// </summary>
        public string ParentSpecificationId { get; set; } = string.Empty;
        
        /// <summary>
        /// File path where this specification should be saved
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a deep clone of this specification file
        /// </summary>
        public SpecificationFile Clone()
        {
            return new SpecificationFile
            {
                Id = this.Id,
                Name = this.Name,
                ComponentId = this.ComponentId,
                Content = this.Content,
                ParentSpecificationId = this.ParentSpecificationId,
                FilePath = this.FilePath,
                Metadata = new Dictionary<string, object>(this.Metadata)
            };
        }
        
        /// <summary>
        /// Returns a string representation of the specification file
        /// </summary>
        public override string ToString()
        {
            return $"Specification: {Name} (Component: {ComponentId})";
        }
    }
}
```

### 2.5 TextSpan

The `TextSpan` class represents a span of text in a source file.

```csharp
namespace SpecTreeGenerator.Common.Models
{
    /// <summary>
    /// Represents a text span in a source file
    /// </summary>
    public class TextSpan
    {
        /// <summary>
        /// Start position of the span
        /// </summary>
        public int Start { get; set; }
        
        /// <summary>
        /// End position of the span
        /// </summary>
        public int End { get; set; }
        
        /// <summary>
        /// Length of the span
        /// </summary>
        public int Length => End - Start;
        
        /// <summary>
        /// Creates a new text span
        /// </summary>
        public TextSpan(int start, int end)
        {
            Start = start;
            End = end;
        }
        
        /// <summary>
        /// Returns a string representation of the span
        /// </summary>
        public override string ToString()
        {
            return $"[{Start}..{End})";
        }
        
        /// <summary>
        /// Converts to Microsoft.CodeAnalysis.Text.TextSpan
        /// </summary>
        public static implicit operator Microsoft.CodeAnalysis.Text.TextSpan(TextSpan span)
        {
            return new Microsoft.CodeAnalysis.Text.TextSpan(span.Start, span.Length);
        }
        
        /// <summary>
        /// Converts from Microsoft.CodeAnalysis.Text.TextSpan
        /// </summary>
        public static implicit operator TextSpan(Microsoft.CodeAnalysis.Text.TextSpan span)
        {
            return new TextSpan(span.Start, span.End);
        }
    }
}
```

## 3. Genetic Algorithm Models

### 3.1 SpecificationGenome

The `SpecificationGenome` class represents a genome in the genetic algorithm, encoding a specific hierarchy of specifications.

```csharp
namespace SpecTreeGenerator.GeneticAlgorithm.Models
{
    using SpecTreeGenerator.Common.Models;
    
    /// <summary>
    /// Represents a genome in the genetic algorithm
    /// </summary>
    public class SpecificationGenome
    {
        /// <summary>
        /// Unique identifier for this genome
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Specification files in this genome
        /// </summary>
        public List<SpecificationFile> SpecificationFiles { get; set; } = new List<SpecificationFile>();
        
        /// <summary>
        /// Parent-child relationships between specifications
        /// </summary>
        public Dictionary<string, string> ParentChildRelationships { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Fitness score of this genome
        /// </summary>
        public double FitnessScore { get; set; }
        
        /// <summary>
        /// Generation this genome belongs to
        /// </summary>
        public int Generation { get; set; }
        
        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a deep clone of this genome
        /// </summary>
        public SpecificationGenome Clone()
        {
            var clone = new SpecificationGenome
            {
                Id = Guid.NewGuid().ToString(),
                FitnessScore = this.FitnessScore,
                Generation = this.Generation,
                Metadata = new Dictionary<string, object>(this.Metadata),
                ParentChildRelationships = new Dictionary<string, string>(this.ParentChildRelationships)
            };
            
            foreach (var spec in this.SpecificationFiles)
            {
                clone.SpecificationFiles.Add(spec.Clone());
            }
            
            return clone;
        }
        
        /// <summary>
        /// Returns a string representation of the genome
        /// </summary>
        public override string ToString()
        {
            return $"Genome: {Id} (Fitness: {FitnessScore}, Generation: {Generation}, Specs: {SpecificationFiles.Count})";
        }
    }
}
```

### 3.2 EvolutionResult

The `EvolutionResult` class represents the result of running the genetic algorithm.

```csharp
namespace SpecTreeGenerator.GeneticAlgorithm.Models
{
    /// <summary>
    /// Represents the result of running the genetic algorithm
    /// </summary>
    public class EvolutionResult
    {
        /// <summary>
        /// Best genome found
        /// </summary>
        public SpecificationGenome BestGenome { get; set; } = null!;
        
        /// <summary>
        /// All genomes in the final population
        /// </summary>
        public List<SpecificationGenome> FinalPopulation { get; set; } = new List<SpecificationGenome>();
        
        /// <summary>
        /// Fitness history over generations
        /// </summary>
        public List<double> FitnessHistory { get; set; } = new List<double>();
        
        /// <summary>
        /// Number of generations run
        /// </summary>
        public int Generations { get; set; }
        
        /// <summary>
        /// Time taken for evolution
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }
        
        /// <summary>
        /// Whether evolution converged
        /// </summary>
        public bool Converged { get; set; }
        
        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Returns a string representation of the evolution result
        /// </summary>
        public override string ToString()
        {
            return $"Evolution Result: {Generations} generations, Best Fitness: {BestGenome?.FitnessScore}, Time: {ElapsedTime}";
        }
    }
}
```

### 3.3 FitnessMetrics

The `FitnessMetrics` class represents metrics used to evaluate the fitness of a genome.

```csharp
namespace SpecTreeGenerator.GeneticAlgorithm.Models
{
    /// <summary>
    /// Represents metrics used to evaluate the fitness of a genome
    /// </summary>
    public class FitnessMetrics
    {
        /// <summary>
        /// Total size of all specifications in characters
        /// </summary>
        public int TotalSize { get; set; }
        
        /// <summary>
        /// Amount of redundant content across specifications
        /// </summary>
        public int RedundantContent { get; set; }
        
        /// <summary>
        /// Number of parent-child relationships
        /// </summary>
        public int InheritanceCount { get; set; }
        
        /// <summary>
        /// Depth of the inheritance hierarchy
        /// </summary>
        public int HierarchyDepth { get; set; }
        
        /// <summary>
        /// Alignment with code structure (0-1)
        /// </summary>
        public double CodeAlignmentScore { get; set; }
        
        /// <summary>
        /// Weighted fitness score
        /// </summary>
        public double WeightedScore { get; set; }
        
        /// <summary>
        /// Returns a string representation of the fitness metrics
        /// </summary>
        public override string ToString()
        {
            return $"Fitness: Size={TotalSize}, Redundancy={RedundantContent}, Inheritance={InheritanceCount}, Depth={HierarchyDepth}, Alignment={CodeAlignmentScore:F2}, Score={WeightedScore:F2}";
        }
    }
}
```

## 4. Configuration Models

### 4.1 EvolutionConfig

The `EvolutionConfig` class represents configuration settings for the genetic algorithm.

```csharp
namespace SpecTreeGenerator.GeneticAlgorithm.Evolution
{
    /// <summary>
    /// Configuration settings for the genetic algorithm
    /// </summary>
    public class EvolutionConfig
    {
        /// <summary>
        /// Size of the population
        /// </summary>
        public int PopulationSize { get; set; } = 100;
        
        /// <summary>
        /// Maximum number of generations to run
        /// </summary>
        public int MaxGenerations { get; set; } = 50;
        
        /// <summary>
        /// Probability of mutation
        /// </summary>
        public double MutationRate { get; set; } = 0.1;
        
        /// <summary>
        /// Probability of crossover
        /// </summary>
        public double CrossoverRate { get; set; } = 0.8;
        
        /// <summary>
        /// Number of elite individuals to preserve
        /// </summary>
        public int ElitismCount { get; set; } = 5;
        
        /// <summary>
        /// Tournament size for selection
        /// </summary>
        public int TournamentSize { get; set; } = 3;
        
        /// <summary>
        /// Convergence threshold
        /// </summary>
        public double ConvergenceThreshold { get; set; } = 0.001;
        
        /// <summary>
        /// Number of generations with no improvement to consider converged
        /// </summary>
        public int ConvergenceGenerations { get; set; } = 10;
        
        /// <summary>
        /// Random seed for reproducibility
        /// </summary>
        public int? RandomSeed { get; set; }
        
        /// <summary>
        /// Additional settings
        /// </summary>
        public Dictionary<string, object> AdditionalSettings { get; set; } = new Dictionary<string, object>();
    }
}
```

### 4.2 HierarchyOptimizerConfig

The `HierarchyOptimizerConfig` class represents configuration settings for the hierarchy optimizer.

```csharp
namespace SpecTreeGenerator.HierarchyOptimization
{
    using SpecTreeGenerator.GeneticAlgorithm.Evolution;
    
    /// <summary>
    /// Configuration settings for the hierarchy optimizer
    /// </summary>
    public class HierarchyOptimizerConfig
    {
        /// <summary>
        /// Configuration for the evolution engine
        /// </summary>
        public EvolutionConfig EvolutionConfig { get; set; } = new EvolutionConfig();
        
        /// <summary>
        /// Maximum time to spend optimizing (in minutes)
        /// </summary>
        public int MaxOptimizationTimeMinutes { get; set; } = 5;
        
        /// <summary>
        /// Weight for size reduction in fitness calculation
        /// </summary>
        public double SizeReductionWeight { get; set; } = 0.4;
        
        /// <summary>
        /// Weight for redundancy reduction in fitness calculation
        /// </summary>
        public double RedundancyReductionWeight { get; set; } = 0.3;
        
        /// <summary>
        /// Weight for inheritance in fitness calculation
        /// </summary>
        public double InheritanceWeight { get; set; } = 0.2;
        
        /// <summary>
        /// Weight for code alignment in fitness calculation
        /// </summary>
        public double CodeAlignmentWeight { get; set; } = 0.1;
        
        /// <summary>
        /// Maximum hierarchy depth to allow
        /// </summary>
        public int MaxHierarchyDepth { get; set; } = 5;
        
        /// <summary>
        /// Additional settings
        /// </summary>
        public Dictionary<string, object> AdditionalSettings { get; set; } = new Dictionary<string, object>();
    }
}
```

### 4.3 OptimizationPipelineConfig

The `OptimizationPipelineConfig` class represents configuration settings for the optimization pipeline.

```csharp
namespace SpecTreeGenerator.Pipeline
{
    using SpecTreeGenerator.HierarchyOptimization;
    
    /// <summary>
    /// Configuration settings for the optimization pipeline
    /// </summary>
    public class OptimizationPipelineConfig
    {
        /// <summary>
        /// Directory containing source code to analyze
        /// </summary>
        public string SourceCodeDirectory { get; set; } = string.Empty;
        
        /// <summary>
        /// List of documentation files to analyze
        /// </summary>
        public List<string> DocumentationFiles { get; set; } = new List<string>();
        
        /// <summary>
        /// Directory to output generated specifications
        /// </summary>
        public string OutputDirectory { get; set; } = string.Empty;
        
        /// <summary>
        /// Configuration for the hierarchy optimizer
        /// </summary>
        public HierarchyOptimizerConfig OptimizerConfig { get; set; } = new HierarchyOptimizerConfig();
        
        /// <summary>
        /// Whether to include private members in specifications
        /// </summary>
        public bool IncludePrivateMembers { get; set; } = false;
        
        /// <summary>
        /// Whether to include implementation details in specifications
        /// </summary>
        public bool IncludeImplementationDetails { get; set; } = false;
        
        /// <summary>
        /// File extensions to include in analysis
        /// </summary>
        public List<string> IncludeExtensions { get; set; } = new List<string> { ".cs" };
        
        /// <summary>
        /// Directories to exclude from analysis
        /// </summary>
        public List<string> ExcludeDirectories { get; set; } = new List<string> { "bin", "obj" };
        
        /// <summary>
        /// Additional settings
        /// </summary>
        public Dictionary<string, object> AdditionalSettings { get; set; } = new Dictionary<string, object>();
    }
}
```

### 4.4 OptimizationResult

The `OptimizationResult` class represents the result of running the optimization pipeline.

```csharp
namespace SpecTreeGenerator.Pipeline
{
    using SpecTreeGenerator.GeneticAlgorithm.Models;
    
    /// <summary>
    /// Represents the result of running the optimization pipeline
    /// </summary>
    public class OptimizationResult
    {
        /// <summary>
        /// Whether the optimization was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Error message if not successful
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Paths to output files
        /// </summary>
        public List<string> OutputFilePaths { get; set; } = new List<string>();
        
        /// <summary>
        /// Result of the evolution process
        /// </summary>
        public EvolutionResult? EvolutionResult { get; set; }
        
        /// <summary>
        /// Optimization metrics
        /// </summary>
        public OptimizationMetrics Metrics { get; set; } = new OptimizationMetrics();
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Metrics for the optimization process
    /// </summary>
    public class OptimizationMetrics
    {
        /// <summary>
        /// Original size of specifications in characters
        /// </summary>
        public int OriginalSize { get; set; }
        
        /// <summary>
        /// Optimized size of specifications in characters
        /// </summary>
        public int OptimizedSize { get; set; }
        
        /// <summary>
        /// Percentage reduction in size
        /// </summary>
        public double SizeReductionPercentage => OriginalSize > 0 ? 100.0 * (1.0 - (double)OptimizedSize / OriginalSize) : 0;
        
        /// <summary>
        /// Total number of sections in specifications
        /// </summary>
        public int TotalSections { get; set; }
        
        /// <summary>
        /// Number of sections that are inherited
        /// </summary>
        public int InheritedSections { get; set; }
        
        /// <summary>
        /// Percentage of sections that are inherited
        /// </summary>
        public double InheritancePercentage => TotalSections > 0 ? 100.0 * (double)InheritedSections / TotalSections : 0;
        
        /// <summary>
        /// Depth of the hierarchy
        /// </summary>
        public int HierarchyDepth { get; set; }
        
        /// <summary>
        /// Time taken for optimization
        /// </summary>
        public TimeSpan OptimizationTime { get; set; }
    }
}
```

## 5. Component Interfaces

### 5.1 Code Analysis Interfaces

```csharp
namespace SpecTreeGenerator.CodeAnalysis.Interfaces
{
    using SpecTreeGenerator.Common.Models;
    using Microsoft.CodeAnalysis;
    
    /// <summary>
    /// Interface for language parsers
    /// </summary>
    public interface ILanguageParser
    {
        /// <summary>
        /// Parses source code files in a directory
        /// </summary>
        Task<IEnumerable<SyntaxTree>> ParseDirectoryAsync(string directory, IEnumerable<string> includeExtensions, IEnumerable<string> excludeDirectories);
        
        /// <summary>
        /// Parses a single source code file
        /// </summary>
        Task<SyntaxTree> ParseFileAsync(string filePath);
    }
    
    /// <summary>
    /// Interface for symbol table builders
    /// </summary>
    public interface ISymbolTableBuilder
    {
        /// <summary>
        /// Builds a symbol table from syntax trees
        /// </summary>
        Task<Compilation> BuildSymbolTableAsync(IEnumerable<SyntaxTree> syntaxTrees);
    }
    
    /// <summary>
    /// Interface for component extractors
    /// </summary>
    public interface IComponentExtractor
    {
        /// <summary>
        /// Extracts components from a compilation
        /// </summary>
        Task<IEnumerable<CodeComponent>> ExtractComponentsAsync(Compilation compilation, bool includePrivateMembers);
    }
    
    /// <summary>
    /// Interface for relationship analyzers
    /// </summary>
    public interface IRelationshipAnalyzer
    {
        /// <summary>
        /// Analyzes relationships between components
        /// </summary>
        Task<IEnumerable<CodeRelationship>> AnalyzeRelationshipsAsync(IEnumerable<CodeComponent> components, Compilation compilation);
    }
    
    /// <summary>
    /// Interface for hierarchy builders
    /// </summary>
    public interface IHierarchyBuilder
    {
        /// <summary>
        /// Builds a code hierarchy from components and relationships
        /// </summary>
        Task<CodeHierarchy> BuildHierarchyAsync(IEnumerable<CodeComponent> components, IEnumerable<CodeRelationship> relationships);
    }
    
    /// <summary>
    /// Interface for specification generators
    /// </summary>
    public interface ISpecificationGenerator
    {
        /// <summary>
        /// Generates specifications from a code hierarchy
        /// </summary>
        Task<IEnumerable<SpecificationFile>> GenerateSpecificationsAsync(CodeHierarchy hierarchy, bool includeImplementationDetails);
    }
}
```

### 5.2 Documentation Extraction Interfaces

```csharp
namespace SpecTreeGenerator.DocumentationExtraction.Interfaces
{
    using SpecTreeGenerator.Common.Models;
    
    /// <summary>
    /// Interface for documentation converters
    /// </summary>
    public interface IDocumentationConverter
    {
        /// <summary>
        /// Converts documentation files to markdown
        /// </summary>
        Task<IEnumerable<string>> ConvertToMarkdownAsync(IEnumerable<string> documentationFiles);
    }
    
    /// <summary>
    /// Interface for markdown parsers
    /// </summary>
    public interface IMarkdownParser
    {
        /// <summary>
        /// Parses markdown content
        /// </summary>
        Task<IEnumerable<DocumentationSection>> ParseMarkdownAsync(IEnumerable<string> markdownFiles);
    }
    
    /// <summary>
    /// Interface for structure analyzers
    /// </summary>
    public interface IStructureAnalyzer
    {
        /// <summary>
        /// Analyzes the structure of documentation sections
        /// </summary>
        Task<DocumentationStructure> AnalyzeStructureAsync(IEnumerable<DocumentationSection> sections);
    }
    
    /// <summary>
    /// Interface for semantic extractors
    /// </summary>
    public interface ISemanticExtractor
    {
        /// <summary>
        /// Extracts semantic information from documentation
        /// </summary>
        Task<IEnumerable<SemanticComponent>> ExtractSemanticComponentsAsync(DocumentationStructure structure, CodeHierarchy codeHierarchy);
    }
    
    /// <summary>
    /// Represents a section of documentation
    /// </summary>
    public class DocumentationSection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Level { get; set; }
        public string SourceFile { get; set; } = string.Empty;
        public List<DocumentationSection> Subsections { get; set; } = new List<DocumentationSection>();
    }
    
    /// <summary>
    /// Represents the structure of documentation
    /// </summary>
    public class DocumentationStructure
    {
        public List<DocumentationSection> RootSections { get; set; } = new List<DocumentationSection>();
        public Dictionary<string, DocumentationSection> SectionLookup { get; set; } = new Dictionary<string, DocumentationSection>();
    }
    
    /// <summary>
    /// Represents a semantic component extracted from documentation
    /// </summary>
    public class SemanticComponent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string SourceSectionId { get; set; } = string.Empty;
        public string? RelatedCodeComponentId { get; set; }
    }
}
```

### 5.3 Genetic Algorithm Interfaces

```csharp
namespace SpecTreeGenerator.GeneticAlgorithm.Interfaces
{
    using SpecTreeGenerator.Common.Models;
    using SpecTreeGenerator.GeneticAlgorithm.Models;
    using SpecTreeGenerator.GeneticAlgorithm.Evolution;
    
    /// <summary>
    /// Interface for specification genome encoders
    /// </summary>
    public interface ISpecificationGenomeEncoder
    {
        /// <summary>
        /// Encodes specifications into a genome
        /// </summary>
        Task<SpecificationGenome> EncodeAsync(IEnumerable<SpecificationFile> specifications);
        
        /// <summary>
        /// Decodes a genome into specifications
        /// </summary>
        Task<IEnumerable<SpecificationFile>> DecodeAsync(SpecificationGenome genome);
    }
    
    /// <summary>
    /// Interface for genetic operators
    /// </summary>
    public interface IGeneticOperators
    {
        /// <summary>
        /// Performs mutation on a genome
        /// </summary>
        Task<SpecificationGenome> MutateAsync(SpecificationGenome genome, double mutationRate);
        
        /// <summary>
        /// Performs crossover between two genomes
        /// </summary>
        Task<(SpecificationGenome, SpecificationGenome)> CrossoverAsync(SpecificationGenome parent1, SpecificationGenome parent2, double crossoverRate);
        
        /// <summary>
        /// Selects parents from a population
        /// </summary>
        Task<List<SpecificationGenome>> SelectAsync(List<SpecificationGenome> population, int count, int tournamentSize);
    }
    
    /// <summary>
    /// Interface for fitness evaluators
    /// </summary>
    public interface IFitnessEvaluator
    {
        /// <summary>
        /// Evaluates the fitness of a genome
        /// </summary>
        Task<double> EvaluateAsync(SpecificationGenome genome, CodeHierarchy codeHierarchy, HierarchyOptimizerConfig config);
        
        /// <summary>
        /// Calculates detailed fitness metrics for a genome
        /// </summary>
        Task<FitnessMetrics> CalculateMetricsAsync(SpecificationGenome genome, CodeHierarchy codeHierarchy);
    }
    
    /// <summary>
    /// Interface for evolution engines
    /// </summary>
    public interface IEvolutionEngine
    {
        /// <summary>
        /// Runs the genetic algorithm
        /// </summary>
        Task<EvolutionResult> EvolveAsync(SpecificationGenome initialGenome, CodeHierarchy codeHierarchy, EvolutionConfig config, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Event for progress updates
        /// </summary>
        event EventHandler<EvolutionProgressEventArgs> ProgressUpdated;
    }
    
    /// <summary>
    /// Event arguments for evolution progress
    /// </summary>
    public class EvolutionProgressEventArgs : EventArgs
    {
        public int Generation { get; set; }
        public double BestFitness { get; set; }
        public double AverageFitness { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }
}
```

### 5.4 Hierarchy Optimization Interfaces

```csharp
namespace SpecTreeGenerator.HierarchyOptimization.Interfaces
{
    using SpecTreeGenerator.Common.Models;
    using SpecTreeGenerator.GeneticAlgorithm.Models;
    
    /// <summary>
    /// Interface for hierarchy optimizers
    /// </summary>
    public interface IHierarchyOptimizer
    {
        /// <summary>
        /// Optimizes a hierarchy of specifications
        /// </summary>
        Task<OptimizationResult> OptimizeAsync(CodeHierarchy codeHierarchy, IEnumerable<SpecificationFile> initialSpecifications, HierarchyOptimizerConfig config, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Event for progress updates
        /// </summary>
        event EventHandler<OptimizationProgressEventArgs> ProgressUpdated;
    }
    
    /// <summary>
    /// Event arguments for optimization progress
    /// </summary>
    public class OptimizationProgressEventArgs : EventArgs
    {
        public string Stage { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public double ProgressPercentage { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }
    
    /// <summary>
    /// Result of the optimization process
    /// </summary>
    public class OptimizationResult
    {
        /// <summary>
        /// Whether the optimization was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Optimized specifications
        /// </summary>
        public IEnumerable<SpecificationFile> OptimizedSpecifications { get; set; } = null!;
        
        /// <summary>
        /// Result of the evolution process
        /// </summary>
        public EvolutionResult EvolutionResult { get; set; } = null!;
        
        /// <summary>
        /// Optimization metrics
        /// </summary>
        public OptimizationMetrics Metrics { get; set; } = null!;
    }
    
    /// <summary>
    /// Metrics for the optimization process
    /// </summary>
    public class OptimizationMetrics
    {
        /// <summary>
        /// Original size of specifications in characters
        /// </summary>
        public int OriginalSize { get; set; }
        
        /// <summary>
        /// Optimized size of specifications in characters
        /// </summary>
        public int OptimizedSize { get; set; }
        
        /// <summary>
        /// Percentage reduction in size
        /// </summary>
        public double SizeReductionPercentage => OriginalSize > 0 ? 100.0 * (1.0 - (double)OptimizedSize / OriginalSize) : 0;
        
        /// <summary>
        /// Total number of sections in specifications
        /// </summary>
        public int TotalSections { get; set; }
        
        /// <summary>
        /// Number of sections that are inherited
        /// </summary>
        public int InheritedSections { get; set; }
        
        /// <summary>
        /// Percentage of sections that are inherited
        /// </summary>
        public double InheritancePercentage => TotalSections > 0 ? 100.0 * (double)InheritedSections / TotalSections : 0;
        
        /// <summary>
        /// Depth of the hierarchy
        /// </summary>
        public int HierarchyDepth { get; set; }
        
        /// <summary>
        /// Time taken for optimization
        /// </summary>
        public TimeSpan OptimizationTime { get; set; }
    }
}
```

### 5.5 Pipeline Interfaces

```csharp
namespace SpecTreeGenerator.Pipeline.Interfaces
{
    using SpecTreeGenerator.Common.Models;
    
    /// <summary>
    /// Interface for the optimization pipeline
    /// </summary>
    public interface IOptimizationPipeline
    {
        /// <summary>
        /// Runs the optimization pipeline
        /// </summary>
        Task<OptimizationResult> RunAsync(OptimizationPipelineConfig config, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Event for progress updates
        /// </summary>
        event EventHandler<PipelineProgressEventArgs> ProgressUpdated;
    }
    
    /// <summary>
    /// Event arguments for pipeline progress
    /// </summary>
    public class PipelineProgressEventArgs : EventArgs
    {
        public string Stage { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public double ProgressPercentage { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }
}
```

## 6. Component Relationships

The following diagram illustrates the relationships between the major components of the system:

```
┌─────────────────────────┐     ┌─────────────────────────┐
│                         │     │                         │
│    Code Analysis        │     │  Documentation           │
│    Engine               │     │  Extraction Engine       │
│                         │     │                         │
└───────────┬─────────────┘     └───────────┬─────────────┘
            │                               │
            │                               │
            ▼                               ▼
┌─────────────────────────────────────────────────────────┐
│                                                         │
│                 Source Consolidator                     │
│                                                         │
└───────────────────────────┬─────────────────────────────┘
                            │
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                                                         │
│              Specification Genome Encoder               │
│                                                         │
└───────────────────────────┬─────────────────────────────┘
                            │
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                                                         │
│                  Genetic Algorithm                      │
│                                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │ Genetic     │  │ Fitness     │  │ Evolution   │     │
│  │ Operators   │  │ Evaluator   │  │ Engine      │     │
│  └─────────────┘  └─────────────┘  └─────────────┘     │
│                                                         │
└───────────────────────────┬─────────────────────────────┘
                            │
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                                                         │
│                 Hierarchy Optimizer                     │
│                                                         │
└───────────────────────────┬─────────────────────────────┘
                            │
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                                                         │
│                Optimization Pipeline                    │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

## 7. Implementation Considerations

### 7.1 Semantic Kernel Integration

The system integrates with Semantic Kernel and Mistral LLM through Azure AI Foundry for semantic analysis of documentation and code. The following considerations should be taken into account:

1. **Authentication**: Use Azure AI Foundry endpoint for Mistral LLM integration
2. **Prompt Engineering**: Carefully design prompts for extracting semantic information
3. **Context Management**: Manage context size to avoid token limits
4. **Error Handling**: Implement robust error handling for API calls
5. **Caching**: Cache results to minimize API calls

### 7.2 Markitdown Integration

Microsoft's Markitdown is used for documentation conversion. Key considerations:

1. **Format Support**: Ensure support for various input formats
2. **Standardization**: Standardize all documentation to markdown
3. **Metadata Preservation**: Preserve metadata during conversion
4. **Error Handling**: Handle conversion errors gracefully

### 7.3 Performance Considerations

1. **Parallelization**: Implement parallel processing for CPU-intensive operations
2. **Memory Management**: Optimize memory usage for large codebases
3. **Incremental Processing**: Support incremental updates to avoid reprocessing
4. **Caching**: Cache intermediate results to improve performance

### 7.4 Extensibility

1. **Plugin Architecture**: Use interfaces for all components to support extensibility
2. **Custom Operators**: Allow custom genetic operators
3. **Custom Fitness Functions**: Support custom fitness evaluation
4. **Language Support**: Design for multi-language support

## 8. Conclusion

This enhanced model specification document provides a comprehensive blueprint for implementing the Genetic Algorithm Specification Optimization system. It addresses the implementation challenges identified during development by providing detailed specifications for all data models and their relationships.

The document ensures proper alignment between models and their usage throughout the codebase, providing a solid foundation for continuing the implementation.

Key benefits of this approach include:

1. **Consistency**: Ensures consistent model usage across the codebase
2. **Completeness**: Provides all required properties and methods
3. **Clarity**: Clearly defines relationships between components
4. **Extensibility**: Supports future extensions and customizations

By following these specifications, the implementation can proceed with a clear understanding of the data models and their relationships, avoiding compilation errors and ensuring a robust and maintainable codebase.
