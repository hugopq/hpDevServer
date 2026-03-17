namespace ServerManager;

using System.Diagnostics;

static class DockerHelper
{
    public static readonly (string Service, string Container)[] Services =
    [
        ("MySQL",      "mysql_central"),
        ("Redis",      "redis_central"),
        ("phpMyAdmin", "phpmyadmin_central"),
    ];

    private static readonly string ComposePath = FindComposePath();

    private static string FindComposePath()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docker-compose.yml")))
                return dir.FullName;
            dir = dir.Parent!;
        }
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "hpDevServer");
    }

    public static async Task<Dictionary<string, bool>> GetStatusesAsync()
    {
        var output = await RunAsync("docker", "ps --format {{.Names}}");
        return Services.ToDictionary(
            s => s.Service,
            s => output.Contains(s.Container));
    }

    public static Task StartAllAsync() => RunComposeAsync("up -d");
    public static Task StopAllAsync()  => RunComposeAsync("stop");

    public static Task StartServiceAsync(string service)
    {
        var svcName = Services.First(s => s.Service == service).Service.ToLower();
        return RunComposeAsync($"up -d {svcName}");
    }

    public static Task StopServiceAsync(string service)
    {
        var svcName = Services.First(s => s.Service == service).Service.ToLower();
        return RunComposeAsync($"stop {svcName}");
    }

    private static Task RunComposeAsync(string args)
    {
        var file = Path.Combine(ComposePath, "docker-compose.yml");
        return RunAsync("docker", $"compose -f \"{file}\" {args}");
    }

    public static async Task<string> RunAsync(string exe, string args)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };
        using var p = Process.Start(psi)!;
        var output = await p.StandardOutput.ReadToEndAsync();
        await p.WaitForExitAsync();
        return output;
    }
}
