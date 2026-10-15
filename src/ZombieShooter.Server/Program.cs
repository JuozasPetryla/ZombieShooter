using ZombieShooter.Core;
using ZombieShooter.Core.Networking;

int port = 7777;
if (args.Length > 0 && (!int.TryParse(args[0], out port) || port is < 1 or > 65535))
{
    Console.Error.WriteLine("Usage: ZombieShooter.Server [port] [normal|nightmare|horde|brute]");
    return 1;
}
string mode = args.Length > 1 ? args[1].ToLowerInvariant() : "normal";
if (mode is not ("normal" or "nightmare" or "horde" or "brute")) return 1;
IContentFactory content = mode == "nightmare" ? new NightmareContentFactory() : new NormalContentFactory();
IGameMode? strategy = mode switch { "horde" => new HordeMode(), "brute" => new BruteMode(), _ => null };
await using var server = new GameServer(port, content, strategy);
server.Start();
Console.WriteLine($"Server: 127.0.0.1:{server.Port}, mode {mode}. Waiting for two clients. Ctrl+C stops.");
var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; done.TrySetResult(); };
await done.Task;
return 0;
