using System.Collections.Generic;
using System.Linq;
using DV.Simulation.Cars;
using UnityEngine;
using VoiceDispatcherMod;

/*
 * This code is from DV Heads Up Display mod at https://github.com/mspielberg/dv-hud/tree/master
 */
namespace DvMod.HeadsUpDisplay {
    public static class FilterTrackEvents {
        private const int MaxEventCount = 5;
        private const int MaxEventSpan = 500;
        
        public static List<TrackEvent> QueryUpcomingEvents()
        {
            if (FollowCurrentTrack(MaxEventSpan, out var currentGrade, out var events)) return new List<TrackEvent>();

            var eventDescriptions = events
                .ExceptUnnamedTracks()
                .ResolveJunctionSpeedLimits()
                .FilterRedundantSpeedLimits()
                .FilterGradeEvents(currentGrade)
                .Take(MaxEventCount)
                .TakeWhile(ev => ev.span < MaxEventSpan)
                /*.Select(ev => ev switch
                    {
                        TrackChangeEvent e => (e.span, e.ID.ToString()),
                        JunctionEvent e => (e.span, GetJunctionEventDescription(e)),
                        DualSpeedLimitEvent e => (e.span, $"{e.limit} / {e.rightLimit} km/h"),
                        SpeedLimitEvent e => (e.span, GetSpeedLimitEventDescription(e)),
                        GradeEvent e => (e.span, $"{e.grade:F1} %"),
                        _ => (0.0, $"Unknown event: {ev}"),
                    })*/
                .ToList();

            return eventDescriptions;
        }

        public static List<SpeedLimitEvent> QueryUpcomingSpeedLimits() {
            if (FollowCurrentTrack(MaxEventSpan, out var currentGrade, out var events)) {
                return new List<SpeedLimitEvent>();
            }

            return events
                .ExceptUnnamedTracks()
                .ResolveJunctionSpeedLimits()
                .FilterRedundantSpeedLimits()
                .OfType<SpeedLimitEvent>()
                .Take(3)
                .ToList();
        }

        private static bool FollowCurrentTrack(int maxEventSpan, out float currentGrade, out IEnumerable<TrackEvent> events) {
            currentGrade = 0f;
            events = Enumerable.Empty<TrackEvent>();
            
            if (!PlayerManager.Car)
                return true;
            var bogie = PlayerManager.Car.Bogies[1];
            var track = bogie.track;
            if (track == null)
                return true;
            var startSpan = bogie.traveller.Span;
            // var locoDirection = PlayerManager.LastLoco == null || PlayerManager.LastLoco.GetComponent<LocoControllerBase>()?.reverser >= 0f;
            Main.Logger.Log("Reverser: " + PlayerManager.LastLoco.GetComponent<SimController>()?.controlsOverrider?.Reverser?.Value);
            var locoDirection = PlayerManager.LastLoco == null || PlayerManager.LastLoco.GetComponent<SimController>()?.controlsOverrider?.Reverser?.Value >= 0.5f;
            var direction = !locoDirection ^ (bogie.trackDirection > 0);
            currentGrade = TrackIndexer.Grade(bogie.point1) * (direction ? 1 : -1);

            events = TrackFollower.FollowTrack(
                track,
                startSpan,
                direction ? maxEventSpan : -maxEventSpan);
            return false;
        }
    }
}