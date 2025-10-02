using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using TwitchWatcher.Configuration;
using TwitchWatcher.Contracts;
using TwitchWatcher.Models;

namespace TwitchWatcher.Services
{
    public class StreamWatcher : BackgroundService
    {
        private readonly ITwitchApi _api;
        private readonly IPlayerService _player;
        private readonly AppOptions _options;
        private readonly ILogger<StreamWatcher> _log;

        private StreamState _state = StreamState.Offline;
        private string? _userId;

        public StreamWatcher(ITwitchApi api, IPlayerService player, IOptions<AppOptions> options, ILogger<StreamWatcher> log)
        {
            _api = api;
            _player = player;
            _options = options.Value;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _log.LogInformation("## StreamWatcher starting for channel {Channel}", _options.ChannelName);
            var channel = _options.ChannelName.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(channel))
            {
                _log.LogError("## App:ChannelName is empty, configure a channelname");
                return;
            }

            _userId = await ResolveUserIdWithRetryAsync(channel, ct);
            if (_userId is null)
            {
                _log.LogError("## Could not resolve user id for '{Channel}'. Exiting watcher.", channel);
                return;
            }

            await RunLoopAsync(channel, ct);
        }

        private async Task<string?> ResolveUserIdWithRetryAsync(string channel, CancellationToken ct)
        {
            var attempts = 0;
            while (attempts < 3 && !ct.IsCancellationRequested)
            {
                try
                {
                    attempts++;
                    var id = await _api.GetUserIdAsync(channel, ct);
                    _log.LogInformation("## Resolved '{Channel}' → userId '{UserId}'", channel, id);
                    return id;
                }
                catch (Exception ex) when (attempts < 3)
                {
                    _log.LogWarning(ex, "## Failed to resolve user id (attempt {Attempt}/3). Retrying…", attempts);
                    await Task.Delay(TimeSpan.FromSeconds(2 * attempts), ct);
                }
            }
            return null;
        }

        private async Task RunLoopAsync(string channel, CancellationToken ct)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(5, _options.PollIntervalSeconds));
            var tick = 0;

            using var timer = new PeriodicTimer(interval);

            _log.LogInformation("## Started watcher for '{Channel}' every {Seconds}s.", channel, interval.TotalSeconds);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var isLive = await _api.IsLiveAsync(_userId, ct);
                    var next = isLive ? StreamState.Live : StreamState.Offline;

                    if (next == StreamState.Live && !_player.IsOpen)
                    {
                        _log.LogInformation("## Stream is live, opening stream if its not already opened");
                        await _player.OpenAsync(new Uri($"https://twitch.tv/{channel}"));
                    }
                    else if (_state == StreamState.Live && next == StreamState.Offline)
                    {
                        _log.LogInformation("## Transition Live → Offline. Closing stream…");
                        await _player.CloseAsync(ct);
                    }


                    if (_state == StreamState.Unknown)
                    {
                        _log.LogInformation("## Initial state is {State}.", next);
                    }

                    _state = next;

                    tick++;
                    if (tick % 2 == 0)
                    {
                        _log.LogInformation("## Heartbeat: still {State}.", _state);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "## Polling failed; will retry after a short backoff.");
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                }

                await timer.WaitForNextTickAsync(ct);
            }

            _log.LogInformation("## Watcher stopping.");
        }
    }
}
