using UnityEngine;
using UnityModManagerNet;

namespace VoiceDispatcherMod {
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Radio Volume", DrawType.Slider, Min = 1, Max = 10)]
        public int Volume = 10;
        
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
        }
    }
}