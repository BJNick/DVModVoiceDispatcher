using DV.Logic.Job;
using DV.ThingTypes.TransitionHelpers;

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
            var isPartOfYourJob = JobsManager.Instance.currentJobs.Contains(job);
            string line = JsonLinesLoader.GetRandomAndReplace(isPartOfYourJob ? "car_in_job" : "car_not_in_job");
            CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
        }

        public static string CreateCarIdLine(TrainCar car) {
            if (car == null || string.IsNullOrEmpty(car.ID)) {
                Main.Logger.Error("Cannot create car ID line: car is null or ID is empty.");
                return "UnknownCar";
            }

            return car.MapToCarID();
        }

        public static void DetailedCommentOnCar(TrainCar car, Job job) {
            var cargoLocKey = car.logicCar.CurrentCargoTypeInCar.ToV2()?.localizationKeyShort ?? "Empty";

            string line = JsonLinesLoader.GetRandomAndReplace("car_description", new() {
                { "car_id", car.MapToCarID() },
                { "car_type_loc_key", car.logicCar.carType.localizationKey },
                { "cargo_type_loc_key", cargoLocKey },
                { "destination_yard_id", job != null ? JobHelper.ExtractSomeDestinationTrack(job).yardId : "Unknown" }, {
                    "destination_track_name",
                    job != null ? JobHelper.ExtractSomeDestinationTrack(job).MapToTrackName() : "Unknown"
                },
                { "job_type_id", job != null ? job.jobType.ToString() : "No Job" }
            });
            Main.Logger.Log(line);
            CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
        }
    }
}