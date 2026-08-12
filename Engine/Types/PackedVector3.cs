using System.Runtime.InteropServices;

namespace Engine;

[StructLayout(LayoutKind.Sequential)]
public readonly struct PackedVertex : IEquatable<PackedVertex>
{
    // Layout: [Light:4] [BlockID:7] [Normal:3] [Z:6] [Y:6] [X:6]
    private readonly uint _data;

    private const uint SixBitMask  = 0x3F;
    private const uint ThreeBitMask = 0x7;
    private const uint SevenBitMask = 0x7F;
    private const uint FourBitMask  = 0xF;

    public PackedVertex(uint rawData) => _data = rawData;

    public PackedVertex(byte x, byte y, byte z, byte normalIndex, ushort blockId, byte lightLevel)
    {
        var px = (uint)Math.Clamp((int)x, 0, 63) & SixBitMask;
        var py = (uint)Math.Clamp((int)y, 0, 63) & SixBitMask;
        var pz = (uint)Math.Clamp((int)z, 0, 63) & SixBitMask;
        var pn = (uint)Math.Clamp((int)normalIndex, 0, 5) & ThreeBitMask;
        var pid = (uint)Math.Clamp((int)blockId, 0, 127) & SevenBitMask;
        var pl = (uint)Math.Clamp((int)lightLevel, 0, 15) & FourBitMask;

        _data = px
                | (py << 6)
                | (pz << 12)
                | (pn << 18)
                | (pid << 21)
                | (pl << 28);
    }

    public uint Raw => _data;

    public byte X => (byte)(_data & SixBitMask);
    public byte Y => (byte)((_data >> 6) & SixBitMask);
    public byte Z => (byte)((_data >> 12) & SixBitMask);
    public byte NormalIndex => (byte)((_data >> 18) & ThreeBitMask);
    public ushort BlockId => (ushort)((_data >> 21) & SevenBitMask);
    public byte LightLevel => (byte)((_data >> 28) & FourBitMask);

    public bool Equals(PackedVertex other) => _data == other._data;
    public override bool Equals(object? obj) => obj is PackedVertex other && Equals(other);
    public override int GetHashCode() => _data.GetHashCode();
    public override string ToString() =>
        $"PackedVertex(X:{X}, Y:{Y}, Z:{Z}, N:{NormalIndex}, ID:{BlockId}, L:{LightLevel})";
}