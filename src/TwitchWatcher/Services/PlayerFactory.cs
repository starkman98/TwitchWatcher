using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using TwitchWatcher.Configuration;
using TwitchWatcher.Contracts;

namespace TwitchWatcher.Services
{
    public class PlayerFactory : IPlayerFactory
    {
        private readonly AppOptions _options;
        private readonly ILoggerFactory _loggerFactory;

        public PlayerFactory(IOptions<AppOptions> options, ILoggerFactory loggerFactory)
        {
            _options = options.Value;
            _loggerFactory = loggerFactory;
        }

        public IPlayerService Create(string login)
        {
            var normalized = Normalize(login);
            var profilePath = BuildProfilePath(normalized, _options.ProfileRootPath);

            var logger = _loggerFactory.CreateLogger<PlayerService>();

            return new PlayerService(Microsoft.Extensions.Options.Options.Create(_options), logger, normalized, profilePath);
        }

        private static string Normalize(string s) => (s ?? string.Empty).Trim().ToLowerInvariant();

        private static string BuildProfilePath(string login, string? configuredRoot)
        {
            string root;
            //if (!string.IsNullOrWhiteSpace(configuredRoot))
            //{
            //    root = Path.GetFullPath(configuredRoot);
            //}
            //else
            //{

            if (string.IsNullOrWhiteSpace(login)) throw new ArgumentException("login empty");

            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                root = Path.Combine(local, "TwitchWatcher", "profiles");
            //}

            var safeLogin = Sanitize(login);
            
            return Path.Combine(root, safeLogin);
        }

        private static string Sanitize (string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            
            foreach (var ch in name)
            {
                sb.Append(invalid.Contains(ch) ? '_' : ch);
            }

            return sb.ToString();
        }
    }
}
