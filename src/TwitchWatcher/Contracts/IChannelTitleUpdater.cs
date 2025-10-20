using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchWatcher.Core.Contracts
{
    public interface IChannelTitleUpdater
    {
        void UpdateChannelTitle(string login, string title);
    }
}
