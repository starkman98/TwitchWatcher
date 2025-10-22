using System;
using System.Collections.Generic;
using System.Text;
using TwitchWatcher.Models;

namespace TwitchWatcher.Contracts
{
    public interface ITwitchApi
    {
        Task<string> GetUserIdAsync(string login, CancellationToken ct = default);
        Task<bool> IsLiveAsync(string userId, CancellationToken ct = default);
        Task<Dictionary<string, TwitchUser>> GetUsersDataByLoginsAsync(IEnumerable<string> logins, CancellationToken ct = default);
        Task<Dictionary<string, TwitchStream>> GetStreamsByUserIdsAsync(IEnumerable<string> userIds, CancellationToken ct = default);
        Task<string> GetChannelTitleAsync(string login, CancellationToken ct = default);
    }
}
