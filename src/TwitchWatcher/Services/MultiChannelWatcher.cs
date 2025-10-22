using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using TwitchWatcher.Configuration;
using TwitchWatcher.Contracts;
using TwitchWatcher.Core.Contracts;
using TwitchWatcher.Models;

namespace TwitchWatcher.Services
{
    public class MultiChannelWatcher : BackgroundService
    {
        private readonly ITwitchApi _api;
        private readonly IPlayerFactory _playerFactory;
        private readonly IOptionsMonitor<AppOptions> _options;
        private readonly ILogger<MultiChannelWatcher> _log;

        private readonly IChannelDataStore _store;

        private readonly Dictionary<string, TwitchUser> _users = new();
        private readonly Dictionary<string, TwitchStream> _streams = new();
        private readonly Dictionary<string, string> _userIds = new();
        private readonly Dictionary<string, StreamState> _states = new();
        private readonly Dictionary<string, IPlayerService> _players = new();
        private readonly Dictionary<string, string> _titles = new();
        private readonly Dictionary<string, string> _imageUrl = new();


        private static string Normalize(string s) => (s ?? "").Trim().ToLowerInvariant();

        private TimeSpan PollInterval => TimeSpan.FromSeconds(Math.Max(5, _options.CurrentValue.PollIntervalSeconds));

        public MultiChannelWatcher(
            ITwitchApi api,
            IPlayerFactory playerFactory,
            IOptionsMonitor<AppOptions> options,
            ILogger<MultiChannelWatcher> log,
            IChannelDataStore store)
        {
            _api = api;
            _playerFactory = playerFactory;
            _options = options;
            _log = log;
            _store = store;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _log.LogInformation("## MultiChannelWatcher starting...");
            await SyncChannelsAsync(ct);

            using var timer = new PeriodicTimer(PollInterval);

            try
            {
                await PollAllAsync(ct);

                while (await timer.WaitForNextTickAsync(ct))
                {
                    await SyncChannelsAsync(ct);
                    await PollAllAsync(ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _log.LogInformation("## MultiChannelWatcher is stopping (cancellation requested).");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "## Fatal error in watcher loop: stopping.");
            }
            finally
            {
                foreach (var player in _players)
                {
                    try { await player.Value.CloseAsync(CancellationToken.None); } catch { }
                }
                _log.LogInformation("## MultiChannelWatcher stopped.");
            }
        }

        private async Task SyncChannelsAsync(CancellationToken ct)
        {
            var config = _options.CurrentValue;
            var desired = (config.Channels ?? new()).Select(c => Normalize(c.Login)).Where(s => !string.IsNullOrWhiteSpace(s)).ToHashSet();

            var toAdd = desired.Except(_userIds.Keys).ToList();
            if (toAdd.Count > 0)
            {
                var map = await _api.GetUsersDataByLoginsAsync(toAdd, ct);
                try
                {
                    foreach (var login in toAdd)
                    {
                        if (map.TryGetValue(login, out var user))
                        {
                            _users[login] = user;
                            _userIds[login] = user.Id;
                            _states[login] = StreamState.Unknown;
                            _titles[login] = user.Title;
                            _imageUrl[login] = user.ProfileImageUrl;

                            var player = _playerFactory.Create(login);
                            _players[login] = player;
                    
                            _log.LogInformation("## [{Login}] Added (userId={UserId})", login, user.Id);
                        }
                        else
                        {
                            _log.LogInformation("## [{Login}] User not found during bulk resolve.", login); 
                        }
                    }
                    _store.SetUsers(_users.Values);

                }
                catch (Exception ex)
                {
                    _log.LogInformation(ex, "## Failed to resolve user ids in bulk (will retry on next sync).");
                }
            }

            foreach (var login in _states.Keys.Except(desired).ToList())
            {
                _log.LogInformation("## [{Login}] Removing channel.", login);

                if (_players.TryGetValue(login, out var player))
                {
                    try { await player.CloseAsync(ct); } catch { }
                }

                _players.Remove(login);
                _states.Remove(login);
                _userIds.Remove(login);
                _titles.Remove(login);
                _imageUrl.Remove(login);
                _users.Remove(login);
                _store.RemoveUser(login);
            }
        }

        private async Task PollAllAsync(CancellationToken ct)
        {
            var logins = _states.Keys.ToList();
            if (logins.Count == 0) return;

            var userIds = logins.Select(l => _userIds.TryGetValue(l, out var id) ? id : null)
                                .Where(id => !string.IsNullOrWhiteSpace(id))
                                .Distinct()
                                .ToList();

            try
            {
                var map = await _api.GetStreamsByUserIdsAsync(userIds, ct);

                _streams.Clear();
                if (map != null)
                {
                    foreach (var kv in map)
                    {
                        _streams[kv.Key] = kv.Value;
                    }
                }

                _store.SetStreams(_streams.Values);
            }
            catch (Exception ex)
            {
                _log.LogInformation(ex, "## Bulk streams request failed, will try per-channel fallback.");
            }

            foreach (var login in logins)
            {
                try
                {
                    await UpdateChannelCachesAsync(login, _streams, ct);
                }
                catch (Exception ex)
                {
                    _log.LogInformation(ex, "## [{Login}] Poll failed, will retry next time.", login);
                }
                await Task.Delay(100, ct);
            }
        }

        public async Task UpdateChannelCachesAsync(string login, Dictionary<string, TwitchStream> streams, CancellationToken ct)
        {
            if (!_userIds.TryGetValue(login, out var userId) || !_players.TryGetValue(login, out var player)) return;

            var prevState = _states.TryGetValue(login, out var s) ? s : StreamState.Unknown;
            var isLive = streams.TryGetValue(userId, out var stream);
            var nextState = isLive ? StreamState.Live : StreamState.Offline;

            if (nextState == StreamState.Live && !player.IsOpen)
            {
                _log.LogInformation("## [{Login}] Live and player not open => opening.", login);
                var url = new Uri($"https://www.twitch.tv/{login}");
                await player.OpenAsync(url, ct);
            }
            else if (prevState == StreamState.Live && nextState == StreamState.Offline)
            {
                _log.LogInformation("## [{Login}] Went offline => closing.", login);
                await player.CloseAsync(ct);
            }

            if (prevState == StreamState.Unknown)
            {
                _log.LogInformation("## [{Login}] Initial state = {state}", login, nextState);
            }

            if (_states[login] != StreamState.Unknown)
            {
                _log.LogInformation("## [{Login}] is still {state}", login, _states[login]);
            }
            _states[login] = nextState;
        }
        
        public async Task ClosePlayerAsync(string login, CancellationToken ct = default)
        {
            var key = Normalize(login);
            if (string.IsNullOrWhiteSpace(key)) return;

            if (_players.TryGetValue(key, out var player))
            {
                try { await player.CloseAsync(ct); }
                catch { }

                _players.Remove(key);

                _states.Remove(key);
                _userIds.Remove(key);
                _titles.Remove(key);
                _imageUrl.Remove(key);
                _users.Remove(key);

                try { _store.RemoveUser(key); }
                catch { }
            }
            else { return; }
        }
    }
}
