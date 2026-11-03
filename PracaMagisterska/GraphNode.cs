using PracaMagisterska;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace PracaMagisterska
{
    class GraphNode
    {
        public string TaskName { get; set; }
        public int TaskID { get; set; }
        public TaskType TaskType { get; set; }
        public List<UsedResource> UsedResource { get; set; }
        
        public Func<ResourceAllocationContext, List<Resource>>? ResourceAllocationFunction { get; set; }
        public int MaxResourcesAllocated;
        public GraphNode? MyParent;
        public double LabelValueFromParent;
        public double MyTaskEndTime;
        public double MyTaskStartTime;

        public int MAX_POSSIBLE_RESOURCES_AMOUNT = 5;
        public double MULTIPLE_RESOURCE_EFFECTIVENESS = 0.7; // każdy kolejny zasób skraca czas pracy o 30%
        public GraphNode(string taskName)
        {
            TaskName = taskName;
            TaskID = int.Parse(TaskName.Split('_')[1]);
            TaskType = TypesManager.FindTaskType(taskName);
            ResourceAllocationFunction = null;
            MyParent = null;
            MyTaskStartTime = double.MaxValue;
            MyTaskEndTime = double.MaxValue;
            UsedResource = new();
            LabelValueFromParent = 0;
            GenerateMaxAllocatedResourcesAmount();
        }

        public GraphNode(GraphNode other)
        {
            TaskName = other.TaskName;
            TaskID = other.TaskID;
            TaskType = other.TaskType;
            ResourceAllocationFunction = other.ResourceAllocationFunction;
            UsedResource = other.UsedResource;
            MyTaskEndTime = other.MyTaskEndTime;
            MyTaskStartTime = other.MyTaskStartTime;
            LabelValueFromParent = other.LabelValueFromParent;
            MaxResourcesAllocated = other.MaxResourcesAllocated;
        }

        public void SetResourceAllocationFunction(Func<ResourceAllocationContext, List<Resource>> func)
        {
            ResourceAllocationFunction = func;
        }

        public void GenerateMaxAllocatedResourcesAmount()
        {
            Random rand = new();
            MaxResourcesAllocated = rand.Next(1, MAX_POSSIBLE_RESOURCES_AMOUNT + 1);
        }

        public void MutateMaxResourcesAmount()
        {
            Random rand = new();
            
            int delta = 0;
            while (delta == 0)
            {
                int maxChange = (int)(Math.Ceiling((double)(MaxResourcesAllocated * 0.3)));
                delta = rand.Next(-maxChange, maxChange+1); // zmiana o maks 30%, zaokrąglona
            }

            MaxResourcesAllocated += delta;

            if (MaxResourcesAllocated <= 0)
                MaxResourcesAllocated = 1;
        }

        public void ExecuteResourceAllocationFunction(List<UsedResource> graphResources, List<UsedChannel> graphChannels)
        {
            if (ResourceAllocationFunction == null)
                return;

            // przygotowanie
            UsedResource = new();
            MyTaskStartTime = double.MaxValue;
            MyTaskEndTime = double.MaxValue;
            ResourceAllocationContext ctx = new()
            {
                NodeIndex = TaskID,
                ParentNode = MyParent,
                UsedResources = graphResources,
                MaxResourcesAllocated = this.MaxResourcesAllocated,
                TaskType = this.TaskType
            };

            List<Resource> allocatedResources = ResourceAllocationFunction(ctx);

            if (allocatedResources.Count == 0)
                return; 
                
            // zebranie zasobów
            List<UsedResource> currentResources = new();

            // jesli dostalismy zasoby ogolne, mozemy je wykorzystac ponownie
            if (allocatedResources[0].resourceType == ResourceType.COMMON)
            {
                foreach (UsedResource ur in graphResources)
                {
                    if (ur.resource == null) continue;

                    if (allocatedResources.Contains(ur.resource))
                    {
                        currentResources.Add(ur);
                        allocatedResources.Remove(ur.resource);
                    }
                }
            }

            foreach (Resource resource in allocatedResources)
            {
                UsedResource ur = new(resource);
                if (resource.resourceType == ResourceType.COMMON)
                    ur.IncreaseGeneratedCost(resource.resourceCost);
                currentResources.Add(ur);
                graphResources.Add(ur);
            }

            UsedResource = currentResources;
            
            // przeliczenie czasów
            double parentCheckedAt;
            double taskTimeFactor = Math.Pow(MULTIPLE_RESOURCE_EFFECTIVENESS, UsedResource.Count-1); // 1 -> 100%, 2-> 70%, 3-> 49%

            Resource? res = UsedResource[0].resource;
            if (res == null) return;

            double taskTime = res.taskTime[TaskID];
            double taskDuration = taskTime * taskTimeFactor;

            if (TaskID == 0)
            {
                MyTaskStartTime = 0;
                MyTaskEndTime = taskDuration;
            }
            else
            {
                parentCheckedAt = FindMyMinimumStartTime(graphChannels);
                var allResourcesFreeInPeriod = FindFreePeriodForAllResources(UsedResource, parentCheckedAt, taskDuration);
                MyTaskStartTime = allResourcesFreeInPeriod.Item1;
                MyTaskEndTime = allResourcesFreeInPeriod.Item2;
            }

            foreach (UsedResource ur in UsedResource)
            {
                var item = ur.resource;
                if (item == null) continue;

                ur.AddWorkingPeriod(MyTaskStartTime, MyTaskEndTime, TaskName);
                ur.IncreaseGeneratedCost(item.taskCost[TaskID]);
            }
        }

        private (double start, double end) FindFreePeriodForAllResources(List<UsedResource> usedResources, double startAt, double duration)
        {

            double tryStart = startAt;

            while (true)
            {
                double tryEnd = tryStart + duration;
                bool allFree = true;

                foreach (UsedResource ur in usedResources)
                {
                    if (!ur.IsFreeAtPeriod(tryStart, tryEnd))
                    {
                        foreach (var wt in ur.WorkingTimes)
                        {
                            if (tryStart < wt.endAt && tryEnd > wt.startAt)
                            {
                                tryStart = wt.endAt;
                                allFree = false;
                                break;
                            }
                        }

                        if (!allFree)
                            break;
                    }
                }

                if (allFree)
                    return (tryStart, tryEnd);
            }
        }

        private bool UsedSameResourcesAsParent() 
        {
            if (MyParent == null) return false;

            if (UsedResource.Count != MyParent.UsedResource.Count)
                return false;

            // prawda tylko jesli zbiory są identyczne
            return UsedResource.All(ur => MyParent.UsedResource.Any(p_ur => object.ReferenceEquals(p_ur, ur)));

        }

        // znajduje czas, w ktorym zakonczy sie sprawdzanie rodzica przez kontrolera
        private double FindMyMinimumStartTime(List<UsedChannel> graphChannels)
        {
            if (MyParent == null) return double.MaxValue;
            double parentEndsAt = MyParent.MyTaskEndTime;
            if (!UsedSameResourcesAsParent())
            {
                Resource? r = UsedResource[0].resource;
                if (r == null) return double.MaxValue;

                var x = ChannelsManager.FindFastestPossibleCheck(graphChannels, parentEndsAt, LabelValueFromParent, r.resourceID); 
                graphChannels[x.channel].AddWorkingPeriod(x.start, x.end, TaskName);
                parentEndsAt = x.end;
            }

            return parentEndsAt;
        }
    }
}