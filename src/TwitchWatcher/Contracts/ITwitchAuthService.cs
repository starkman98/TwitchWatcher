using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchWatcher.Contracts
{
    public interface ITwitchAuthService
    {
        Task<string> GetTokenAsync(CancellationToken ct = default);
    }
}
