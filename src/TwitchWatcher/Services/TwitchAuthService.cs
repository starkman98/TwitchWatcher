using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TwitchWatcher.Configuration;
using TwitchWatcher.Contracts;

namespace TwitchWatcher.Services
{
    public class TwitchAuthService : ITwitchAuthService
    {
        private readonly HttpClient _http;
        private readonly AppOptions _options;

        private string? _cachedToken;
        private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

        public TwitchAuthService(IOptions<AppOptions> options)
        {
            _options = options.Value;
            _http = new HttpClient { BaseAddress = new Uri("https://id.twitch.tv/") };
        }

        public async Task<string> GetTokenAsync(CancellationToken ct = default)
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt)
                return _cachedToken;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["grant_type"] = "client_credentials"
            });

            var response = await _http.PostAsync("oauth2/token", content, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var token = json.RootElement.GetProperty("access_token").GetString();
            var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();

            _cachedToken = token!;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);
            
            return _cachedToken;
        }
    }
}
