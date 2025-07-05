using System.Collections.Generic;
using DV.Booklets;
using DV.Logic.Job;
using DV.ThingTypes;
using static VoiceDispatcherMod.VoicingUtils;

namespace VoiceDispatcherMod {
    public class CarHelper {
        public static string lastClickedCarId = string.Empty;

        public static void OnCarClicked(TrainCar car) {
            var job = JobsManager.Instance.GetJobOfCar(car.logicCar);
            if (job != null && lastClickedCarId != car.ID && JobsManager.Instance.currentJobs.Count > 0) {
                TerseCommentOnCarJob(job);
                lastClickedCarId = car.ID;
            } else {
                DetailedCommentOnCar(car, job);
                lastClickedCarId = "";
            }
        }

        public static void TerseCommentOnCarJob(Job job) {
            var lineBuilder = new List<string>();
            var isPartOfYourJob = JobsManager.Instance.currentJobs.Contains(job);
            if (isPartOfYourJob) {
                lineBuilder.Add(Randomizer.GetRandomLine("CarInJob", 1, 3));
            } else {
                lineBuilder.Add(Randomizer.GetRandomLine("CarNotInJob", 1, 3));
            }

            CommsRadioNarrator.PlayWithClick(lineBuilder);
        }

        public static void DetailedCommentOnCar(TrainCar car, Job job) {
            var lineBuilder = new List<string>();
            lineBuilder.AddRange(VoicedCarNumber(car.ID));

            switch (job?.jobType) {
                case null:
                    lineBuilder.Add("NotPartOfAnyOrder");
                    break;
                case JobType.ShuntingLoad:
                    lineBuilder.Add("WaitingForLoading");
                    break;
                case JobType.ShuntingUnload:
                    lineBuilder.Add("WaitingForUnloading");
                    break;
                case JobType.Transport:
                case JobType.EmptyHaul:
                    lineBuilder.Add("BoundFor");
                    lineBuilder.Add(GetYardName(JobHelper.ExtractSomeDestinationTrack(job)));
                    break;
                default:
                    lineBuilder.Add("PartOf");
                    lineBuilder.Add("JobType" + job.jobType);
                    break;
            }

            CommsRadioNarrator.PlayWithClick(lineBuilder);
        }
    }
}