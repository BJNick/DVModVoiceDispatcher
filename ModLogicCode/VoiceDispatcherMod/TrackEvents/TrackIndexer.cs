using DV.PointSet;
using DV.Signs;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using DV.Simulation.Cars;
using UnityEngine;
using VoiceDispatcherMod;

/*
 * This code is from DV Heads Up Display mod at https://github.com/mspielberg/dv-hud/tree/master
 */
namespace DvMod.HeadsUpDisplay
{
    public static class TrackIndexer
    {
        internal const int SIGN_COLLIDER_LAYER = 30;
        private const float SIMPLIFIED_RESOLUTION = 30f;

        private static readonly Dictionary<RailTrack, List<TrackEvent>> indexedTracks =
            new Dictionary<RailTrack, List<TrackEvent>>();

        public static IEnumerable<TrackEvent> GetTrackEvents(RailTrack track)
        {
            if (!indexedTracks.TryGetValue(track, out var data))
            {
                data = indexedTracks[track] = GenerateTrackEvents(track).ToList();
            }
            return data;
        }

        public static IEnumerable<TrackEvent> GetTrackEvents(RailTrack track, bool first, double start)
        {
            var allTrackEvents = GetTrackEvents(track);
            return allTrackEvents.RelativeFromSpan(start, first);
        }

        private static IEnumerable<TrackEvent> ParseSign(string colliderName, bool direction, double span)
        {
            string[] parts = colliderName.Split('\n');
            switch (parts.Length)
            {
                case 1:
                    if (int.TryParse(parts[0], out var limit))
                        yield return new SpeedLimitEvent(span, direction, limit * 10);
                    break;

                case 2:
                    if (int.TryParse(parts[0], out var top))
                    {
                        if (parts[1][0] == '+' || parts[1][0] == '-')
                        {
                            yield return new SpeedLimitEvent(span, direction, top * 10);
                            yield return new GradeEvent(span, direction, float.Parse(parts[1]));
                        }
                        else if (int.TryParse(parts[1], out var bottom))
                        {
                            yield return new DualSpeedLimitEvent(span, direction, top * 10, bottom * 10);
                        }
                    }
                    else if (int.TryParse(parts[1], out var bottom))
                    {
                        yield return new SpeedLimitEvent(span, direction, bottom * 10);
                    }
                    else
                    {
                        Main.Logger.Warning($"Unable to parse sign: \"{colliderName.Replace("\n", "\\n")}\"");
                    }
                    break;
            }
        }

        public static float Grade(EquiPointSet.Point point)
        {
            return Mathf.RoundToInt(point.forward.y * 200) / 2f;
        }

        private static IEnumerable<TrackEvent> FindSigns(EquiPointSet.Point point)
        {
            var hits = Physics.RaycastAll(
                new Ray((Vector3)point.position + WorldMover.currentMove, point.forward),
                (float)point.spanToNextPoint,
                1 << SIGN_COLLIDER_LAYER, 
                QueryTriggerInteraction.Collide);
            
            /*Vector3 start = (Vector3)point.position + WorldMover.currentMove;
            Vector3 end = start + point.forward * (float)point.spanToNextPoint;
            VisualizeRay(start, end);*/
            
            foreach (var hit in hits)
            {
                var dp = Vector3.Dot(hit.collider.transform.forward, point.forward);
                bool direction = dp < 0f;
                foreach (var trackEvent in ParseSign(hit.collider.name, direction, point.span + hit.distance)) {
                    //Main.Logger.Warning($"Found a sign with name {hit.collider.name} with transform {hit.collider.transform.position}");
                    yield return trackEvent;
                }
            }
        }

        private static IEnumerable<TrackEvent> GenerateTrackEvents(RailTrack track)
        {
            Main.Logger.Log("Generating track events for " + track.name + " at " + track.transform.position);
            var pointSet = track.GetKinkedPointSet();
            EquiPointSet simplified = EquiPointSet.ResampleEquidistant(
                pointSet,
                Mathf.Min(SIMPLIFIED_RESOLUTION, (float)pointSet.span / 15));
            
            Main.Logger.Log("Simplified count: " + simplified.points.Length + " vs original " + pointSet.points.Length);
            
            foreach (var point in simplified.points)
            {
                foreach (var trackEvent in FindSigns(point))
                    yield return trackEvent;
            }
        }

        [HarmonyPatch(typeof(Streamer), nameof(Streamer.AddSceneGO))]
        public static class AddSceneGOPatch
        {
            public static void Postfix(GameObject sceneGO)
            {
                var signDebugs = sceneGO.GetComponentsInChildren<SignDebug>();
                bool foundSigns = false;
                foreach (var signDebug in signDebugs)
                {
                    signDebug.gameObject.layer = SIGN_COLLIDER_LAYER;
                    var collider = signDebug.gameObject.AddComponent<SphereCollider>();
                    collider.name = signDebug.text;
                    collider.center = new Vector3(2f, 0f, 0f);
                    collider.radius = 3f;
                    collider.isTrigger = true;

                    foundSigns = true;
                }
                if (foundSigns)
                    indexedTracks.Clear();
            }
        }
        
        private static void VisualizeRay(Vector3 start, Vector3 end)
        {
            // Create a cube primitive
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // Disable collider if you don't want it interfering with physics
            Collider col = cube.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            // Compute direction and length
            Vector3 direction = end - start;
            float length = direction.magnitude;
            // Position: midpoint of the ray
            cube.transform.position = start + direction / 2f;
            // Rotation: look along the direction
            cube.transform.rotation = Quaternion.LookRotation(direction);
            // Scale: thin cube stretched along Z axis to match ray length
            cube.transform.localScale = new Vector3(0.05f, 0.05f, length);
            // Optional: color it red
            cube.GetComponent<Renderer>().material.color = Color.red;
        }
    }
}