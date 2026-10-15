using System.Buffers.Binary;
using System.Text.Json;

namespace ZombieShooter.Core.Networking;

public enum NetworkAction { Reload, Pistol, Shotgun, Undo }
public sealed record ClientFrame(float MoveX, float MoveY, float AimX, float AimY, bool Fire, NetworkAction[] Actions);
public sealed record ServerFrame(int PlayerId, SessionSnapshot Snapshot);

public static class WireProtocol
{
    public const int MaximumBytes = 1_048_576;
    public static async Task WriteAsync<T>(Stream stream, T message, CancellationToken cancellation)
    {
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(message);
        if (data.Length > MaximumBytes) throw new InvalidDataException("Frame exceeds protocol limit.");
        byte[] header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, data.Length);
        await stream.WriteAsync(header, cancellation);
        await stream.WriteAsync(data, cancellation);
    }
    public static async Task<T> ReadAsync<T>(Stream stream, CancellationToken cancellation)
    {
        byte[] header = new byte[4];
        await stream.ReadExactlyAsync(header, cancellation);
        int length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length <= 0 || length > MaximumBytes) throw new InvalidDataException("Invalid frame size.");
        byte[] data = new byte[length];
        await stream.ReadExactlyAsync(data, cancellation);
        return JsonSerializer.Deserialize<T>(data) ?? throw new InvalidDataException("Empty frame.");
    }
}
