using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoiceDispatcherMod {
    public class StationHelper {
        public static StationController playerStation;
        private static Vector3 lastPlayerPosition = Vector3.negativeInfinity;

        // TODO: Adjust distances
        private const float YardRadiusSqrDistance = 1000f;
        private const float OfficeRadiusSqrDistance = 25f;
        private const float RangeCheckSqrDistance = 25f;

        private static bool playerEnteredOffice = false;

        public static event Action<StationController> OnYardEntered;
        public static event Action<StationController> OnYardExited;
        public static event Action<StationController> OnStationEntered;

        public static void OnUpdate() {
            if (StationController.allStations == null || !PlayerManager.PlayerTransform) {
                return;
            }

            // Update station and issue events
            GetPlayerStation();
            CheckEnteredStation();
        }

        private static void CheckEnteredStation() {
            if (playerStation) {
                if (!playerEnteredOffice && IsPlayerInOfficeRange(playerStation)) {
                    playerEnteredOffice = true;
                    OnStationEntered?.Invoke(playerStation);
                } else if (playerEnteredOffice && !IsPlayerInOfficeRange(playerStation)) {
                    playerEnteredOffice = false;
                }
            } else {
                playerEnteredOffice = false;
            }
        }

        public static void AddWelcomeToYardMessage(List<string> lineBuilder, StationController station) {
            lineBuilder.Add(Randomizer.GetRandomLine("EnteringYard", 1, 5));
            lineBuilder.Add(VoicingUtils.GetYardName(station.stationInfo));
            lineBuilder.Add("ShortSilence");
        }

        public static void AddWelcomeToStationMessage(List<string> lineBuilder, StationController station) {
            lineBuilder.Add(Randomizer.GetRandomLine("EnteringStation", 1, 3));
            lineBuilder.Add(VoicingUtils.GetYardName(station.stationInfo));
            lineBuilder.Add("ShortSilence");
        }

        public static void AddExitingYardMessage(List<string> lineBuilder, StationController station) {
            lineBuilder.Add(Randomizer.GetRandomLine("ExitingYard", 1, 4));
            lineBuilder.Add(VoicingUtils.GetYardName(station.stationInfo));
            lineBuilder.Add("ShortSilence");
        }

        public static StationController GetPlayerStation() {
            if (CheckPlayerPositionAgain()) {
                if (playerStation) {
                    if (!IsPlayerInYardRange(playerStation)) {
                        ForceUpdatePlayerStation();
                    }
                } else {
                    ForceUpdatePlayerStation();
                }
            }
            return playerStation;
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
            var newStation = GetYardInRange();
            if (newStation != playerStation) {
                if (!newStation) {
                    OnYardExited?.Invoke(playerStation);
                } else {
                    OnYardEntered?.Invoke(newStation);
                }

                playerStation = newStation;
            }
        }

        public static StationController GetYardInRange() {
            foreach (var station in StationController.allStations) {
                if (IsPlayerInYardRange(station)) {
                    return station;
                }
            }

            return null;
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
            // TODO station center
            return (station.stationRange.PlayerSqrDistanceFromStationOffice <= YardRadiusSqrDistance);
        }
    }
}