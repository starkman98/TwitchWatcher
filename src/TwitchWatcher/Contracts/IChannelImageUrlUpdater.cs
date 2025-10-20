using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchWatcher.Core.Contracts
{
    public interface IChannelImageUrlUpdater
    {
        void UpdateChannelImageUrl(string login, string imageUrl);
    }
}
