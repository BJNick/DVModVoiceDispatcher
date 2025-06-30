using Newtonsoft.Json;

namespace PiperSharp.Models
{
    public class VoiceLanguage
    {
        [JsonProperty("code")]
        public string Code { get; set; }
    
        [JsonProperty("family")]
        public string Family { get; set; }
    
        [JsonProperty("name_english")]
        public string Name { get; set; }
    }
}