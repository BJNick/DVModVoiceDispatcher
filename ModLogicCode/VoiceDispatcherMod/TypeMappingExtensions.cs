using System.Collections.Generic;
using DV.Logic.Job;
using DV.ThingTypes;

namespace VoiceDispatcherMod {
    public static class TypeMappingExtensions {
        public static string MapToTrackName(this TrackID trackId) {
            var trimmedOrderNumber = trackId.orderNumber?.StartsWith("0") == true
                ? trackId.orderNumber.Substring(1)
                : trackId.orderNumber;
            var replacements = new Dictionary<string, string> {
                { "yard_id", trackId.yardId },
                { "sub_yard_id", trackId.subYardId },
                { "order_number", trimmedOrderNumber },
                { "track_type", trackId.trackType }
            };
            Main.Logger.Warning(replacements.ToString());
            return JsonLinesLoader.GetRandomAndReplace("track_name", replacements);
        }

        public static string MapToJobTypeName(this JobType jobType) {
            return JsonLinesLoader.MapType("job_type", jobType.ToString());
        }

        public static string MapToYardName(this TrackID trackId) {
            return JsonLinesLoader.MapType("yard_id", trackId.yardId);
        }

        public static string MapToYardName(this StationInfo stationInfo) {
            return JsonLinesLoader.MapType("yard_id", stationInfo.YardID);
        }

        public static string MapToCarCount<T>(this List<T> cars) {
            return JsonLinesLoader.GetRandomAndReplace("car_count",
                new() { { "count", cars.Count.ToString() } });
        }
    }
}