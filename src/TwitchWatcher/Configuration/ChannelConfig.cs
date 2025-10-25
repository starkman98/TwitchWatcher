using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Xml;
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

        private string _thumbnailUrl;
        public string ThumbnailUrl
        {
            get => _thumbnailUrl;
            set
            {
                if (_thumbnailUrl != value)
                {
                    _thumbnailUrl = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThumbnailUrl)));
                }
            }
        }

        private bool _isNotFound;
        public bool IsNotFound
        {
            get => _isNotFound;
            set
            {
                if (_isNotFound != value)
                {
                    _isNotFound = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNotFound)));
                }
            }
        }

        private string _gameName;
        public string GameName
        {
            get => _gameName;
            set
            {
                if (_gameName != value)
                {
                    _gameName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameName)));
                }
            }
        }

        public string _startedAt;
        public string StartedAt
        {
            get => _startedAt;
            set
            {
                if (_startedAt != value)
                {
                    _startedAt = value;
                    _startedAtDto = ParseStartedAt(_startedAt);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartedAt)));
                    RefreshUptime();
                }
            }
        }

        private DateTimeOffset? _startedAtDto;

        public TimeSpan UpTime
        {
            get
            {
                if (_startedAtDto is null) return TimeSpan.Zero;
                var now = DateTimeOffset.UtcNow;
                return now - _startedAtDto.Value;
            }
        }

        public string DisplayUpTime
        {
            get
            { 
                var timeSpan = UpTime;
                if (timeSpan <= TimeSpan.Zero) return string.Empty;

                return timeSpan.ToString(@"hh\:mm\:ss");
            }
        }

        public void RefreshUptime()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpTime)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayUpTime)));
        }

        private DateTimeOffset? ParseStartedAt(string startedAt)
        {
            if (string.IsNullOrWhiteSpace(startedAt)) return null;
            if (DateTimeOffset.TryParse(startedAt, out var dto))
                return dto.ToUniversalTime();
            if (DateTime.TryParse(startedAt, out var dt))
                return new DateTimeOffset(dt).ToUniversalTime();
            return null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
