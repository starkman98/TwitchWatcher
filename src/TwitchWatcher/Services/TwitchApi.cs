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

        public async Task<string> GetUserIdAsync(string login, CancellationToken ct = default)
        {
            var token = await _auth.GetTokenAsync(ct);

            var request = BuildUserRequest(login, token);

            var client = _httpClientFactory.CreateClient("TwitchHelix");
            var response = await client.SendAsync(request, ct);
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            var result = await JsonSerializer.DeserializeAsync<UsersResponse>(stream, cancellationToken: ct);

            if (result?.Data == null || result.Data.Count == 0)
                throw new Exception($"User {login} not found.");

            return result.Data[0].Id;
        }

        public async Task<bool> IsLiveAsync(string userId, CancellationToken ct = default)
        {
            var token = await _auth.GetTokenAsync(ct);
            var client = _httpClientFactory.CreateClient("TwitchHelix");

            var request = BuildStreamRequest(userId, token);

            var response = await client.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                token = await _auth.GetTokenAsync(ct);
                request = BuildStreamRequest(userId, token);
                response = await client.SendAsync(request, ct);
            }

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            var result = await JsonSerializer.DeserializeAsync<StreamsResponse>(stream, cancellationToken: ct);

            if (result?.Data == null || result.Data.Count == 0) return false;

            return string.Equals(result.Data[0].Type, "live", StringComparison.OrdinalIgnoreCase);
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
