using Engine;

namespace HarpyEngine.Rendering;

/// <summary>
/// A perspective camera. Owns its own position/rotation rather than requiring
/// an ECS Transform, so it can be used standalone in the viewport (editor camera)
/// or driven by a Transform component later (e.g. an in-scene camera entity).
/// </summary>
public class Camera
{
    public Vector3 Position { get; set; }

    /// <summary>Yaw in radians, rotation around the world Up axis.</summary>
    public float Yaw { get; set; }

    /// <summary>Pitch in radians, rotation around the local Right axis. Clamped to avoid gimbal flip.</summary>
    public float Pitch { get; set; }

    public float FieldOfViewRadians { get; set; } = MathF.PI / 4f; // 45 degrees
    public float NearPlane { get; set; } = 0.05f;
    public float FarPlane { get; set; } = 1000f;

    private const float MaxPitch = MathF.PI / 2f - 0.01f;

    public Camera(Vector3? position = null)
    {
        Position = position ?? new Vector3(0f, 0f, 3f);
    }

    /// <summary>
    /// Forward direction derived from yaw/pitch, using a right-handed,
    /// Y-up convention: yaw=0, pitch=0 looks down -Z.
    /// </summary>
    public Vector3 Forward
    {
        get
        {
            var cosPitch = MathF.Cos(Pitch);
            var x = MathF.Sin(Yaw) * cosPitch;
            var y = MathF.Sin(Pitch);
            var z = -MathF.Cos(Yaw) * cosPitch;
            return new Vector3(x, y, z).Normalized();
        }
    }

    public Vector3 Right => Vector3.Cross(Forward, Vector3.Up).Normalized();

    public Vector3 Up => Vector3.Cross(Right, Forward).Normalized();

    public void AddYawPitch(float deltaYaw, float deltaPitch)
    {
        Yaw += deltaYaw;
        Pitch = Math.Clamp(Pitch + deltaPitch, -MaxPitch, MaxPitch);
    }

    public Matrix4x4 GetViewMatrix() => Matrix4x4.LookAt(Position, Position + Forward, Vector3.Up);

    public Matrix4x4 GetProjectionMatrix(float aspectRatio) =>
        Matrix4x4.Perspective(FieldOfViewRadians, MathF.Max(aspectRatio, 0.0001f), NearPlane, FarPlane);
}
