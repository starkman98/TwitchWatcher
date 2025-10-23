using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using TwitchWatcher.Configuration;
using TwitchWatcher.Contracts;

namespace TwitchWatcher.Services
{
    public class PlayerService : IPlayerService
    {
        private Process? _process;
        private readonly AppOptions _options;
        private readonly string? _login;
        private string? _profilePath = string.Empty;
        private readonly ILogger<PlayerService> _log;

        public bool IsOpen
        {
            get
            {
                if (_process != null && !_process.HasExited)
                    return true;

                if (string.IsNullOrEmpty(_profilePath))
                    return false;

                var pid = TryFindChromePidForProfile(_profilePath);
                return pid.HasValue;
            }
        }


        public PlayerService(IOptions<AppOptions> options, ILogger<PlayerService> log)
        {
            _options = options.Value;
            _log = log;
        }

        public PlayerService(IOptions<AppOptions> options, ILogger<PlayerService> log, string login, string profilePath)
        {
            _options = options.Value;
            _log = log;
            _login = login;
            _profilePath = profilePath;
        }

        public async Task OpenAsync(Uri url, CancellationToken ct)
        {
            _log.LogInformation("## OpenAsync: IsOpen={IsOpen}, Profile={Profile}", IsOpen, _profilePath);

            if (IsOpen) return;
            
            if (_process != null && !_process.HasExited) return;

            var channel = _options.ChannelName;
            //var profilePath = GetProfilePath(channel);
            //_profilePath = profilePath;
            Directory.CreateDirectory(_profilePath);

            var args = new List<string>
            {
                "--new-window",
                $"--user-data-dir=\"{_profilePath}\"",
                "--no-first-run",
                "--no-default-browser-check",
                "--disable-background-mode",
                "--disable-features=RendererCodeIntegrity"
            };

            if (_options.OpenInAppWindow)
            {
                args.Add($"--app=\"{url}\"");
            }
            else
            {
                args.Add($"\"{url}\"");
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _options.ChromePath,
                Arguments = string.Join(" ", args),
                UseShellExecute = false
            };

            _process = Process.Start(processStartInfo);

            await Task.Delay(1000, ct);

            if (!string.IsNullOrEmpty(_profilePath))
            {
                var pid = TryFindChromePidForProfile(_profilePath);
                if (pid is int realPid)
                {
                    _log.LogInformation("## Reattached to PID {Pid} for profile {Profile}", realPid, _profilePath);
                    try { _process = Process.GetProcessById(realPid); } catch { /* ignore */ }
                }
            }
            return;
        }

        public Task CloseAsync(CancellationToken ct)
        {   
            if (_process != null && !_process.HasExited)
            {
                _process.CloseMainWindow();
                if (!_process.WaitForExit(5000)) _process.Kill();
                return Task.CompletedTask;
            }

            if (!string.IsNullOrEmpty(_profilePath))
            {
                var pid = TryFindChromePidForProfile(_profilePath);
                if (pid is int p)
                {
                    try
                    {
                        using var proc = Process.GetProcessById(p);
                        if (!proc.HasExited)
                        {
                            proc.CloseMainWindow();
                            if (!proc.WaitForExit(5000)) proc.Kill();
                        }
                    }
                    catch { /* ignore */ }
                }
            }

            _process = null;
            return Task.CompletedTask;


        }

        private int? TryFindChromePidForProfile(string profilePath)
        {
            try
            {
                // WMI query gives us CommandLine for each chrome.exe process
                using var searcher = new ManagementObjectSearcher(
                    "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='chrome.exe'");
                foreach (ManagementObject mo in searcher.Get())
                {
                    var cmd = (string?)mo["CommandLine"] ?? string.Empty;
                    if (cmd.IndexOf(profilePath, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return Convert.ToInt32(mo["ProcessId"]);
                    }
                }
            }
            catch
            {
                // Swallow and treat as "not found"
            }
            return null;
        }
    }
}
