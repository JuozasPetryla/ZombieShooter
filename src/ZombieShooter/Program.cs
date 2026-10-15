using ZombieShooter;
using ZombieShooter.Core.Networking;

try
{
    NetworkGameClient? client = null;
    if (args.Length > 0)
    {
        if (args[0] != "--connect" || args.Length > 3)
            throw new ArgumentException("Usage: ZombieShooter [--connect [host] [port]]");
        int port = args.Length > 2 ? int.Parse(args[2]) : 7777;
        client = NetworkGameClient.ConnectAsync(args.Length > 1 ? args[1] : "127.0.0.1", port).GetAwaiter().GetResult();
    }
    using (client)
    using (var game = new ZombieShooterGame(client)) game.Run();
    return 0;
}
catch (Exception e) when (e is System.Net.Sockets.SocketException or IOException or
    OperationCanceledException or ArgumentException or FormatException or OverflowException)
{
    Console.Error.WriteLine($"Cannot start game: {e.Message}");
    return 1;
}
