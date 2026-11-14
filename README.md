# Medical Diagnostic Assistant - Step 1

## Architecture

- `MedicalAI.Core`: imaging/inference contracts and validated domain result models.
- `MedicalAI.Inference`: ONNX Runtime and SkiaSharp image-processing adapter; references Core.
- `MedicalAI.UI`: .NET 8 Blazor Web App with server interactivity; references Core and Inference.
- `MedicalAI.Benchmarks`: console benchmark project; references Core and Inference.

The Core project has no dependency on ONNX Runtime, image libraries, or UI frameworks. No trained model or inference implementation is included in this setup step.

## Recreate with the .NET CLI

Run commands from an empty `MedicalDiagnosticAssistant` directory. These commands use .NET 8 and work with the cross-platform `dotnet` CLI:

```text
dotnet new sln -n MedicalDiagnosticAssistant
dotnet new classlib -n MedicalAI.Core -f net8.0
dotnet new classlib -n MedicalAI.Inference -f net8.0
dotnet new blazor -n MedicalAI.UI -f net8.0 --interactivity Server
dotnet new console -n MedicalAI.Benchmarks -f net8.0

dotnet sln MedicalDiagnosticAssistant.sln add MedicalAI.Core/MedicalAI.Core.csproj
dotnet sln MedicalDiagnosticAssistant.sln add MedicalAI.Inference/MedicalAI.Inference.csproj
dotnet sln MedicalDiagnosticAssistant.sln add MedicalAI.UI/MedicalAI.UI.csproj
dotnet sln MedicalDiagnosticAssistant.sln add MedicalAI.Benchmarks/MedicalAI.Benchmarks.csproj

dotnet add MedicalAI.Inference/MedicalAI.Inference.csproj reference MedicalAI.Core/MedicalAI.Core.csproj
dotnet add MedicalAI.UI/MedicalAI.UI.csproj reference MedicalAI.Inference/MedicalAI.Inference.csproj
dotnet add MedicalAI.UI/MedicalAI.UI.csproj reference MedicalAI.Core/MedicalAI.Core.csproj
dotnet add MedicalAI.Benchmarks/MedicalAI.Benchmarks.csproj reference MedicalAI.Inference/MedicalAI.Inference.csproj
dotnet add MedicalAI.Benchmarks/MedicalAI.Benchmarks.csproj reference MedicalAI.Core/MedicalAI.Core.csproj

dotnet add MedicalAI.Inference/MedicalAI.Inference.csproj package Microsoft.ML.OnnxRuntime
dotnet add MedicalAI.Inference/MedicalAI.Inference.csproj package SkiaSharp
dotnet add MedicalAI.Inference/MedicalAI.Inference.csproj package SkiaSharp.NativeAssets.Linux.NoDependencies
dotnet add MedicalAI.Benchmarks/MedicalAI.Benchmarks.csproj package BenchmarkDotNet
```

`dotnet build MedicalDiagnosticAssistant.sln` restores packages and builds all four projects.
