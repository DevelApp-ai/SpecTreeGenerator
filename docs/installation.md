# GeneticSpecOptimizer Installation Guide

This document provides instructions for installing and setting up the GeneticSpecOptimizer system.

## Prerequisites

Before installing GeneticSpecOptimizer, ensure you have the following prerequisites:

- **.NET 8 SDK** or later
- **Git** (optional, for cloning the repository)
- **Visual Studio 2022** or **Visual Studio Code** with C# extensions (recommended for development)

## Installation Steps

### 1. Install .NET 8 SDK

If you don't have .NET 8 SDK installed, download and install it from the [official .NET download page](https://dotnet.microsoft.com/download/dotnet/8.0).

Verify the installation by running:

```bash
dotnet --version
```

The output should show version 8.0.0 or higher.

### 2. Get the Source Code

#### Option A: Clone the Repository (if using Git)

```bash
git clone https://github.com/DevelApp-ai/SpecTreeGenerator.git
cd SpecTreeGenerator
```

#### Option B: Download the Source Code

Download the source code as a ZIP file and extract it to your preferred location.

### 3. Build the Solution

Navigate to the root directory of the project and build the solution:

```bash
cd /path/to/SpecTreeGenerator
dotnet build
```

This will restore all NuGet packages and build the solution.

### 4. Run the Sample Application (Optional)

To verify that everything is working correctly, you can run the sample application:

```bash
cd src/Sample
dotnet run
```

This will execute a sample optimization pipeline on a small test project.

## Project Structure

The GeneticSpecOptimizer solution is organized into the following projects:

- **GeneticSpecOptimizer.Common**: Common models and utilities
- **GeneticSpecOptimizer.CodeAnalysis**: Code analysis components
- **GeneticSpecOptimizer.DocumentationExtraction**: Documentation extraction components
- **GeneticSpecOptimizer.GeneticAlgorithm**: Genetic algorithm implementation
- **GeneticSpecOptimizer.HierarchyOptimization**: Hierarchy optimization components
- **GeneticSpecOptimizer.Pipeline**: Main pipeline implementation
- **Sample**: Sample application demonstrating usage

## Troubleshooting

### Common Issues

#### Missing .NET SDK

If you encounter errors related to missing .NET SDK, ensure you have installed .NET 8 SDK and that it's in your PATH.

#### Build Errors

If you encounter build errors:

1. Make sure all NuGet packages are restored: `dotnet restore`
2. Check that you're using .NET 8 SDK: `dotnet --version`
3. Clear the build cache and rebuild: `dotnet clean && dotnet build`

#### Runtime Errors

If you encounter runtime errors:

1. Check the error message for specific details
2. Ensure all configuration settings are correct
3. Verify that input files and directories exist and are accessible

## Next Steps

After installation, refer to the [Usage Guide](usage.md) for instructions on how to use the GeneticSpecOptimizer system.
