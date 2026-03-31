using NUnit.Framework;
using HarpyEngine;
using Moq;
using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Collections.Generic;
using HarpyEngine.Rendering;

namespace Engine.Tests;

[TestFixture]
public class InputHandlerTests
{
    private InputHandler _inputHandler;
    private Mock<IKeyboard> _mockKeyboard;

    [SetUp]
    public void Setup()
    {
        _inputHandler = new InputHandler();
        _mockKeyboard = new Mock<IKeyboard>();
        
        // Setup default keyboard behavior
        _mockKeyboard.Setup(k => k.IsKeyPressed(It.IsAny<Key>())).Returns(false);
    }

    [Test]
    public void IsWireframe_DefaultsToFalse()
    {
        Assert.That(_inputHandler.IsWireframe, Is.False);
    }

    [Test]
    public void SpaceKey_TogglesWireframe()
    {
        // Register default bindings first
        var registerMethod = typeof(InputHandler).GetMethod("RegisterDefaultBindings", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        registerMethod.Invoke(_inputHandler, null);

        // We need to trigger the KeyDown event. 
        var handleMethod = typeof(InputHandler).GetMethod("HandleKeyDown", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        handleMethod.Invoke(_inputHandler, new object[] { null!, _mockKeyboard.Object, Key.Space });
        Assert.That(_inputHandler.IsWireframe, Is.True);

        handleMethod.Invoke(_inputHandler, new object[] { null!, _mockKeyboard.Object, Key.Space });
        Assert.That(_inputHandler.IsWireframe, Is.False);
    }

}
