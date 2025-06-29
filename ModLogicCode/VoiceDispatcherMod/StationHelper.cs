using System;
using System.Collections.Generic;
using System.Linq;
using DV.ThingTypes;
using UnityEngine;
using static VoiceDispatcherMod.VoicingUtils;

namespace VoiceDispatcherMod {
    public class StationHelper {
        public static StationController playerYard;
        public static StationController subYard;
        private static Vector3 lastPlayerPosition = Vector3.negativeInfinity;

        private const float OfficeRadiusSqrDistance = 100f;
        private const float RangeCheckSqrDistance = 2500f;
        private const float YardRadiusRatio = 0.7f; // smaller than job spawn radius to avoid false positives

        private static StationController playerEnteredOffice;

        public static event Action<StationController> OnYardEntered;
        public static event Action<StationController> OnYardExited;
        public static event Action<StationController> OnStationEntered;

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
                    OnStationEntered?.Invoke(yard);
                } else if (playerEnteredOffice == yard && !IsPlayerInOfficeRange(yard)) {
                    playerEnteredOffice = null;
                }
            }
        }

        public static void AddWelcomeToYardMessage(List<string> lineBuilder, StationController station) {
            lineBuilder.Add(Randomizer.GetRandomLine("EnteringYard", 1, 5));
            lineBuilder.Add(GetYardName(station.stationInfo));
            lineBuilder.Add("ShortSilence");
        }

        public static void AddWelcomeToStationMessage(List<string> lineBuilder, StationController station) {
            lineBuilder.Add(Randomizer.GetRandomLine("EnteringStation", 1, 5));
            lineBuilder.Add(GetYardName(station.stationInfo));
            lineBuilder.Add("ShortSilence");
        }

        public static void AddExitingYardMessage(List<string> lineBuilder, StationController station) {
            lineBuilder.Add(Randomizer.GetRandomLine("ExitingYard", 1, 5));
            lineBuilder.Add(GetYardName(station.stationInfo));
            lineBuilder.Add("ShortSilence");
        }

        public static void AddHighestPayingJob(List<string> lineBuilder, StationController station) {
            var allJobs = station.logicStation.availableJobs;
            var job = allJobs.OrderByDescending(it => it.initialWage).FirstOrDefault();
            if (job == null) {
                lineBuilder.Add("0");
                lineBuilder.Add("Orders");
                return;
            }

            lineBuilder.Add("HighestPayingJob");
            lineBuilder.Add("JobType" + job.jobType);

            if (job.jobType is JobType.Transport or JobType.EmptyHaul) {
                lineBuilder.Add("BoundFor");
                lineBuilder.Add(GetYardName(JobHelper.ExtractTransportDestinationTrack(job)));
            }

            lineBuilder.Add("ShortSilence");

            lineBuilder.Add("Over");
            int wage = Mathf.RoundToInt(job.initialWage);
            lineBuilder.AddRange(RoundedDown(wage));
            lineBuilder.Add("Dollars");
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
                    OnYardExited?.Invoke(playerYard);
                } else {
                    OnYardEntered?.Invoke(newYard);
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