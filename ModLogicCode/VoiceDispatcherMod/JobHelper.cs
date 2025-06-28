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
            var lineBuilder = new List<string>();
            lineBuilder.Add("YouHave");
            AddJobSpecificLines(lineBuilder, job);
            CommsRadioNarrator.PlayWithClick(lineBuilder);
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

        public static void AddShuntingUnloadJobLines(List<string> lineBuilder, Job job) {
            if (job == null || job.tasks == null || job.tasks.Count == 0) {
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