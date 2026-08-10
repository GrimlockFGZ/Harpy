# Harpy Engine

[![License](https://img.shields.io/github/license/GrimlockFGZ/Harpy?style=flat-square)](https://github.com/GrimlockFGZ/Harpy/blob/main/LICENSE)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![AOT Ready](https://img.shields.io/badge/Native%20AOT-Supported-success?style=flat-square)

Harpy is a lightweight C# game engine focused on modern graphics and clean architecture. It combines a custom Entity-Component System (ECS) with an OpenGL rendering pipeline (via Silk.NET) to provide a flexible base for 3D games and tools.

---

## Features

* Uses a custom ECS driven by sparse-set pools for fast iteration
* Wraps hardware-accelerated OpenGL graphics through Silk.NET
* Hot-reloads shaders on the fly with compile-time error reporting
* Handles meshes with support for instanced drawing
* Organizes gameplay logic through a decoupled system and scene manager
* Calculates frame-rate independent delta time and outputs color-coded, file-specific logs

---

## Project layout

```text
Harpy/
├── Engine/                    # Core engine framework
│   ├── Core/                 # ECS, registry, time service, and logger
│   ├── Exceptions/           # Custom exception types
│   └── Engine.csproj
├── Engine.Rendering/         # Graphics rendering systems
│   ├── Mesh.cs              # Mesh data and rendering
│   ├── Shader.cs            # Shader compilation and management
│   └── Engine.Rendering.csproj
├── Engine.Resources/         # Asset management and resource handling
│   └── Engine.Resources.csproj
├── Engine.Tests/            # Unit tests for engine components
│   └── Engine.Tests.csproj
├── Sandbox/                 # Demo application and editor
│   ├── App.cs              # Avalonia application entry point
│   ├── Editor/             # Editor UI components
│   └── Sandbox.csproj
└── HarpyEngine.slnx        # Solution file
