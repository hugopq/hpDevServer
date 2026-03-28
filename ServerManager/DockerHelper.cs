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

    public static async Task StartAllAsync(Action<string> onOutput)
    {
        await EnsureDockerRunningAsync(onOutput);
        await RunComposeStreamAsync("up -d", onOutput);
    }

    public static Task StopAllAsync(Action<string> onOutput) => RunComposeStreamAsync("stop", onOutput);

    public static async Task StartServiceAsync(string service, Action<string> onOutput)
    {
        await EnsureDockerRunningAsync(onOutput);
        var svcName = Services.First(s => s.Service == service).Service.ToLower();
        await RunComposeStreamAsync($"up -d {svcName}", onOutput);
    }

    public static async Task EnsureDockerRunningAsync(Action<string> onOutput)
    {
        if (await IsDockerRunningAsync()) return;

        onOutput("Docker não está a correr. A tentar iniciar o Docker Desktop...");

        var dockerExe =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         @"Programs\Docker\Docker\Docker Desktop.exe");

        if (!File.Exists(dockerExe))
            dockerExe = @"C:\Program Files\Docker\Docker\Docker Desktop.exe";

        if (!File.Exists(dockerExe))
        {
            onOutput("Docker Desktop não encontrado. Verifica se está instalado.");
            return;
        }

        Process.Start(new ProcessStartInfo(dockerExe) { UseShellExecute = true });

        // Aguarda até o Docker estar pronto (máx. 60 segundos)
        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(2000);
            if (await IsDockerRunningAsync())
            {
                onOutput("Docker Desktop iniciado.");
                return;
            }
            onOutput($"A aguardar Docker Desktop... ({(i + 1) * 2}s)");
        }

        onOutput("Tempo esgotado a aguardar pelo Docker Desktop.");
    }

    private static async Task<bool> IsDockerRunningAsync()
    {
        try
        {
            var psi = new ProcessStartInfo("docker", "ps")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var p = Process.Start(psi)!;
            await p.WaitForExitAsync();
            return p.ExitCode == 0;
        }
        catch { return false; }
    }

    public static Task StopServiceAsync(string service, Action<string> onOutput)
    {
        var svcName = Services.First(s => s.Service == service).Service.ToLower();
        return RunComposeStreamAsync($"stop {svcName}", onOutput);
    }

    public static async Task<string> RunBackupAsync()
    {
        var fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
        var filePath = Path.Combine(ComposePath, fileName);

        var psi = new ProcessStartInfo("docker", "exec mysql_central mysqldump -u root -proot --all-databases")
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var p = Process.Start(psi)!;
        var output = await p.StandardOutput.ReadToEndAsync();
        await p.WaitForExitAsync();

        if (p.ExitCode != 0)
        {
            var err = await p.StandardError.ReadToEndAsync();
            throw new Exception(err);
        }

        await File.WriteAllTextAsync(filePath, output);
        return filePath;
    }

    private static Task RunComposeStreamAsync(string args, Action<string> onOutput)
    {
        var file = Path.Combine(ComposePath, "docker-compose.yml");
        return RunStreamAsync("docker", $"compose -f \"{file}\" {args}", onOutput);
    }

    public static async Task RunStreamAsync(string exe, string args, Action<string> onOutput)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var p = Process.Start(psi)!;

        p.OutputDataReceived += (_, e) => { if (e.Data != null) onOutput(e.Data); };
        p.ErrorDataReceived  += (_, e) => { if (e.Data != null) onOutput(e.Data); };
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        await p.WaitForExitAsync();
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
