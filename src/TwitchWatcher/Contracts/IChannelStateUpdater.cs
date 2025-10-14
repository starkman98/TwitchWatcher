using System;
using System.Collections.Generic;
using System.Text;
using TwitchWatcher.Models;

namespace TwitchWatcher.Core.Contracts
{
    public interface IChannelStateUpdater
    {
        void UpdateChannelState(string login, StreamState state);
    }
}
