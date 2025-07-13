using System;
using System.Collections.Generic;
using System.Linq;
using DvMod.HeadsUpDisplay;
using UnityEngine;

namespace VoiceDispatcherMod {
    public class SignHelper {
        private const int SpeedLimitQueryInterval = 3;
        private const int SpeedLimitMargin = 5;
        private const float WarningTime = 10f; // seconds before the speed limit is reached to warn about speeding

        private const float DeralmentDelay = 5f;
        private const float DeralmentReset = 15f;
        private const float DeralmentCooldown = RateLimiter.Minute * 10;

        private const float MinimumSpeed = 10f; // Minimum speed to consider for speed limit checks

        private const float DistanceBetweenCloseSpeedLimits = 200f; // Distance to consider them as double speed limit
        private const float SkipDistanceBetweenSpeedLimits = 50; // Distance to skip a high speed limit if the next one is lower

        private static int _lastSpeedLimitRead = -1;
        private static float _lastDerailment = float.NegativeInfinity;

        public static void CheckSpeedLimits() {
            if (RateLimiter.CannotYetPlay("SpeedLimitCheck", SpeedLimitQueryInterval)) {
                return;
            }

            var allSigns = FilterTrackEvents.QueryUpcomingEventsInText();
            Main.Logger.Log("Events: " + string.Join("; ", allSigns.Select(it => (int)it.Item1 + " " + it.Item2)));

            var speedLimits = FilterTrackEvents.QueryUpcomingSpeedLimits(2);
            if (speedLimits.Count > 0 && GetCurrentSpeed() > MinimumSpeed && !IsPlayerDerailed()) {
                //Main.Logger.Log("Upcoming speed limits: " + string.Join(", ", speedLimits.Select(it => it.limit)));
                var nextSpeedLimit = speedLimits.First();
                var secondSpeedLimit = speedLimits.Skip(1).FirstOrDefault();
                var minimumLimit = nextSpeedLimit;

                if (secondSpeedLimit != null && nextSpeedLimit.limit != secondSpeedLimit.limit &&
                    secondSpeedLimit.span - nextSpeedLimit.span < DistanceBetweenCloseSpeedLimits &&
                    secondSpeedLimit.limit < nextSpeedLimit.limit) {
                    minimumLimit = secondSpeedLimit;
                    if (secondSpeedLimit.span - nextSpeedLimit.span < SkipDistanceBetweenSpeedLimits) {
                        PlaySpeedLimitRead(secondSpeedLimit);
                    } else {
                        PlayDoubleSpeedLimitRead(nextSpeedLimit, secondSpeedLimit);
                    }
                } else {
                    PlaySpeedLimitRead(nextSpeedLimit);
                }

                if (GetCurrentSpeed() > minimumLimit.limit + SpeedLimitMargin) {
                    if (TimeUntil(minimumLimit.span) < WarningTime) {
                        PlaySpeedingWarning(minimumLimit);
                    }
                }
            }

            if (GetCurrentSpeed() == 0) {
                // Reset the last speed limit read when the player stops
                _lastSpeedLimitRead = -1;
            }

            PlayDerailmentMessage();
        }

        private static float TimeUntil(double span) {
            var currentSpeed = GetCurrentSpeed();
            if (currentSpeed <= 0) {
                return float.MaxValue; // Cannot calculate time if speed is zero
            }

            return (float)span / currentSpeed;
        }

        private static float GetCurrentSpeed() {
            if (PlayerManager.LastLoco == null) {
                return 0f;
            }

            var playerSpeed = PlayerManager.LastLoco.GetAbsSpeed() * 3.6f; // Convert m/s to km/h
            //Main.Logger.Log("Current speed: " + playerSpeed);
            return playerSpeed;
        }

        private static bool IsPlayerDerailed() {
            return PlayerManager.LastLoco?.derailed ?? false;
        }

        private static void PlaySpeedLimitRead(SpeedLimitEvent speedLimit) {
            if (_lastSpeedLimitRead == speedLimit.limit) {
                return; // Avoid repeating the same speed limit
            }

            _lastSpeedLimitRead = speedLimit.limit;

            string line = JsonLinesLoader.GetRandomAndReplace("speed_limit_change", new() {
                { "speed_limit", speedLimit.limit.ToString() },
                { "distance", ((int)speedLimit.span).ToString() },
                { "distance_rounded", VoicingUtils.RoundDown((int)speedLimit.span).ToString() }
            });
            CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
        }

        private static void PlayDoubleSpeedLimitRead(SpeedLimitEvent speedLimit, SpeedLimitEvent speedLimit2) {
            if (_lastSpeedLimitRead == speedLimit2.limit) {
                return; // Avoid repeating the same speed limit
            }

            _lastSpeedLimitRead = speedLimit2.limit;

            string line = JsonLinesLoader.GetRandomAndReplace("speed_limit_change_2", new() {
                { "speed_limit", speedLimit.limit.ToString() },
                { "distance", ((int)speedLimit.span).ToString() },
                { "distance_rounded", VoicingUtils.RoundDown((int)speedLimit.span).ToString() },

                { "speed_limit_2", speedLimit2.limit.ToString() },
                { "distance_2", ((int)speedLimit2.span).ToString() },
                { "distance_rounded_2", VoicingUtils.RoundDown((int)speedLimit2.span).ToString() }
            });
            CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
        }

        private static void PlaySpeedingWarning(SpeedLimitEvent speedLimit) {
            if (RateLimiter.CannotYetPlay("SpeedingWarning", 5)) {
                return;
            }

            string line = JsonLinesLoader.GetRandomAndReplace("speeding_warning", new() {
                { "speed_limit", speedLimit.limit.ToString() },
            });
            CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
        }

        private static void PlayDerailmentMessage() {
            var derailTimeDiff = Time.time - _lastDerailment;

            if (IsPlayerDerailed() && derailTimeDiff > DeralmentCooldown) {
                _lastDerailment = Time.time;
                return;
            }

            if (!(derailTimeDiff > DeralmentDelay) || !(derailTimeDiff < DeralmentReset)) {
                return;
            }

            if (RateLimiter.CannotYetPlay("Derailment", 6)) {
                return;
            }

            var lineBuilder = new List<string>();
            lineBuilder.Add(Randomizer.GetRandomLine("Derailment", 1, 5));
            CommsRadioNarrator.PlayWithClick(LineChain.FromAssetBundleLines(lineBuilder));
        }
    }
}