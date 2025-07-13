using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static VoiceDispatcherMod.VoicingUtils;

namespace VoiceDispatcherMod {
    public class StationHelper {
        public static StationController playerYard;
        public static StationController subYard;
        private static Vector3 lastPlayerPosition = Vector3.negativeInfinity;

        private const float OfficeRadiusSqrDistance = 25f;
        private const float RangeCheckSqrDistance = 2500f;
        private const float YardRadiusRatio = 0.7f; // smaller than job spawn radius to avoid false positives

        private static StationController playerEnteredOffice;

        public static void OnUpdate() {
            if (StationController.allStations == null || !PlayerManager.PlayerTransform) {
                return;
            }

            // Update station and issue events
            GetPlayerStation();
            CheckEnteredOffice(playerYard);
            CheckEnteredOffice(subYard);
        }

        private static void CheckEnteredOffice(StationController yard) {
            if (yard) {
                if (!playerEnteredOffice && IsPlayerInOfficeRange(yard)) {
                    playerEnteredOffice = yard;
                    OnStationEntered(yard);
                } else if (playerEnteredOffice == yard && !IsPlayerInOfficeRange(yard)) {
                    playerEnteredOffice = null;
                }
            }
        }


        static void OnYardEntered(StationController station) {
            if (RateLimiter.CannotYetPlay("YardEnterOrExit" + station.stationInfo.YardID,
                    RateLimiter.Minutes(3))) {
                return;
            }

            var line = CreateWelcomeToYardLine(station);
            Main.Logger.Log(line);
            CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
        }

        static void OnYardExited(StationController previousStation) {
            if (RateLimiter.CannotYetPlay("YardEnterOrExit" + previousStation.stationInfo.YardID,
                    RateLimiter.Minutes(3))) {
                return;
            }

            var line = CreateExitingYardLine(previousStation);
            Main.Logger.Log(line);
            CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
        }

        static void OnStationEntered(StationController station) {
            if (RateLimiter.CannotYetPlay("StationWelcome" + station.stationInfo.YardID, RateLimiter.Minutes(2))) {
                return;
            }

            var line = CreateWelcomeToStationOfficeLine(station);
            if (!RateLimiter.CannotYetPlay("AutoHighestJobRead" + station.stationInfo.YardID,
                    RateLimiter.Minutes(10))) {
                line += " " + CreateHighestPayingJobLine(station);
            }

            Main.Logger.Log(line);
            CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
        }

        public static string CreateWelcomeToStationOfficeLine(StationController station) {
            if (station == null || station.stationInfo == null) {
                Main.Logger.Error("Cannot create welcome line for station: station or stationInfo is null.");
                return string.Empty;
            }

            return JsonLinesLoader.GetRandomAndReplace("station_office_entry", new() {
                { "yard_name", station.stationInfo.MapToYardName() },
                { "yard_id", station.stationInfo.YardID }
            });
        }

        public static string CreateWelcomeToYardLine(StationController station) {
            if (station == null || station.stationInfo == null) {
                Main.Logger.Error("Cannot create welcome line for yard: station or stationInfo is null.");
                return string.Empty;
            }

            return JsonLinesLoader.GetRandomAndReplace("yard_entry", new() {
                { "yard_name", station.stationInfo.MapToYardName() },
                { "yard_id", station.stationInfo.YardID }
            });
        }

        public static string CreateExitingYardLine(StationController station) {
            if (station == null || station.stationInfo == null) {
                Main.Logger.Error("Cannot create exiting line for yard: station or stationInfo is null.");
                return string.Empty;
            }

            return JsonLinesLoader.GetRandomAndReplace("yard_exit", new() {
                { "yard_name", station.stationInfo.MapToYardName() },
                { "yard_id", station.stationInfo.YardID }
            });
        }

        public static string CreateHighestPayingJobLine(StationController station) {
            if (!station || station.logicStation?.availableJobs == null) {
                Main.Logger.Error("Cannot create highest paying job line: station or its jobs are null.");
                return string.Empty;
            }

            var allJobs = station.logicStation.availableJobs;
            var job = allJobs.OrderByDescending(it => it.initialWage).FirstOrDefault();
            if (job == null) {
                Main.Logger.Error("No jobs available to create highest paying job line.");
                return String.Empty;
            }

            var replacements = new Dictionary<string, string> {
                { "job_type", job.jobType.MapToJobTypeName() },
                { "job_type_id", job.jobType.ToString() },
                { "destination_yard_name", JobHelper.ExtractSomeDestinationTrack(job).MapToYardName() },
                { "destination_track_name", JobHelper.ExtractSomeDestinationTrack(job).MapToTrackName() },
                { "exact_payout", Mathf.RoundToInt(job.initialWage).ToString() },
                { "rounded_payout", RoundDown(Mathf.RoundToInt(job.initialWage)).ToString() }
            };
            return JsonLinesLoader.GetRandomAndReplace("highest_paying_job", replacements);
        }

        public static StationController GetPlayerStation() {
            if (CheckPlayerPositionAgain()) {
                ForceUpdatePlayerStation();
            }

            return playerYard;
        }

        private static bool CheckPlayerPositionAgain() {
            var checkAgain = (PlayerManager.PlayerTransform.position - lastPlayerPosition).sqrMagnitude >
                             RangeCheckSqrDistance;
            if (checkAgain) {
                lastPlayerPosition = PlayerManager.PlayerTransform.position;
            }

            return checkAgain;
        }

        public static void ForceUpdatePlayerStation() {
            var newYard = FindYardInRange();
            if (newYard != playerYard) {
                subYard = FindSubYardInRange();

                if (!newYard) {
                    OnYardExited(playerYard);
                } else {
                    OnYardEntered(newYard);
                }

                playerYard = newYard;
            }
        }

        public static StationController FindYardInRange() {
            foreach (var station in StationController.allStations) {
                if (IsPlayerInYardRange(station) && !IsMilitarySubStation(station)) {
                    return station;
                }
            }

            return null;
        }

        public static StationController FindSubYardInRange() {
            foreach (var station in StationController.allStations) {
                if (IsPlayerInYardRange(station) && IsMilitarySubStation(station)) {
                    return station;
                }
            }

            return null;
        }

        public static bool IsMilitarySubStation(StationController station) {
            if (station == null || station.stationRange == null) {
                return false;
            }

            var yardID = station.stationInfo.YardID;
            return yardID != "MB" && yardID.EndsWith("MB");
        }


        public static bool IsPlayerInOfficeRange(StationController station) {
            if (station == null || station.stationRange == null) {
                return false;
            }

            return (station.stationRange.PlayerSqrDistanceFromStationOffice <= OfficeRadiusSqrDistance);
        }

        public static bool IsPlayerInYardRange(StationController station) {
            if (station == null || station.stationRange == null) {
                return false;
            }

            return station.stationRange.PlayerSqrDistanceFromStationCenter <=
                   station.stationRange.generateJobsSqrDistance * YardRadiusRatio * YardRadiusRatio;
        }
    }
}