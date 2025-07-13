using UnityEngine;
using UnityModManagerNet;

namespace VoiceDispatcherMod {
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Radio Volume", DrawType.Slider, Min = 1, Max = 10)]
        public int Volume = 10;

        [Draw("TTS Model Name")]
        public string Model = "en_US-joe-medium";
        
        [Draw("Job Helper (current order read, completion)")]
        public bool EnableJobHelper = true;
        
        [Draw("Sign Helper (speed limit signs read, speeding warnings, derailment checks)")]
        public bool EnableSignHelper = true;
        
        [Draw("Station Helper (station arrival, highest jobs read)")]
        public bool EnableStationHelper = true;
        
        [Draw("Car Helper (point radio to car to describe it, its job)")]
        public bool EnableCarHelper = true;
        
        [Draw("Debug Keystrokes (L, P, O, ;)")]
        public bool EnableDebugKeys = false;
        
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
        }
    }
}