using System.Collections.Generic;
using System.Linq;
using DV.Booklets;
using DV.Logic.Job;
using DV.ThingTypes;
using static VoiceDispatcherMod.VoicingUtils;

namespace VoiceDispatcherMod {
    public static class JobHelper {
        public static void AddGenericJobLines(List<string> lineBuilder, Job job) {
            lineBuilder.Add("YouHave");

            var typeLine = "JobType" + job.jobType;
            lineBuilder.Add(typeLine);

            foreach (var task in job.tasks) {
                AddTaskLines(task, lineBuilder);
            }
        }

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

            var lineBuilder = new List<string>();
            lineBuilder.Add("YouHave");
            lineBuilder.AddRange(Exact(jobs.Count));
            lineBuilder.Add("Orders");
            lineBuilder.Add("ShortSilence");

            foreach (var job in jobs) {
                AddJobSpecificLines(lineBuilder, job);
                lineBuilder.Add("ShortSilence");
                lineBuilder.Add("ShortSilence");
            }

            CommsRadioNarrator.PlayWithClick(lineBuilder);
        }

        public static void ReadJobOverview(Job job) {
            //var lineBuilder = new List<string>();
            //lineBuilder.Add("YouHave");
            //AddJobSpecificLines(lineBuilder, job);
            var line = CreateJobSpecificLineFromJson(job);
            Main.Logger.Log(line);
            CommsRadioNarrator.GenerateAndPlay(line);
        }

        public static void AddJobSpecificLines(List<string> lineBuilder, Job job) {
            if (job == null || job.tasks == null || job.tasks.Count == 0) {
                Main.Logger.Error("Invalid job or no tasks found.");
                return;
            }

            switch (job.jobType) {
                case JobType.ShuntingLoad:
                    AddShuntingLoadJobLines(lineBuilder, job);
                    break;
                case JobType.ShuntingUnload:
                    AddShuntingUnloadJobLines(lineBuilder, job);
                    break;
                case JobType.Transport:
                    AddTransportJobLines(lineBuilder, job);
                    break;
                case JobType.EmptyHaul:
                    AddEmptyHaulJobLines(lineBuilder, job);
                    break;
                default:
                    AddGenericJobLines(lineBuilder, job);
                    break;
            }
        }
        
        public static string CreateJobSpecificLineFromJson(Job job) {
            switch (job.jobType) {
                case JobType.ShuntingLoad:
                    return CreateShuntingLoadJobLineFromJson(job);
                case JobType.ShuntingUnload:
                    return CreateShuntingUnloadJobLineFromJson(job);
                case JobType.Transport:
                    return CreateTransportJobLineFromJson(job);
                case JobType.EmptyHaul:
                    return CreateEmptyHaulJobLineFromJson(job);
                default:
                    return "Error: Unsupported job type.";
            }
        }

        public static void AddShuntingLoadJobLines(List<string> lineBuilder, Job job) {
            if (job == null || job.tasks == null || job.tasks.Count == 0) {
                Main.Logger.Error("Invalid job or no tasks found.");
                return;
            }

            var jobInfo = JobDataExtractor.ExtractShuntingLoadJobData(new Job_data(job));

            lineBuilder.Add("JobType" + job.jobType);
            lineBuilder.Add("ShortSilence");

            lineBuilder.Add("Couple");

            for (var index = 0; index < jobInfo.startingTracksData.Count; index++) {
                var carDataPerTrackID = jobInfo.startingTracksData[index];
                var track = carDataPerTrackID.track;
                var carCount = carDataPerTrackID.cars.Count;
                if (index == jobInfo.startingTracksData.Count - 1) {
                    lineBuilder.Add("And");
                }

                lineBuilder.AddRange(SayNumberOfCars(carCount));
                lineBuilder.Add("AtTrack");
                lineBuilder.AddRange(VoicedTrackId(track));
                lineBuilder.Add("ShortSilence");
            }

            lineBuilder.Add("ThenMove");
            lineBuilder.AddRange(SayNumberOfCars(jobInfo.allCarsToLoad));
            lineBuilder.Add("ToTrack");
            lineBuilder.AddRange(VoicedTrackId(jobInfo.loadMachineTrack));
            lineBuilder.Add("ForLoading");
            lineBuilder.Add("ShortSilence");

            lineBuilder.Add("ThenUncouple");
            lineBuilder.Add("AtTrackType" + GetTrackTypeLetter(jobInfo.destinationTrack));
            lineBuilder.AddRange(VoicedTrackId(jobInfo.destinationTrack));
            lineBuilder.Add("ForDeparture");
        }

