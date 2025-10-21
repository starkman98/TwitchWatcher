using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using TwitchWatcher.Models;

namespace TwitchWatcher.Configuration
{
    public class ChannelConfig : INotifyPropertyChanged
    {
        public string Login { get; set; } = "";

        public string _displayName = string.Empty;
        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
                }
            }
        }

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

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
                }
            }
        }

        public string _imageUrl = string.Empty;
        public string ImageUrl
        {
            get => _imageUrl;
            set
            {
                if (_imageUrl != value)
                {
                    _imageUrl = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageUrl)));
                }
            }
        }

        public int _viewerCount;
        public int ViewerCount
        {
            get => _viewerCount;
            set
            {
                if (_viewerCount != value)
                {
                    _viewerCount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewerCount)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
