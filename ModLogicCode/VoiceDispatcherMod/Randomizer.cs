using System.Collections.Generic;
using UnityEngine;

namespace VoiceDispatcherMod {
    /** Never return the same line for the same id in a row. */
    public static class Randomizer {
        // Map of id to last selected index
        private static readonly Dictionary<string, int> LastSelectedIndexMap = new();

        public static string GetRandomLine(string id, int startIndex, int endIndex) {
            if (startIndex == endIndex) {
                return $"{id}{startIndex}";
            }

            int lastIndex = LastSelectedIndexMap.ContainsKey(id) ? LastSelectedIndexMap[id] : -1;
            int generatedIndex = startIndex - 1;
            while (generatedIndex == lastIndex || generatedIndex < startIndex) {
                generatedIndex = Random.Range(startIndex, endIndex + 1);
            }

            LastSelectedIndexMap[id] = generatedIndex;
            return $"{id}{generatedIndex}";
        }
    }
}