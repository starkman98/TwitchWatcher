using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Diagnostics;

namespace TwitchWatcher.WPF.Infrastructure.Configuration
{
    public sealed class JsonWritableOptions<T> : IWritableOptions<T>
        where T : new()
    {
        private readonly IConfigurationRoot _configRoot;
        private readonly string _section;
        private readonly string _filePath;

        public JsonWritableOptions(IConfigurationRoot configRoot, string section, string filePath)
        {
            _configRoot = configRoot;
            _section = section;
            _filePath = filePath;
        }

        public T Value
        {
            get
            {
                var obj = new T();
                _configRoot.GetSection(_section).Bind(obj);
                return obj;
            }
        }

        public void Update(Action<T> apply)
        {
            var json = File.ReadAllText(_filePath);
            using var doc = JsonDocument.Parse(json);
            using var ms = new MemoryStream();
            using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });

            var current = Value;
            apply(current);

            writer.WriteStartObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.NameEquals(_section))
                {
                    writer.WritePropertyName(_section);
                    JsonSerializer.Serialize(writer, current, new JsonSerializerOptions { WriteIndented = true });
                }
                else
                {
                    prop.WriteTo(writer);
                }
            }

            if (!doc.RootElement.TryGetProperty(_section, out _))
            {
                writer.WritePropertyName(_section);
                JsonSerializer.Serialize(writer, current, new JsonSerializerOptions { WriteIndented = true });
            }

            writer.WriteEndObject();
            writer.Flush();

            File.WriteAllBytes(_filePath, ms.ToArray());

            _configRoot.Reload();
        }
    }
}
