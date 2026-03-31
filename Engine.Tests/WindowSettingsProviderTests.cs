using NUnit.Framework;
using HarpyEngine;
using HarpyEngine.Rendering.Helios;
using Silk.NET.Windowing;
using Silk.NET.Maths;

namespace Engine.Tests;

[TestFixture]
public class WindowSettingsProviderTests
{
    [Test]
    public void DefaultSettings_AreCorrect()
    {
        var provider = new WindowSettingsProvider();
        
        Assert.That(provider.Title, Is.EqualTo("Harpy Engine"));
        Assert.That(provider.Width, Is.EqualTo(1920));
        Assert.That(provider.Height, Is.EqualTo(1080));
        Assert.That(provider.State, Is.EqualTo(WindowState.Fullscreen));
    }

    [Test]
    public void GetOptions_ReturnsCorrectOptions()
    {
        var provider = new WindowSettingsProvider
        {
            Title = "Test Window",
            Width = 800,
            Height = 600,
            State = WindowState.Normal
        };

        var options = provider.GetOptions();

        Assert.That(options.Title, Is.EqualTo("Test Window"));
        Assert.That(options.Size, Is.EqualTo(new Vector2D<int>(800, 600)));
        Assert.That(options.WindowState, Is.EqualTo(WindowState.Normal));
        Assert.That(options.API.API, Is.EqualTo(ContextAPI.OpenGL));
        Assert.That(options.API.Profile, Is.EqualTo(ContextProfile.Core));
        Assert.That(options.API.Version.MajorVersion, Is.EqualTo(3));
        Assert.That(options.API.Version.MinorVersion, Is.EqualTo(3));
    }
}
