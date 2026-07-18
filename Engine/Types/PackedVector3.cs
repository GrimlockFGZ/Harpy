
namespace Engine;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public readonly struct PackedVector3 : IEquatable<PackedVector3>
{
    /// <summary>
    /// The internal 32-bit storage.
    /// Layout: [Lighting: 4b] [BlockID: 10b] [Z: 6b] [Y: 6b] [X: 6b]
    /// </summary>
    private readonly uint _data;

    // Masks for bitwise isolation
    private const uint SixBitMask = 0x3F;      // 0b111111 (63)
    private const uint TenBitMask = 0x3FF;     // 0b1111111111 (1023)
    private const uint FourBitMask = 0xF;      // 0b1111 (15)

    // --- Constructors ---

    public PackedVector3(uint rawData) => _data = rawData;

    public PackedVector3(float x, float y, float z, ushort blockId = 0, byte lightLevel = 0)
    {
        // Clamp as floats first (since input is float), then cast to uint
        var packedX = (uint)Math.Clamp(x, 0.0f, 63.0f) & SixBitMask;
        var packedY = (uint)Math.Clamp(y, 0.0f, 63.0f) & SixBitMask;
        var packedZ = (uint)Math.Clamp(z, 0.0f, 63.0f) & SixBitMask;

        // Clamp as uints for the ID and Light
        var packedId = Math.Clamp(blockId, 0u, 1023u) & TenBitMask;
        var packedLight = Math.Clamp(lightLevel, 0u, 15u) & FourBitMask;

        // Perform the packing shifts
        _data = packedX 
                | (packedY << 6) 
                | (packedZ << 12) 
                | (packedId << 18) 
                | (packedLight << 28);
    }


    public float X => _data & SixBitMask;
    public float Y => (_data >> 6) & SixBitMask;
    public float Z => (_data >> 12) & SixBitMask;
    
    public ushort BlockId => (ushort)((_data >> 18) & TenBitMask);
    public byte LightLevel => (byte)((_data >> 28) & FourBitMask);


    /// <summary>
    /// Automatically "inflates" a packed vector into a full Vector3 for math operations.
    /// </summary>
    public static implicit operator Vector3(PackedVector3 packed) => 
        new Vector3(packed.X, packed.Y, packed.Z);

    /// <summary>
    /// Automatically "compresses" a Vector3 into a 6-bit packed coordinate.
    /// Note: This will strip BlockId and LightLevel data if converting from a pure Vector3.
    /// </summary>
    public static implicit operator PackedVector3(Vector3 vector) => 
        new PackedVector3(vector.X, vector.Y, vector.Z);

    // --- Boilerplate ---

    public bool Equals(PackedVector3 other) => _data == other._data;
    public override bool Equals(object? obj) => obj is PackedVector3 other && Equals(other);
    public override int GetHashCode() => _data.GetHashCode();
    
    public override string ToString() => 
        $"PackedVector3(X: {X}, Y: {Y}, Z: {Z}, ID: {BlockId}, Light: {LightLevel})";
}