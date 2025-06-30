using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace PiperSharp.Models
{
    public class VoiceModel
    {
        [JsonProperty("key")]
        public string Key { get; set; }
    
        [JsonProperty("name")]
        public string Name { get; set; }
    
        [JsonProperty("num_speakers")]
        public int NumSpeakers { get; set; }
    
        [JsonProperty("speaker_id_map")]
        public Dictionary<string, int> SpeakerIdMap { get; set; }
    
        [JsonProperty("files")]
        public Dictionary<string, dynamic> Files { get; set; }
    
        [JsonProperty("language")]
        public VoiceLanguage Language { get; set; }
    
        [JsonProperty("audio")]  
        public VoiceAudio? Audio { get; set; }
    
        [JsonIgnore]
        public string? ModelLocation { get; set; }
    
        public string GetModelLocation()
        {
            if (ModelLocation is null) throw new FileNotFoundException("Model not downloaded!");
            var modelFileName = Path.GetFileName(Files.Keys.FirstOrDefault(f => f.EndsWith(".onnx")));
            return Path.Combine(ModelLocation, modelFileName).AddPathQuotesIfRequired();
        }


        public static Task<VoiceModel> LoadModelByKey(string modelKey) 
            => LoadModel(Path.Combine(PiperDownloader.DefaultModelLocation, modelKey));
        public static async Task<VoiceModel> LoadModel(string directory)
        {
            if (!Directory.Exists(directory)) throw new DirectoryNotFoundException("Model directory not found!");
            var modelInfoFile = Path.Combine(directory, "model.json");
            if (!File.Exists(modelInfoFile)) throw new FileNotFoundException("model.json file not found!");
            var json = File.ReadAllText(modelInfoFile);
            var model = JsonConvert.DeserializeObject<VoiceModel>(json);
            if (model is null) throw new ApplicationException("Could not parse model.json file!");
            model.ModelLocation = directory;
            return model;
        }
    }
}