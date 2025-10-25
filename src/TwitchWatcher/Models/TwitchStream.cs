using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TwitchWatcher.Models
{
    public class TwitchStream
    {
        [JsonPropertyName("id")] public string Id { get; set; }

        [JsonPropertyName("user_id")] public string UserId { get; set; }

        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("title")] public string Title { get; set; }

        [JsonPropertyName("viewer_count")] public int ViewerCount { get; set; }

        [JsonPropertyName("thumbnail_url")] public string ThumbnailUrl { get; set; }
        [JsonPropertyName("game_name")] public string GameName { get; set; }
        [JsonPropertyName("started_at")] public string StartedAt { get; set; }

        public StreamState State { get; set; }

    }
}
