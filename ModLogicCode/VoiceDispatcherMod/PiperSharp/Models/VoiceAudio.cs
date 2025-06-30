using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PiperSharp.Models
{
    public class VoiceAudio
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("quality")]
        public VoiceQuality Quality { get; set; }
    
        [JsonProperty("sample_rate")]
        public uint SampleRate { get; set; }
    }
}