using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TwitchWatcher.Models
{
    public class StreamsResponse
    {
        [JsonPropertyName("data")]
        public List<TwitchStream> Data { get; set; } = new();
    }
}
