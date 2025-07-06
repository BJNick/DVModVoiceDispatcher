using System;
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
            return JsonLinesLoader.GetRandomAndReplace("track_name", replacements);
        }
        
        public static string MapToTrackName(this Track track) {
            return track.ID.MapToTrackName();
        }

        public static string MapToJobTypeName(this JobType jobType) {
            return JsonLinesLoader.MapType("job_type", jobType.ToString());
        }

        public static string MapToYardName(this TrackID trackId) {
            return JsonLinesLoader.MapType("yard_id", trackId?.yardId);
        }
        
        public static string MapToYardName(this Track track) {
            return track.ID.MapToYardName();
        }

        public static string MapToYardName(this StationInfo stationInfo) {
            return JsonLinesLoader.MapType("yard_id", stationInfo?.YardID);
        }

        public static string MapToCarCount<T>(this List<T> cars) {
            return JsonLinesLoader.GetRandomAndReplace("car_count",
                new() { { "count", cars.Count.ToString() } });
        }

        public static string MapToDigit(this int digit) {
            return JsonLinesLoader.MapType("digit", digit.ToString());
        }
        
        public static string MapToCarID(this TrainCar car) {
            if (!car || string.IsNullOrEmpty(car.ID)) {
                Main.Logger.Error("Cannot map car to ID: car is null or ID is empty.");
                return "Unknown car";
            }
            var digitPart = car.ID.Substring(car.ID.Length - 3, 3) ?? throw new ArgumentNullException("car.ID.Substring(car.ID.Length - 3, 3)");
            var letterPart = car.ID.Substring(0, car.ID.Length - 3);
            var carType = car.logicCar?.carType?.v1;
            var replacements = new Dictionary<string, string> {
                { "full_car_id", car.ID },
                { "letter_part", letterPart },
                { "digit_part", digitPart },
                { "first_digit", digitPart[0].ToString() },
                { "second_digit", digitPart[1].ToString() },
                { "third_digit", digitPart[2].ToString() },
                { "car_type_id", carType?.ToString() ?? "Unknown" },
                { "car_type_name", carType?.MapToCarTypeName() ?? "Unknown" }
            };
            return JsonLinesLoader.GetRandomAndReplace("car_id", replacements);
        }
        
        public static string MapToCarTypeName(this TrainCarType carType) {
            return JsonLinesLoader.MapType("train_car_type", carType.ToString());
        }
    }
}