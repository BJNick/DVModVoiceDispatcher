using System;
using System.Collections.Generic;
using System.Linq;
using DvMod.HeadsUpDisplay;

namespace VoiceDispatcherMod {
    public class SignHelper {
        private const int SpeedLimitQueryInterval = 3; 
        private const int SpeedLimitMargin = 10;
        private const float WarningTime = 10f; // seconds before the speed limit is reached to warn about speeding

        private static int _lastSpeedLimitRead = -1;
        
        public static void CheckSpeedLimits() {
            if (RateLimiter.CannotYetPlay("SpeedLimitCheck", SpeedLimitQueryInterval)) {
                return;
            }
            var speedLimits = FilterTrackEvents.QueryUpcomingSpeedLimits();
            Main.Logger.Log("Upcoming speed limits: " + string.Join(", ", speedLimits.Select(it => it.limit)));
            if (speedLimits.Count > 0 && GetCurrentSpeed() > 0) {
                var nextSpeedLimit = speedLimits.First();
                PlaySpeedLimitRead(nextSpeedLimit);

                if (GetCurrentSpeed() > nextSpeedLimit.limit + SpeedLimitMargin) {
                    if (TimeUntil(nextSpeedLimit.span) < WarningTime) {
                        PlaySpeedingWarning();
                    }
                }
            }

            if (GetCurrentSpeed() == 0) {
                // Reset the last speed limit read when the player stops
                _lastSpeedLimitRead = -1;
            }
        }

        private static float TimeUntil(double span) {
            var currentSpeed = GetCurrentSpeed();
            if (currentSpeed <= 0) {
                return float.MaxValue; // Cannot calculate time if speed is zero
            }
            return (float) span / currentSpeed;
        }
        
        private static float GetCurrentSpeed() {
            var playerSpeed = PlayerManager.LastLoco.GetAbsSpeed() * 3.6f; // Convert m/s to km/h
            Main.Logger.Log("Current speed: " + playerSpeed);
            return playerSpeed;
        }

        private static void PlaySpeedLimitRead(SpeedLimitEvent speedLimit) {
            if (_lastSpeedLimitRead == speedLimit.limit) {
                return; // Avoid repeating the same speed limit
            }
            _lastSpeedLimitRead = speedLimit.limit;
            
            var lineBuilder = new List<string>();
            lineBuilder.Add("SpeedLimit");
            lineBuilder.AddRange(VoicingUtils.Exact(speedLimit.limit));
            lineBuilder.Add("In");
            lineBuilder.AddRange(VoicingUtils.RoundedDown((int) Math.Round(speedLimit.span)));
            lineBuilder.Add("Meters");
            CommsRadioNarrator.PlayWithClick(lineBuilder);
        }
        
        private static void PlaySpeedingWarning() {
            if (RateLimiter.CannotYetPlay("SpeedingWarning", 5)) {
                return;
            }
            var lineBuilder = new List<string>();
            lineBuilder.Add(Randomizer.GetRandomLine("SpeedingWarning", 1, 5));
            CommsRadioNarrator.PlayWithClick(lineBuilder);
        }
    }
}