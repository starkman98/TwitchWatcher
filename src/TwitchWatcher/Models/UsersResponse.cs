using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TwitchWatcher.Models
{
    public class UsersResponse
    {
        [JsonPropertyName("data")]
        public List<TwitchUser> Data { get; set; } = new();
    }
}
