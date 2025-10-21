using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using TwitchWatcher.Core.Contracts;
using TwitchWatcher.Models;

namespace TwitchWatcher.Core.Services
{
    public class ChannelDataStore : IChannelDataStore
    {
        private readonly ConcurrentDictionary<string, TwitchUser> _users = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, TwitchStream> _streams = new(StringComparer.OrdinalIgnoreCase);

        public event EventHandler? DataChanged;

        private static string Normalize(string s) => (s ?? string.Empty).Trim().ToLowerInvariant();

        public IReadOnlyDictionary<string, TwitchUser> GetUsersSnapshot() =>
            _users.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, TwitchStream> GetStreamsSnapshot() =>
            _streams.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public bool TryGetUser(string login, out TwitchUser? user) =>
            _users.TryGetValue(Normalize(login), out user);

        public bool TryGetStreamByUserId(string userId, out TwitchStream? stream) =>
            _streams.TryGetValue(userId, out stream);

        public void SetUsers(IEnumerable<TwitchUser> users)
        {
            if (users == null) return;

            foreach (var user in users)
            {
                var key = Normalize(user?.Login ?? string.Empty);
                if (string.IsNullOrWhiteSpace(key)) continue;
                _users[key] = user!;
            }
            OnDataChanged();
        }

        public void SetStreams(IEnumerable<TwitchStream> streams)
        {
            if (streams == null) return;

            foreach (var stream in streams)
            {
                if (string.IsNullOrWhiteSpace(stream?.UserId)) continue;
                _streams[stream.UserId] = stream!;
            }
            OnDataChanged();
        }

        public void RemoveUser(string login)
        {
            var key = Normalize(login);
            _users.TryRemove(key, out _);
            OnDataChanged();
        }

        private void OnDataChanged() => DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