        public static string CreateShuntingLoadJobLineFromJson(Job job) {
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

        public static void AddShuntingUnloadJobLines(List<string> lineBuilder, Job job) {
            if (job?.tasks == null || job.tasks.Count == 0) {
                Main.Logger.Error("Invalid job or no tasks found.");
                return;
            }

            var jobInfo = JobDataExtractor.ExtractShuntingUnloadJobData(new Job_data(job));

            lineBuilder.Add("JobType" + job.jobType);
            lineBuilder.Add("ShortSilence");

            lineBuilder.Add("PickUp");
            lineBuilder.AddRange(SayNumberOfCars(jobInfo.allCarsToUnload));
            lineBuilder.Add("AtTrackType" + GetTrackTypeLetter(jobInfo.startingTrack));
            lineBuilder.AddRange(VoicedTrackId(jobInfo.startingTrack));
            lineBuilder.Add("ShortSilence");

            lineBuilder.Add("ThenUnloadThoseCars");
            lineBuilder.Add("AtTrack");
            lineBuilder.AddRange(VoicedTrackId(jobInfo.unloadMachineTrack));
            lineBuilder.Add("ShortSilence");

            lineBuilder.Add("ThenUncouple");
            for (var index = 0; index < jobInfo.destinationTracksData.Count; index++) {
                var carDataPerTrackID = jobInfo.destinationTracksData[index];
                var track = carDataPerTrackID.track;
                var carCount = carDataPerTrackID.cars.Count;
                if (index == jobInfo.destinationTracksData.Count - 1) {
                    lineBuilder.Add("And");
                }

                lineBuilder.AddRange(SayNumberOfCars(carCount));
                lineBuilder.Add("AtTrack");
                lineBuilder.AddRange(VoicedTrackId(track));
                if (index < jobInfo.destinationTracksData.Count - 1) {
                    lineBuilder.Add("ShortSilence");
                }
            }

            lineBuilder.Add("ToCompleteTheOrder");
        }

        public static string CreateShuntingUnloadJobLineFromJson(Job job) {
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


        public static void AddTransportJobLines(List<string> lineBuilder, Job job) {
            if (job == null || job.tasks == null || job.tasks.Count == 0) {
                Main.Logger.Error("Invalid job or no tasks found.");
                return;
            }

            var jobInfo = JobDataExtractor.ExtractTransportJobData(new Job_data(job));
            AddBasicTransportJobLines(lineBuilder, job.jobType, jobInfo.transportingCars, jobInfo.startingTrack,
                jobInfo.destinationTrack);
        }

        public static void AddEmptyHaulJobLines(List<string> lineBuilder, Job job) {
            if (job == null || job.tasks == null || job.tasks.Count == 0) {
                Main.Logger.Error("Invalid job or no tasks found.");
                return;
            }

            var jobInfo = JobDataExtractor.ExtractEmptyHaulJobData(new Job_data(job));
            AddBasicTransportJobLines(lineBuilder, job.jobType, jobInfo.transportingCars, jobInfo.startingTrack,
                jobInfo.destinationTrack);
        }

        public static void AddBasicTransportJobLines(List<string> lineBuilder, JobType jobType,
            List<Car_data> transportingCars,
            TrackID startingTrack, TrackID destinationTrack) {
            lineBuilder.Add("JobType" + jobType);
            lineBuilder.Add("ShortSilence");

            lineBuilder.Add("PickUp");
            lineBuilder.AddRange(SayNumberOfCars(transportingCars));
            lineBuilder.Add("FromTrack");
            lineBuilder.AddRange(VoicedTrackId(startingTrack));
            lineBuilder.Add("In");
            lineBuilder.Add(GetYardName(startingTrack));
            lineBuilder.Add("ShortSilence");

            lineBuilder.Add("ThenDropOffThoseCars");
            lineBuilder.Add("AtTrack");
            lineBuilder.AddRange(VoicedTrackId(destinationTrack));
            lineBuilder.Add("In");
            lineBuilder.Add(GetYardName(destinationTrack));
        }
        
        public static string CreateTransportJobLineFromJson(Job job) {
            if (job?.tasks == null || job.tasks.Count == 0) {
                Main.Logger.Error("Invalid job or no tasks found.");
                return "Error parsing job data.";
            }

            var jobInfo = JobDataExtractor.ExtractTransportJobData(new Job_data(job));
            return CreateBasicTransportJobLineFromJson(job.jobType, jobInfo.transportingCars,
                jobInfo.startingTrack, jobInfo.destinationTrack);
        }
        
        public static string CreateEmptyHaulJobLineFromJson(Job job) {
            if (job == null || job.tasks == null || job.tasks.Count == 0) {
                Main.Logger.Error("Invalid job or no tasks found.");
                return "Error parsing job data.";
            }

            var jobInfo = JobDataExtractor.ExtractEmptyHaulJobData(new Job_data(job));
            return CreateBasicTransportJobLineFromJson(job.jobType, jobInfo.transportingCars,
                jobInfo.startingTrack, jobInfo.destinationTrack);
        }

        public static string CreateBasicTransportJobLineFromJson(JobType jobType,
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

        public static void AddTaskLines(Task task, List<string> lineBuilder) {
            var taskData = task.GetTaskData();
            Main.Logger.Log(taskData.type.ToString());
            if (taskData.nestedTasks != null && taskData.nestedTasks.Count > 0) {
                foreach (var nestedTask in taskData.nestedTasks) {
                    AddTaskLines(nestedTask, lineBuilder);
                }

                return;
            }

            var start = taskData.startTrack;
            var end = taskData.destinationTrack;
            var carCount = taskData.cars?.Count;

            if (taskData.warehouseTaskType == WarehouseTaskType.Unloading) {
                lineBuilder.Add("Unload");
            } else if (taskData.warehouseTaskType == WarehouseTaskType.Loading) {
                lineBuilder.Add("Load");
            } else {
                lineBuilder.Add("Move");
            }

            if (carCount != null) {
                lineBuilder.AddRange(SayNumberOfCars(carCount.Value));
            } else {
                lineBuilder.Add("Cars");
            }

            if (start?.ID != null) {
                lineBuilder.Add("FromTrackType" + GetTrackTypeLetter(start.ID));
                lineBuilder.AddRange(VoicedTrackId(start.ID));
            }

            if (end?.ID != null) {
                if (taskData.warehouseTaskType == WarehouseTaskType.None) {
                    lineBuilder.Add("ToTrackType" + GetTrackTypeLetter(end.ID));
                } else {
                    lineBuilder.Add("AtTrackType" + GetTrackTypeLetter(end.ID));
                }

                lineBuilder.AddRange(VoicedTrackId(end.ID));
            }
        }

        public static TrackID ExtractTransportDestinationTrack(Job job) {
            var jobData = new Job_data(job);
            if (jobData.type == JobType.EmptyHaul) {
                var haulJobData = JobDataExtractor.ExtractEmptyHaulJobData(jobData);
                return haulJobData.destinationTrack;
            } else if (jobData.type == JobType.Transport) {
                var transportJobData = JobDataExtractor.ExtractTransportJobData(jobData);
                return transportJobData.destinationTrack;
            } else {
                throw new System.Exception("Unexpected job type for extracting destination track: " + jobData.type);
            }
        }
    }
}