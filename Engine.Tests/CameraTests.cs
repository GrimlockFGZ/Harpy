using Engine;
using HarpyEngine.Rendering;
using NUnit.Framework;

namespace Engine.Tests;

[TestFixture]
public class CameraTests
{
    private const float Tolerance = 1e-4f;

    [Test]
    public void DefaultForward_ZeroYawZeroPitch_LooksDownNegativeZ()
    {
        var camera = new Camera { Yaw = 0f, Pitch = 0f };

        var forward = camera.Forward;

        Assert.That(forward.X, Is.EqualTo(0f).Within(Tolerance));
        Assert.That(forward.Y, Is.EqualTo(0f).Within(Tolerance));
        Assert.That(forward.Z, Is.EqualTo(-1f).Within(Tolerance));
    }

    [Test]
    public void Forward_IsAlwaysUnitLength()
    {
        var camera = new Camera { Yaw = 1.234f, Pitch = 0.5f };

        Assert.That(camera.Forward.Magnitude, Is.EqualTo(1f).Within(Tolerance));
    }

    [Test]
    public void AddYawPitch_ClampsPitchToAvoidGimbalFlip()
    {
        var camera = new Camera();

        camera.AddYawPitch(0f, 100f); // absurdly large upward look

        Assert.That(camera.Pitch, Is.LessThan(MathF.PI / 2f));
        Assert.That(camera.Pitch, Is.GreaterThan(0f));
    }

    [Test]
    public void AddYawPitch_ClampsNegativePitch()
    {
        var camera = new Camera();

        camera.AddYawPitch(0f, -100f);

        Assert.That(camera.Pitch, Is.GreaterThan(-MathF.PI / 2f));
        Assert.That(camera.Pitch, Is.LessThan(0f));
    }

    [Test]
    public void Right_IsPerpendicularToForwardAndUp()
    {
        var camera = new Camera { Yaw = 0.7f, Pitch = 0.3f };

        var dotForward = Vector3.Dot(camera.Right, camera.Forward);
        var dotUp = Vector3.Dot(camera.Right, camera.Up);

        Assert.That(dotForward, Is.EqualTo(0f).Within(Tolerance));
        Assert.That(dotUp, Is.EqualTo(0f).Within(Tolerance));
    }

    [Test]
    public void GetViewMatrix_PlacesCameraPositionAtOrigin()
    {
        var camera = new Camera(new Vector3(3f, 1f, 3f)) { Yaw = 0f, Pitch = 0f };

        var view = camera.GetViewMatrix();
        var cameraInViewSpace = view * camera.Position;

        Assert.That(cameraInViewSpace.X, Is.EqualTo(0f).Within(Tolerance));
        Assert.That(cameraInViewSpace.Y, Is.EqualTo(0f).Within(Tolerance));
        Assert.That(cameraInViewSpace.Z, Is.EqualTo(0f).Within(Tolerance));
    }

    [Test]
    public void GetProjectionMatrix_GuardsAgainstZeroAspectRatio()
    {
        var camera = new Camera();

        Assert.DoesNotThrow(() => camera.GetProjectionMatrix(0f));
    }
}
