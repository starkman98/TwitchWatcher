using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
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

        private readonly IChannelStateUpdater _channelStateUpdater;
        private readonly IChannelTitleUpdater _channelTitleUpdater;
        private readonly IChannelImageUrlUpdater _channelImageUrlUpdater;
        
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
            IChannelStateUpdater channelStateUpdater,
            IChannelTitleUpdater channelTitleUpdater,
            IChannelImageUrlUpdater channelImageUrlUpdater)
        {
            _api = api;
            _playerFactory = playerFactory;
            _options = options;
            _log = log;
            _channelStateUpdater = channelStateUpdater;
            _channelTitleUpdater = channelTitleUpdater;
            _channelImageUrlUpdater = channelImageUrlUpdater;
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

            foreach (var login in desired.Except(_states.Keys).ToList())
            {
                try
                {
                    var userId = await _api.GetUserIdAsync(login, ct);
                    _userIds[login] = userId;
                    _states[login] = StreamState.Unknown;

                    var title = await _api.GetChannelTitleAsync(login, ct);
                    _titles[login] = title;

                    var imageUrl = await _api.GetChannelImageUrlAsync(login, ct);
                    _imageUrl[login] = imageUrl;

                    var player = _playerFactory.Create(login);
                    _players[login] = player;

                    _log.LogInformation("## [{Login}] Added (userId={UserId})", login, userId);
                }
                catch (Exception ex)
                {
                    _log.LogInformation("## [{Login}] Failed to add channel (will retry on next sync).", login);
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
            }
        }

        private async Task PollAllAsync(CancellationToken ct)
        {
            var logins = _states.Keys.ToList();
            foreach (var login in logins)
            {
                await PollOneAsync(login, ct);

                await Task.Delay(100, ct);
            }
        }
        
        private async Task PollOneAsync(string login, CancellationToken ct)
        {
            if (!_userIds.TryGetValue(login, out var userId) || !_players.TryGetValue(login, out var player)) return;

            try
            {
                var isLive = await _api.IsLiveAsync(userId, ct);
                var next = isLive ? StreamState.Live : StreamState.Offline;
                var prev = _states[login];

                if (next == StreamState.Live && !player.IsOpen)
                {
                    _log.LogInformation("## [{Login}] Live and player not open => opening.", login);
                    var url = new Uri($"https://www.twitch.tv/{login}");
                    await player.OpenAsync(url, ct);
                }
                else if (prev == StreamState.Live && next == StreamState.Offline)
                {
                    _log.LogInformation("## [{Login}] Went offline => closing.", login);
                    await player.CloseAsync(ct);
                }

                if (prev == StreamState.Unknown)
                {
                    _log.LogInformation("## [{Login}] Initial state = {state}", login, next);
                }

                if (_states[login] != StreamState.Unknown)
                {
                    _log.LogInformation("## [{Login}] is still {state}", login, _states[login]);
                }

                _states[login] = next;

                _channelStateUpdater.UpdateChannelState(login, next);

                var title = await _api.GetChannelTitleAsync(login, ct);
                _channelTitleUpdater.UpdateChannelTitle(login, title);

                var imageUrl = await _api.GetChannelImageUrlAsync(login, ct);
                _channelImageUrlUpdater.UpdateChannelImageUrl(login, imageUrl);

            
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _log.LogInformation("## MultiChannelWatcher is stopping (cancellation requested).");
            }
            catch (Exception ex)
            {
                _log.LogInformation(ex, "## [{Login}] Poll failed, will retry.", login);
            }
        }
    }
}
