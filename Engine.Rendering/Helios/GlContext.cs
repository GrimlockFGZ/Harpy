using Silk.NET.OpenGL;

namespace HarpyEngine.Rendering.Helios;

/// <summary>
/// A centralized wrapper for the OpenGL context.
/// This allows for easy injection and management of GL calls throughout the engine.
/// </summary>
public class GlContext : IDisposable
{
    /// <summary>
    /// The Silk.NET OpenGL instance.
    /// </summary>
    public GL Api { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlContext"/> class.
    /// </summary>
    /// <param name="gl">The raw Silk.NET OpenGL instance.</param>
    public GlContext(GL gl)
    {
        Api = gl ?? throw new ArgumentNullException(nameof(gl));
    }

    /// <summary>
    /// Disposes of the underlying OpenGL context.
    /// </summary>
    public void Dispose()
    {
        Api.Dispose();
    }

    /// <summary>
    /// Implicitly convert <see cref="GlContext"/> to <see cref="GL"/> for compatibility with existing Silk.NET methods if needed,
    /// though direct use of Api property is preferred.
    /// </summary>
    /// <param name="context">The GlContext instance.</param>
    public static implicit operator GL(GlContext context) => context.Api;
}
