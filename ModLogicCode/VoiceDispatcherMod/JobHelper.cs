using System.Collections.Generic;
using System.Linq;
using DV.Booklets;
using DV.Logic.Job;
using DV.ThingTypes;
using static VoiceDispatcherMod.VoicingUtils;

namespace VoiceDispatcherMod {
    public static class JobHelper {
        public static void ReadFirstJobOverview() {
            if (JobsManager.Instance == null || JobsManager.Instance.currentJobs == null ||
                JobsManager.Instance.currentJobs.Count == 0) {
                Main.Logger.Error("No jobs available to read.");
                return;
            }

            var firstJob = JobsManager.Instance.currentJobs.FirstOrDefault();
            if (firstJob != null) {
                ReadJobOverview(firstJob);
            } else {
                Main.Logger.Error("No valid job found to read.");
            }
        }

        public static void ReadAllJobsOverview() {
            var jobs = JobsManager.Instance?.currentJobs ?? new List<Job>();
            if (jobs.Count == 1) {
                ReadJobOverview(jobs[0]);
                return;
            }

            var lineBuilder = new List<Line>();
            lineBuilder.AddRange(LineChain.SplitIntoChain(JsonLinesLoader.GetRandomAndReplace("you_have_n_orders", new() {
                { "count", jobs.Count.ToString() }
            })));

            for (var i = 0; i < jobs.Count; i++) {
                lineBuilder.AddRange(LineChain.SplitIntoChain(CreateJobSpecificLine(jobs[i])));
                if (i < jobs.Count - 1) {
                    lineBuilder.Add(new PauseLine(0.5f));
                }
            }

            Main.Logger.Log(string.Join(" ", lineBuilder));
            CommsRadioNarrator.PlayWithClick(lineBuilder);
        }

        public static void ReadJobOverview(Job job) {
            var line = CreateJobSpecificLine(job);
            Main.Logger.Log(line);
            CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
        }

        public static string CreateJobSpecificLine(Job job) {
            return job.jobType switch {
                JobType.ShuntingLoad => CreateShuntingLoadJobLine(job),
                JobType.ShuntingUnload => CreateShuntingUnloadJobLine(job),
                JobType.Transport => CreateTransportJobLine(job),
                JobType.EmptyHaul => CreateEmptyHaulJobLine(job),
                _ => CreateGenericJobLine(job)
            };
        }
        
        public static string CreateShuntingLoadJobLine(Job job) {
            if (job?.tasks == null || job.tasks.Count == 0) {
                Main.Logger.Error("Invalid job or no tasks found.");
                return "Error parsing job data.";
            }

            var jobInfo = JobDataExtractor.ExtractShuntingLoadJobData(new Job_data(job));
            Dictionary<string, string> replacements = new Dictionary<string, string> {
                { "job_type_name", job.jobType.MapToJobTypeName() },
                { "job_type_id", job.jobType.ToString() },
                { "yard_id", jobInfo.destinationTrack.yardId },
                { "yard_name", jobInfo.destinationTrack.MapToYardName() },
                { "number_of_pickups", jobInfo.startingTracksData.Count.ToString() },
                { "loading_track_name", jobInfo.loadMachineTrack.MapToTrackName() },
                { "destination_track_name", jobInfo.destinationTrack.MapToTrackName() },
                { "destination_car_count", jobInfo.allCarsToLoad.MapToCarCount() },
            };
            for (var i = 0; i < jobInfo.startingTracksData.Count; i++) {
                var trackData = jobInfo.startingTracksData[i];
                var trackName = trackData.track.MapToTrackName();
                var carCount = trackData.cars.MapToCarCount();
                replacements.Add($"pickup_{i + 1}_track_name", trackName);
                replacements.Add($"pickup_{i + 1}_car_count", carCount);
            }

            return JsonLinesLoader.GetRandomAndReplace("shunting_load_job_overview", replacements);
        }
        
        public static string CreateShuntingUnloadJobLine(Job job) {
            if (job?.tasks == null || job.tasks.Count == 0) {
                Main.Logger.Error("Invalid job or no tasks found.");
                return "Error parsing job data.";
            }

            var jobInfo = JobDataExtractor.ExtractShuntingUnloadJobData(new Job_data(job));
            Dictionary<string, string> replacements = new Dictionary<string, string> {
                { "job_type_name", job.jobType.MapToJobTypeName() },
                { "job_type_id", job.jobType.ToString() },
                { "yard_id", jobInfo.startingTrack.yardId },
                { "yard_name", jobInfo.startingTrack.MapToYardName() },
                { "number_of_dropoffs", jobInfo.destinationTracksData.Count.ToString() },
                { "unloading_track_name", jobInfo.unloadMachineTrack.MapToTrackName() },
                { "starting_track_name", jobInfo.startingTrack.MapToTrackName() },
                { "starting_car_count", jobInfo.allCarsToUnload.MapToCarCount() }
            };
            for (var i = 0; i < jobInfo.destinationTracksData.Count; i++) {
                var trackData = jobInfo.destinationTracksData[i];
                var trackName = trackData.track.MapToTrackName();
                var carCount = trackData.cars.MapToCarCount();
                replacements.Add($"dropoff_{i + 1}_track_name", trackName);
                replacements.Add($"dropoff_{i + 1}_car_count", carCount);
            }

            return JsonLinesLoader.GetRandomAndReplace("shunting_unload_job_overview", replacements);
        }

