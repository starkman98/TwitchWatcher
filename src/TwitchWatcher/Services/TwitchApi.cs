using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Http.Headers;
using TwitchWatcher.Configuration;
using TwitchWatcher.Contracts;
using System.Text.Json;
using TwitchWatcher.Models;

namespace TwitchWatcher.Services
{
    public class TwitchApi :ITwitchApi
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITwitchAuthService _auth;
        private readonly AppOptions _options;

        public TwitchApi(IHttpClientFactory httpClientFactory, ITwitchAuthService auth, IOptions<AppOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _auth = auth;
            _options = options.Value;
        }

        public async Task<Dictionary<string, TwitchUser>> GetUsersDataByLoginsAsync(IEnumerable<string> logins, CancellationToken ct = default)
        {
            var loginList = logins
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (loginList.Count == 0) return new Dictionary<string, TwitchUser>(StringComparer.OrdinalIgnoreCase);

            var token = await _auth.GetTokenAsync(ct);
            var client = _httpClientFactory.CreateClient("TwitchHelix");
            var query = string.Join("&", loginList.Select(l => $"login={Uri.EscapeDataString(l)}"));
            var request = new HttpRequestMessage(HttpMethod.Get, $"users?{query}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Client-Id", _options.ClientId);

            var response = await client.SendAsync(request, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                token = await _auth.GetTokenAsync(ct);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                response = await client.SendAsync(request, ct);
            }

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            var result = await JsonSerializer.DeserializeAsync<UsersResponse>(stream, cancellationToken: ct);

            var map = new Dictionary<string, TwitchUser>(StringComparer.OrdinalIgnoreCase);
            if (result?.Data != null)
            {
                foreach (var user in result.Data)
                {
                    map[user.Login] = user;
                }
            }

            return map;
        }

        public async Task<Dictionary<string, TwitchStream>> GetStreamsByUserIdsAsync(IEnumerable<string?> userIds, CancellationToken ct = default)
        {
            var ids = userIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (ids.Count == 0) return new Dictionary<string, TwitchStream>();

            var token = await _auth.GetTokenAsync(ct);
            var client = _httpClientFactory.CreateClient("TwitchHelix");
            var query = string.Join("&", ids.Select(id => $"user_id={Uri.EscapeDataString(id)}"));
            var request = new HttpRequestMessage(HttpMethod.Get, $"streams?{query}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Client-Id", _options.ClientId);

            var response = await client.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                token = await _auth.GetTokenAsync(ct);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                response = await client.SendAsync(request, ct);
            }

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            var result = await JsonSerializer.DeserializeAsync<StreamsResponse>(stream, cancellationToken: ct);

            var map = new Dictionary<string, TwitchStream>();
            if (result?.Data != null)
            {
                foreach (var s in result.Data)
                {
                    if (!string.IsNullOrWhiteSpace(s.UserId))
                        map[s.UserId] = s; // last one wins, normally one per user
                }
            }
            
            return map;
        }

        private HttpRequestMessage BuildUserRequest(string login, string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"users?login={login}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Client-Id", _options.ClientId);
            return request;
        }

        private HttpRequestMessage BuildStreamRequest(string userId, string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"streams?user_id={userId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Client-Id", _options.ClientId);
            return request;
        }
    }
}
