using System.Linq;
using DV.Logic.Job;

namespace VoiceDispatcherMod {
    public static class VoicingUtils {
        public static string[] VoicedTrackId(TrackID trackId) {
            // TODO: Some types have two letters, like "SP" for storage passenger track.
            return trackId != null
                ? SeparateIntoLetters(trackId.TrackPartOnly.Substring(0, 2))
                : new[] { "Unknown", "Track" };
        }

        public static string[] VoicedCarNumber(string carId) {
            if (carId == null) {
                return new[] { "Unknown", "Car" };
            }

            return SeparateIntoLetters(carId.Substring(carId.Length - 3, 3));
        }

        public static string GetTrackTypeLetter(TrackID trackId) {
            if (trackId == null) {
                return "M";
            }

            return trackId.FullID.Split('-').Last();
        }

        public static string GetYardName(TrackID trackId) {
            if (trackId == null) {
                return "Unknown Yard";
            }

            return "Yard" + trackId.yardId;
        }

        public static string GetYardName(StationInfo stationInfo) {
            if (stationInfo?.YardID == null) {
                return "Unknown Yard";
            }

            return "Yard" + stationInfo.YardID;
        }

        public static string[] SeparateIntoLetters(string text) {
            return text.ToCharArray().Select(c => c.ToString()).ToArray();
        }
    }
}