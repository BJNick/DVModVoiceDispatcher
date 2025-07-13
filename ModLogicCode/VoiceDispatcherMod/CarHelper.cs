using System.Linq;
using DV.Booklets;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;

namespace VoiceDispatcherMod {
    public class CarHelper {
        public static string lastClickedCarId = string.Empty;

        public enum CarStepType {
            Load,
            Unload,
            Deliver,
            Store
        }

        public static void OnCarClicked(TrainCar car) {
            var job = JobsManager.Instance.GetJobOfCar(car.logicCar);

            if (job != null && lastClickedCarId != car.ID && JobsManager.Instance.currentJobs.Count > 0) {
                TerseCommentOnCarJob(car, job);
                lastClickedCarId = car.ID;
            } else {
                DetailedCommentOnCar(car, job);
                lastClickedCarId = "";
            }
        }

        public static void TerseCommentOnCarJob(TrainCar car, Job job) {
            var isPartOfYourJob = JobsManager.Instance.currentJobs.Contains(job);
            if (isPartOfYourJob) {
                CurrentCarStepComment(car, job);
                return;
            }
            string line = JsonLinesLoader.GetRandomAndReplace("car_not_in_job");
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

        public static void CurrentCarStepComment(TrainCar car, Job job) {
            TrackID destinationTrack;
            CarStepType carStepType;

            switch (job.jobType) {
                case JobType.ShuntingLoad: {
                    var data = JobDataExtractor.ExtractShuntingLoadJobData(new Job_data(job));
                    if (car.logicCar.CurrentCargoTypeInCar == CargoType.None) {
                        carStepType = CarStepType.Load;
                        destinationTrack = data.loadMachineTrack;
                    } else {
                        carStepType = CarStepType.Store;
                        destinationTrack = data.destinationTrack;
                    }

                    break;
                }
                case JobType.ShuntingUnload: {
                    var data = JobDataExtractor.ExtractShuntingUnloadJobData(new Job_data(job));
                    if (car.logicCar.CurrentCargoTypeInCar != CargoType.None) {
                        carStepType = CarStepType.Unload;
                        destinationTrack = data.unloadMachineTrack;
                    } else {
                        carStepType = CarStepType.Store;
                        destinationTrack = FindStorageTrackForCar(car, data);
                    }
                    break;
                }
                case JobType.Transport:
                case JobType.EmptyHaul: {
                    destinationTrack = JobHelper.ExtractSomeDestinationTrack(job);
                    carStepType = CarStepType.Deliver;
                    break;
                }
                default: {
                    var genericLine = JsonLinesLoader.GetRandomAndReplace("car_in_job");
                    CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(genericLine));
                    return;
                }
            }
            
            var line = JsonLinesLoader.GetRandomAndReplace("car_job_step", new() {
                { "car_id", car.MapToCarID() },
                { "car_type_loc_key", car.logicCar.carType.localizationKey },
                { "cargo_type_loc_key", car.logicCar.CurrentCargoTypeInCar.ToV2()?.localizationKeyShort ?? "Empty" },
                { "destination_yard_id", destinationTrack.yardId },
                { "destination_track_name", destinationTrack.MapToTrackName() },
                { "step_type", carStepType.ToString() }
            });
            CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
        }

        private static TrackID FindStorageTrackForCar(TrainCar car, ShuntingUnloadJobData jobData) {
            foreach (var entry in jobData.destinationTracksData) {
                if (entry.cars.Select(it => it.ID).Contains(car.ID)) {
                    return entry.track;
                }
            }

            Main.Logger.Error("Could not find storage track for car: " + car.ID);
            return null;
        }
    }
}