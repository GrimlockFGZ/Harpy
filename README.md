# 🎮 Harpy Engine

A lightweight, high-performance C# game engine built on modern graphics and ECS architecture. Harpy combines a robust entity-component system with OpenGL rendering to provide a flexible foundation for 3D game development.

## ✨ Features

- **Entity-Component System (ECS)** - Scalable architecture using sparse-set pools for optimal iteration performance
- **OpenGL Rendering** - Modern graphics pipeline using Silk.NET for hardware-accelerated rendering
- **Shader System** - Hot-reloadable shaders with compile-time error reporting
- **Mesh Management** - Efficient mesh rendering with support for instanced drawing
- **Scene Management** - Organized scene and system lifecycle management
- **Avalonia Editor** - WYSIWYG editor with hierarchy panel and asset browser
- **Time Service** - Frame-rate independent delta time calculations
- **Comprehensive Logging** - Debug-friendly logging with color-coded output

## 🏗️ Project Structure

```
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
```

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 or higher
- Visual Studio 2022 or compatible IDE

### Building

**Windows:**
```batch
.\build.bat
```

**Manual Build:**
```bash
dotnet build HarpyEngine.slnx
```

### Running the Sandbox

```bash
dotnet run --project Sandbox
```

## 🎯 Core Systems

### Entity-Component System

The `Registry` class manages entities and their components using a sparse-set pool architecture for high-performance iteration:

```csharp
var registry = new Registry();
var entity = registry.CreateEntity();
registry.AddComponent(entity, new Transform { Position = Vector3.Zero });

// Query all entities with a component type
var entities = registry.ViewEntities<Transform>();
var components = registry.ViewData<Transform>();
```

### Scene Management

Organize gameplay logic through systems and scenes:

```csharp
var scene = new Scene();
scene.AddSystem(new RenderSystem());
scene.AddSystem(new PhysicsSystem());

var context = new SceneContext { /* ... */ };
scene.Initialize(context);
scene.Update(context, deltaTime);
scene.Render(context, deltaTime);
```

### Rendering

**Shaders:**
```csharp
var shader = new Shader(gl, "vertex.glsl", "fragment.glsl");
shader.Use();
// Shaders automatically reload when source files change
shader.Reload();
```

**Meshes:**
```csharp
var vertices = new[] { /* ... */ };
var mesh = new Mesh(gl, vertices);
mesh.DrawInstanced(instanceCount);
```

### Logging

Debug-friendly logging with file/line information and color-coded output:

```csharp
Logger.LogInfo("Game started");
Logger.LogWarning("Performance warning");
Logger.LogError("Shader compilation failed");
Logger.LogSuccess("Level loaded");
```

## 🛠️ Development

### Project Organization

- **Engine** - Framework-agnostic core systems and utilities
- **Engine.Rendering** - Graphics pipeline using Silk.NET
- **Engine.Resources** - Asset loading and management
- **Engine.Tests** - Unit tests
- **Sandbox** - Demo application and editor UI

### Architecture Highlights

- **Sparse-Set ECS** - O(1) component access with cache-friendly iteration
- **Pool-Based Memory Management** - Reduced allocations through dense storage
- **Hot-Reloadable Shaders** - Iterate quickly without rebuilding
- **Decoupled Systems** - Rendering, physics, and gameplay independent

## 📦 Dependencies

- **Silk.NET** - OpenGL bindings and window management
- **Avalonia** - Cross-platform UI framework
- **xUnit** - Unit testing framework

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

## 📝 Notes

- The engine is in active development
- The Sandbox serves as both a demo and testing ground for engine features
- Hot shader reloading is enabled for rapid iteration during development
