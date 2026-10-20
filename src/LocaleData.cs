using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KeyTips
{
    public class LocaleData
    {
        [JsonPropertyName("Language")]
        public string Language { get; set; }

        [JsonPropertyName("Translate")]
        public Dictionary<string, string> Translate { get; set; }
    }
}
