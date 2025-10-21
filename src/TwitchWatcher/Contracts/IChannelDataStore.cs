using System;
using System.Collections.Generic;
using System.Text;
using TwitchWatcher.Models;

namespace TwitchWatcher.Core.Contracts
{
    public interface IChannelDataStore
    {
        event EventHandler? DataChanged;

        IReadOnlyDictionary<string, TwitchUser> GetUsersSnapshot();
        IReadOnlyDictionary<string, TwitchStream> GetStreamsSnapshot();

        bool TryGetUser(string login, out TwitchUser? user);
        bool TryGetStreamByUserId(string userId, out TwitchStream? stream);

        void SetUsers(IEnumerable<TwitchUser> users);
        void SetStreams(IEnumerable<TwitchStream> streams);
        void RemoveUser(string login);
    }
}
