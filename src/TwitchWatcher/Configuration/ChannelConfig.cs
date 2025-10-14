using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using TwitchWatcher.Models;

namespace TwitchWatcher.Configuration
{
    public class ChannelConfig : INotifyPropertyChanged
    {
        public string Login { get; set; } = "";

        private StreamState _state;
        public StreamState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