        public static string CreateTransportJobLine(Job job) {
            if (job?.tasks == null || job.tasks.Count == 0) {
                Main.Logger.Error("Invalid job or no tasks found.");
                return "Error parsing job data.";
            }

            var jobInfo = JobDataExtractor.ExtractTransportJobData(new Job_data(job));
            return CreateBasicTransportJobLine(job.jobType, jobInfo.transportingCars,
                jobInfo.startingTrack, jobInfo.destinationTrack);
        }

        public static string CreateEmptyHaulJobLine(Job job) {
            if (job == null || job.tasks == null || job.tasks.Count == 0) {
                Main.Logger.Error("Invalid job or no tasks found.");
                return "Error parsing job data.";
            }

            var jobInfo = JobDataExtractor.ExtractEmptyHaulJobData(new Job_data(job));
            return CreateBasicTransportJobLine(job.jobType, jobInfo.transportingCars,
                jobInfo.startingTrack, jobInfo.destinationTrack);
        }

        public static string CreateBasicTransportJobLine(JobType jobType,
            List<Car_data> transportingCars,
            TrackID startingTrack, TrackID destinationTrack) {
            Dictionary<string, string> replacements = new Dictionary<string, string> {
                { "job_type_name", jobType.MapToJobTypeName() },
                { "job_type_id", jobType.ToString() },
                { "car_count", transportingCars.MapToCarCount() },
                { "starting_track_name", startingTrack.MapToTrackName() },
                { "starting_yard_name", startingTrack.MapToYardName() },
                { "destination_track_name", destinationTrack.MapToTrackName() },
                { "destination_yard_name", destinationTrack.MapToYardName() }
            };
            return JsonLinesLoader.GetRandomAndReplace("transport_job_overview", replacements);
        }
        
        public static string CreateGenericJobLine(Job job) {
            if (job == null || job.tasks == null || job.tasks.Count == 0) {
                Main.Logger.Error("Invalid job or no tasks found.");
                return "Error parsing job data.";
            }

            var lineBuilder = new List<string>();
            lineBuilder.Add(job.jobType.MapToJobTypeName() + JsonLinesLoader.SentenceDelimiter());
            foreach (var task in job.tasks) {
                lineBuilder.Add(CreateGenericTaskLine(task));
            }

            return string.Join(" ", lineBuilder);
        }
        
        public static string CreateGenericTaskLine(Task task) {
            var taskData = task.GetTaskData();
            if (taskData.nestedTasks != null && taskData.nestedTasks.Count > 0) {
                var nestedLines = new List<string>();
                foreach (var nestedTask in taskData.nestedTasks) {
                    nestedLines.Add(CreateGenericTaskLine(nestedTask));
                }

                return string.Join(" ", nestedLines);
            }

            var start = taskData.startTrack;
            var end = taskData.destinationTrack;

            Dictionary<string, string> replacements = new Dictionary<string, string> {
                { "warehouse_task_type", taskData.warehouseTaskType.ToString() },
                { "car_count", taskData.cars?.MapToCarCount() ?? "" },
                { "starting_track_name", start?.MapToTrackName() ?? "" },
                { "starting_yard_name", start?.MapToYardName() ?? "" },
                { "destination_track_name", end?.MapToTrackName() ?? "" },
                { "destination_yard_name", end?.MapToYardName() ?? "" },
                { "warehouse_track_name", end?.MapToTrackName() ?? "" },
                { "warehouse_yard_name", end?.MapToYardName() ?? "" }
            };

            return JsonLinesLoader.GetRandomAndReplace("generic_job_task", replacements);
        }

        public static TrackID ExtractSomeDestinationTrack(Job job) {
            var jobData = new Job_data(job);
            return job.jobType switch {
                JobType.ShuntingLoad => JobDataExtractor.ExtractShuntingLoadJobData(jobData).loadMachineTrack,
                JobType.ShuntingUnload => JobDataExtractor.ExtractShuntingUnloadJobData(jobData).unloadMachineTrack,
                JobType.Transport => JobDataExtractor.ExtractTransportJobData(jobData).destinationTrack,
                JobType.EmptyHaul => JobDataExtractor.ExtractEmptyHaulJobData(jobData).destinationTrack,
                _ => new TrackID("Unknown", "Unknown Destination", "", "Unknown")
            };
        }
    }
}