using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchWatcher.Contracts
{
    public interface ITwitchApi
    {
        Task<string> GetUserIdAsync(string login, CancellationToken ct = default);
        Task<bool> IsLiveAsync(string userId, CancellationToken ct = default);
        Task<string> GetChannelTitleAsync(string login, CancellationToken ct = default);
        Task<string> GetChannelImageUrlAsync(string login, CancellationToken ct = default);
    }
}
