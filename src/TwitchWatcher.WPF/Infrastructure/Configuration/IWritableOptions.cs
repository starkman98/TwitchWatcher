using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchWatcher.WPF.Infrastructure.Configuration
{
    public interface IWritableOptions<out T>
    {
        T Value { get; }
        void Update(Action<T> apply);
    }
}
