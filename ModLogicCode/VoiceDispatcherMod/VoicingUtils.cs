using System;
using System.Collections.Generic;
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

        public static string[] SayNumberOfCars<T>(List<T> list) {
            return SayNumberOfCars(list.Count);
        }

        public static string[] SayNumberOfCars(int number) {
            if (number <= 12) {
                return new[] { number + "Cars" };
            }

            var numberParts = SayFullNumber(number).ToList();
            numberParts.Add("Cars");
            return numberParts.ToArray();
        }
        
        public static string[] SaySpeedLimit(int speedLimit) {
            switch (speedLimit) {
                
            }
            
            
            if (speedLimit <= 0) {
                return new[] { "No", "Speed", "Limit" };
            }

            var numberParts = SayFullNumber(speedLimit).ToList();
            numberParts.Add("Speed");
            numberParts.Add("Limit");
            return numberParts.ToArray();
        }

        public static string[] RoundedDown(int number) {
            return SayApproximateNumber(number);
        }

        public static string[] SayApproximateNumber(int number) {
            return SayFullNumber(RoundDown(number));
        }
        
        public static int RoundDown(int number) {
            if (number < 100) {
                return number;
            }
            var totalDigits = number.ToString().Length;
            var tenthPower = (int)Math.Pow(10, totalDigits - 2);
            return number / tenthPower * tenthPower;
        }

        public static string[] Exact(int number) {
            return SayFullNumber(number);
        }

        public static string[] SayFullNumber(int number) {
            if (number == 0) {
                return new[] { "0" };
            }

            var numberParts = new List<string>();
            if (number >= 1_000_000) {
                var subMillion = number / 1_000_000 % 1000;
                if (subMillion == 0) {
                    subMillion = 999; // Max possible value
                }

                numberParts.AddRange(SayTripleDigitNumber(subMillion));
                numberParts.Add("1000000");
            }

            if (number >= 1000) {
                var subThousand = number / 1000 % 1000;
                if (subThousand != 0) {
                    numberParts.AddRange(SayTripleDigitNumber(subThousand));
                    numberParts.Add("1000");
                }
            }

            if (number % 1000 > 0) {
                numberParts.AddRange(SayTripleDigitNumber(number % 1000));
            }

            return numberParts.ToArray();
        }

        public static string[] SayTripleDigitNumber(int number) {
            if (number < 100) {
                return SayDoubleDigitNumber(number);
            }

            var numberParts = new List<string>();
            numberParts.Add(NthDigit(number, 3).ToString());
            numberParts.Add("100");
            if (number % 100 > 0) {
                numberParts.AddRange(SayDoubleDigitNumber(number % 100));
            }

            return numberParts.ToArray();
        }

        public static string[] SayDoubleDigitNumber(int number) {
            if (number <= 20) {
                return new[] { number.ToString() };
            }

            if (number / 10 * 10 == number) {
                return new[] { number.ToString() };
            }

            return new[] { (NthDigit(number, 2) * 10).ToString(), NthDigit(number, 1).ToString() };
        }

        public static int NthDigit(int number, int n) {
            return number / (int)Math.Pow(10, n - 1) % 10;
        }
    }
}