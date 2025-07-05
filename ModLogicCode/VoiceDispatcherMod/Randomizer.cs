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

            int lastIndex = LastSelectedIndexMap.TryGetValue(id, out var value) ? value : -1;
            int generatedIndex = startIndex - 1;
            while (generatedIndex == lastIndex || generatedIndex < startIndex) {
                generatedIndex = Random.Range(startIndex, endIndex + 1);
            }

            LastSelectedIndexMap[id] = generatedIndex;
            return $"{id}{generatedIndex}";
        }
        
        public static string GetConsecutiveLine(string id, int startIndex, int endIndex) {
            if (startIndex == endIndex) {
                return $"{id}{startIndex}";
            }

            int lastIndex = LastSelectedIndexMap.TryGetValue(id, out var value) ? value : startIndex - 1;
            int generatedIndex = lastIndex + 1;
            if (generatedIndex > endIndex) {
                generatedIndex = startIndex;
            }

            LastSelectedIndexMap[id] = generatedIndex;
            return $"{id}{generatedIndex}";
        }
        
        public static string GetRandomLine(LineGroup lineGroup) {
            if (lineGroup == null || lineGroup.lines.Count == 0) {
                return string.Empty;
            }
            if (lineGroup.lines.Count == 1) {
                return lineGroup.lines[0];
            }

            int lastIndex = LastSelectedIndexMap.TryGetValue(lineGroup.description, out var value) ? value : -1;
            int generatedIndex = -1;
            while (generatedIndex == lastIndex || generatedIndex < 0) {
                generatedIndex = Random.Range(0, lineGroup.lines.Count);
            }

            LastSelectedIndexMap[lineGroup.description] = generatedIndex;
            return lineGroup.lines[generatedIndex];
        }
    }
}