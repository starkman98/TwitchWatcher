using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchWatcher.Contracts
{
    public interface ITwitchApi
    {
        Task<string> GetUserIdAsync(string login, CancellationToken ct);
        Task<bool> CloseAsync(CancellationToken ct);
    }
}
