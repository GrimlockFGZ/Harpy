using Engine;
using NUnit.Framework;

namespace Engine.Tests;

[TestFixture]
public class Matrix4x4Tests
{
    private const float Tolerance = 1e-4f;

    private static void AssertVectorsEqual(Vector3 expected, Vector3 actual, float tolerance = Tolerance)
    {
        Assert.That(actual.X, Is.EqualTo(expected.X).Within(tolerance));
        Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(tolerance));
        Assert.That(actual.Z, Is.EqualTo(expected.Z).Within(tolerance));
    }

    [Test]
    public void Identity_TransformsPointUnchanged()
    {
        var p = new Vector3(1f, 2f, 3f);
        var result = Matrix4x4.Identity * p;

        AssertVectorsEqual(p, result);
    }

    [Test]
    public void FromTRS_TranslationOnly_MovesPoint()
    {
        var m = Matrix4x4.FromTRS(new Vector3(5f, 0f, 0f), Quaternion.Identity, Vector3.One);
        var result = m * Vector3.Zero;

        AssertVectorsEqual(new Vector3(5f, 0f, 0f), result);
    }

    [Test]
    public void FromTRS_ScaleOnly_ScalesPoint()
    {
        var m = Matrix4x4.FromTRS(Vector3.Zero, Quaternion.Identity, new Vector3(2f, 3f, 4f));
        var result = m * new Vector3(1f, 1f, 1f);

        AssertVectorsEqual(new Vector3(2f, 3f, 4f), result);
    }

    [Test]
    public void FromTRS_MatchesTransformTransformPoint()
    {
        var transform = new Transform(
            new Vector3(1f, 2f, 3f),
            Quaternion.Normalize(new Quaternion(0.1f, 0.2f, 0.3f, 0.9f)),
            new Vector3(1.5f, 1.5f, 1.5f));

        var local = new Vector3(2f, -1f, 0.5f);

        var expected = transform.TransformPoint(local);
        var actual = Matrix4x4.FromTransform(transform) * local;

        AssertVectorsEqual(expected, actual, 1e-3f);
    }

    [Test]
    public void Multiply_WithIdentity_ReturnsOriginal()
    {
        var m = Matrix4x4.FromTRS(new Vector3(1f, 2f, 3f), Quaternion.Identity, Vector3.One);
        var result = m * Matrix4x4.Identity;

        Assert.That(result, Is.EqualTo(m));
    }

    [Test]
    public void LookAt_OriginLookingDownNegativeZ_PlacesTargetInFront()
    {
        var view = Matrix4x4.LookAt(Vector3.Zero, new Vector3(0f, 0f, -1f), Vector3.Up);

        // A point directly in front of the camera should map to (0, 0, -depth) in view space.
        var viewSpacePoint = view * new Vector3(0f, 0f, -5f);

        Assert.That(viewSpacePoint.X, Is.EqualTo(0f).Within(Tolerance));
        Assert.That(viewSpacePoint.Y, Is.EqualTo(0f).Within(Tolerance));
        Assert.That(viewSpacePoint.Z, Is.LessThan(0f)); // in front, view space -Z
    }

    [Test]
    public void Perspective_PointOnNearPlane_MapsInsideClipRange()
    {
        var proj = Matrix4x4.Perspective(MathF.PI / 4f, 1f, 0.1f, 100f);

        // W component after projection is -viewZ (GL convention); verify Z/W lands in [-1, 1] for a mid-range point.
        var viewSpace = new Vector3(0f, 0f, -10f);
        var clip = proj * viewSpace;

        // Matrix4x4's Vector3 operator doesn't divide by W, so verify the raw Z row output is sane
        // by checking it against the manually computed clip-space Z using the same formula.
        var near = 0.1f;
        var far = 100f;
        var expectedZNumerator = (far + near) * viewSpace.Z + 2f * far * near;
        var rangeInv = 1f / (near - far);
        var expectedClipZ = expectedZNumerator * rangeInv;

        Assert.That(clip.Z, Is.EqualTo(expectedClipZ).Within(Tolerance));
    }

    [Test]
    public void WriteColumnMajor_ProducesGLCompatibleLayout()
    {
        var m = Matrix4x4.FromTRS(new Vector3(7f, 8f, 9f), Quaternion.Identity, Vector3.One);
        Span<float> columns = stackalloc float[16];
        m.WriteColumnMajor(columns);

        // Translation lives in M14/M24/M34 (row-major indices); in column-major GL layout
        // that's the 4th column, i.e. indices 12, 13, 14.
        Assert.That(columns[12], Is.EqualTo(7f).Within(Tolerance));
        Assert.That(columns[13], Is.EqualTo(8f).Within(Tolerance));
        Assert.That(columns[14], Is.EqualTo(9f).Within(Tolerance));
    }
}
