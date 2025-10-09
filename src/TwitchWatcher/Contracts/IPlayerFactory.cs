using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchWatcher.Contracts
{
    public interface IPlayerFactory
    {
        IPlayerService Create(string login);
    }
}
