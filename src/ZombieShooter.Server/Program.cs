using ZombieShooter.Core;
using ZombieShooter.Core.Networking;

int port = 7777;
GamePreset preset = GamePreset.Normal;
if (args.Length > 2 ||
    (args.Length > 0 && (!int.TryParse(args[0], out port) || port is < 1 or > 65535)) ||
    (args.Length > 1 && (!Enum.TryParse(args[1], true, out preset) || !Enum.IsDefined(preset))))
{
    Console.Error.WriteLine("Usage: ZombieShooter.Server [port] [normal|horde|brute]");
    return 1;
}
await using var server = new GameServer(port, preset);
server.Start();
Console.WriteLine($"Server: 127.0.0.1:{server.Port}, mode {preset}. Waiting for two clients. Ctrl+C stops.");
var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; done.TrySetResult(); };
await done.Task;
return 0;
