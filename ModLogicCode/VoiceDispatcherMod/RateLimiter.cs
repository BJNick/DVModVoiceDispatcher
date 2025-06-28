using System.Collections.Generic;
using UnityEngine;

namespace VoiceDispatcherMod {
    public static class RateLimiter {
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
    }
}