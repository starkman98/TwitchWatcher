using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        string? _profilePath = string.Empty;
        
        
        public PlayerService(IOptions<AppOptions> options, ILogger<PlayerService> logger)
        {
            _options = options.Value;
        }

        public Task OpenAsync(Uri url, CancellationToken ct)
        {
            if (_process != null && !_process.HasExited) return Task.CompletedTask;

            var channel = _options.ChannelName;
            var profilePath = GetProfilePath(channel);
            Directory.CreateDirectory(profilePath);

            var args = new List<string>
            {
                "--new-window",
                $"--user-data-dir=\"{profilePath}\"",
                "--no-first-run",
                "--no-default-browser-check"
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
            return Task.CompletedTask;
        }

        public Task CloseAsync(CancellationToken ct)
        {   
            if (_process != null && !_process.HasExited)
            {
                _process.CloseMainWindow();
                if (!_process.WaitForExit(5000)) _process.Kill();
            }
            _process = null;
            return Task.CompletedTask;
        }

        private static string NormalizeChannel(string channel)
        {
            var normalized = (channel ?? "").Trim().ToLowerInvariant();
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                sb.Append(invalid.Contains(ch) ? '_' : ch);
            }
            return sb.ToString();
        }

        private string GetProfilePath(string channel, string? configuredRoot = null)
        {
            var root = string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TwitchWatcher", "profiles")
                : Path.GetFullPath(configuredRoot);
            var safeChannel = NormalizeChannel(channel);
            return Path.Combine(root, safeChannel);
        }
    }
}
