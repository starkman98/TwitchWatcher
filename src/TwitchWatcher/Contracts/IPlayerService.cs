using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchWatcher.Contracts
{
    public interface IPlayerService
    {
        Task OpenAsync(Uri url, CancellationToken ct);
        Task CloseAsync(CancellationToken ct);
    }
}
