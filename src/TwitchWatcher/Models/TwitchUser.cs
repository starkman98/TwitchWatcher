using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TwitchWatcher.Models
{
    public class TwitchUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [JsonPropertyName("profile_image_url")]
        public string ProfileImageUrl { get; set; } = string.Empty;
    }
}
