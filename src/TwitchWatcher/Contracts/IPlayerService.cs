using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchWatcher.Contracts
{
    public interface IPlayerService
    {
        Task OpenAsync(Uri url, CancellationToken ct = default);
        Task CloseAsync(CancellationToken ct = default);
        bool IsOpen { get; }
    }
}
