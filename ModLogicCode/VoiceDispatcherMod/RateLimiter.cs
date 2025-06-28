using System.Collections.Generic;
using UnityEngine;

namespace VoiceDispatcherMod {
    public static class RateLimiter {
        public const float Minute = 60f;
        public const float Hour = 3600f;

        private static readonly Dictionary<string, float> LastCallTimes = new();

        // Returns true if the minimum interval has passed since the last call for this id
        public static bool CannotYetPlay(string id, float minInterval) {
            float now = Time.time;
            if (!LastCallTimes.TryGetValue(id, out float lastTime) || now - lastTime >= minInterval) {
                LastCallTimes[id] = now;
                return false;
            }
            //Main.Logger.Warning($"Cannot yet play: {id}, passed only {now - lastTime}");
            return true;
        }
        
        public static float Minutes(float minutes) => minutes * Minute;
        public static float Hours(float hours) => hours * Hour;
    }
}