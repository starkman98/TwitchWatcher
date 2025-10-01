using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchWatcher.Configuration
{
    public class AppOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public int PollIntervalSeconds { get; set; } = 30;
        public string ChromePath { get; set; } = string.Empty;
        public bool OpenInAppWindow { get; set; } = true;
    }
}
