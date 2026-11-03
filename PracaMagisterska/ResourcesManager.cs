using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace PracaMagisterska
{ 
    static class ResourcesManager
    {
        public static List<Resource> Resources = new();

        public static void SetResources(List<Resource> resources)
        {
            Resources = resources;
        }

        // zwraca nowe najszybsze zasoby spośród dostępnych w danym problemie zasobów w liczbie maxResourceAmount lub 1, jeśli nie da się dostać więcej
        public static List<Resource> FindFastestResource(int taskID, int maxResourceAmount, TaskType taskType)
        {
            Resource? fastestResource = null;
            List<Resource> result = new();

            foreach (Resource r in Resources)
            {
                if (TypesManager.IsTaskExecutableByResource(r.resourceType, taskType) && r.taskTime[taskID] != -1)
                {
                    fastestResource = r;
                    break;
                }
            }

            if (fastestResource == null) return result;

            foreach (Resource r in Resources)
            {
                if (r.taskTime[taskID] < fastestResource.taskTime[taskID] && r.taskTime[taskID] != -1 && TypesManager.IsTaskExecutableByResource(r.resourceType, taskType))
                    fastestResource = r;
            }

            if (TypesManager.IsTaskExecutableByManyResource(taskType))
            {
                for (int i = 0; i < maxResourceAmount; i++)
                    result.Add(fastestResource);
            }
            else
            {
                result.Add(fastestResource);
            }

            return result;
        }


        // szuka najszybszego zasobu w już wykorzystanych w drzewie zasobach, zwracając maksymalnie maxResourceAmount zasobów, przy czym, zawsze zwrócone zasoby są tego samego typu
        // sortuje zasoby według ich czasu zwolnienia, tzn, zasob ktory najszybciej będzie wolny, będzie zwrócony 
        public static List<Resource> FindFastestExistingResource(int taskID, int maxResourceAmount, List<UsedResource> usedResources, TaskType taskType)
        {
            if (taskType == TaskType.DEDICATED_TASK || taskType == TaskType.COMMON_DEDICATED_TASK)
                return new();

            List<Resource> result = new();
            List<UsedResource> fastestResources = new();
            List<UsedResource> commonResources = FindCommonUsedResources(usedResources);
            Resource? fastestResource = null;

            foreach (UsedResource ur in commonResources)
            {
                if (ur.resource == null) continue;

                if (fastestResource == null)
                {
                    if (TypesManager.IsTaskExecutableByResource(ur.resource.resourceType, taskType) && ur.resource.taskTime[taskID] != -1)
                    {
                        fastestResource = ur.resource;
                        fastestResources.Add(ur);
                        continue;
                    }
                }
                else
                {
                    if (ur.resource.taskTime[taskID] < fastestResource.taskTime[taskID] && TypesManager.IsTaskExecutableByResource(ur.resource.resourceType, taskType) && ur.resource.taskTime[taskID] != -1)
                    {
                        fastestResource = ur.resource;
                        fastestResources = new();
                    }

                    if (ur.resource.resourceID == fastestResource.resourceID)
                        fastestResources.Add(ur);
                }
                
            }

            // tutaj mamy juz w result liste najszybszych zasobow
            List<UsedResource> sorted = fastestResources.OrderBy(r => r.LastJobEndsAt).ToList();
            
            if (TypesManager.IsTaskExecutableByManyResource(taskType))
            {
                int realMaxAmount = Math.Min(sorted.Count, maxResourceAmount);
                for (int i = 0; i < realMaxAmount; i++)
                {
                    var item = sorted[i];
                    if (item.resource != null)
                        result.Add(item.resource);
                }    
            }
            else
            {
                if (sorted.Count > 0)
                {
                    var item = sorted[0];
                    if (item.resource != null)
                        result.Add(item.resource);
                }    
            }

            return result;
        }


        // zwraca zestaw zasobów, ktory był wykorzystany przy rodzicu. dla dedykowanych zwraca nowe dedykowane
        public static List<Resource> SameResourceAsParent(int taskID, List<UsedResource> usedResources, GraphNode parentNode, TaskType taskType)
        {
            List<Resource> result = new();

            if (taskID==0 ||  parentNode.UsedResource.Count == 0)
                return new();

            bool childMulti = TypesManager.IsTaskExecutableByManyResource(taskType);
            bool isAllowed = childMulti || (!childMulti && parentNode.UsedResource.Count <= 1);
                
            if (isAllowed)
            {
                if (parentNode == null ) return result;
                var parentNodeUr = parentNode.UsedResource[0];
                if (parentNodeUr.resource == null) return result;

                if (TypesManager.IsTaskExecutableByResource(parentNodeUr.resource.resourceType, taskType) && parentNodeUr.resource.taskCost[taskID] != -1)
                {
                    if (taskType == TaskType.COMMON_DEDICATED_TASK || taskType == TaskType.DEDICATED_TASK) 
                    {
                        foreach (UsedResource ur in parentNode.UsedResource)
                        {
                            if (ur.resource == null) continue;
                            result.Add(new(ur.resource));
                        }
                    }

                    else
                    {
                        foreach (UsedResource ur in parentNode.UsedResource)
                        {
                            if (ur.resource == null) continue;
                            result.Add(ur.resource);
                        }
                    }
                }
            }

            return result;
        }


        // zwraca nowe najtańsze zasoby spośród dostępnych w danym problemie zasobów w liczbie maxResourceAmount lub 1, jeśli nie da się dostać więcej 
        public static List<Resource> FindCheapestResource(int taskID, int maxResourceAmount, TaskType taskType)
        {
            Resource? cheapestResource = null;
            List<Resource> result = new();

            foreach (Resource r in Resources)
            {
                if (TypesManager.IsTaskExecutableByResource(r.resourceType, taskType) && r.taskCost[taskID] != -1)
                {
                    cheapestResource = r;
                    break;
                }
            }

            if (cheapestResource == null) return result;

            foreach (Resource r in Resources)
            {
                if (r.taskCost[taskID] < cheapestResource.taskCost[taskID] && r.taskCost[taskID] != -1 && TypesManager.IsTaskExecutableByResource(r.resourceType, taskType))
                    cheapestResource = r;
            }

            if (TypesManager.IsTaskExecutableByManyResource(taskType))
            {
                for (int i = 0; i < maxResourceAmount; i++)
                    result.Add(cheapestResource);
            }
            else
            {
                result.Add(cheapestResource);
            }

            return result;
        }


        // szuka najtańszego zasobu w już wykorzystanych w drzewie zasobach, zwracając maksymalnie maxResourceAmount zasobów, przy czym, zawsze zwrócone zasoby są tego samego typu
        // sortuje zasoby według ich czasu zwolnienia, tzn, zasob ktory najszybciej będzie wolny, będzie zwrócony 
        public static List<Resource> FindExistingCheapestResource(List<UsedResource> usedResources, int taskID, int maxResourceAmount, TaskType taskType)
        {
            if (taskType == TaskType.DEDICATED_TASK || taskType == TaskType.COMMON_DEDICATED_TASK)
                return new();

            List<Resource> result = new();
            List<UsedResource> cheapestResources = new();
            List<UsedResource> commonResources = FindCommonUsedResources(usedResources);
            Resource? cheapestResource = null;

            foreach (UsedResource ur in commonResources)
            {
                if (ur.resource == null) continue;

                if (cheapestResource == null)
                {
                    if (TypesManager.IsTaskExecutableByResource(ur.resource.resourceType, taskType) && ur.resource.taskCost[taskID] != -1)
                    {
                        cheapestResource = ur.resource;
                        cheapestResources.Add(ur);
                        continue;
                    }
                }
                else
                {
                    if (ur.resource.taskCost[taskID] < cheapestResource.taskCost[taskID] && TypesManager.IsTaskExecutableByResource(ur.resource.resourceType, taskType) && ur.resource.taskCost[taskID] != -1)
                    {
                        cheapestResource = ur.resource;
                        cheapestResources = new();
                    }

                    if (ur.resource.resourceID == cheapestResource.resourceID)
                        cheapestResources.Add(ur);
                }

            }

            // tutaj mamy juz w result liste najszybszych zasobow
            List<UsedResource> sorted = cheapestResources.OrderBy(r => r.LastJobEndsAt).ToList();

            if (TypesManager.IsTaskExecutableByManyResource(taskType))
            {
                int realMaxAmount = Math.Min(sorted.Count, maxResourceAmount);
                for (int i = 0; i < realMaxAmount; i++)
                {
                    var item = sorted[i];
                    if (item.resource != null)
                        result.Add(item.resource);
                }
                    
            }
            else
            {
                if (sorted.Count > 0)
                {
                    var item = sorted[0];
                    if (item.resource != null)
                        result.Add(item.resource);
                }
                    
            }

            return result;
        }


        //zwraca nowy zasob o najmniejszym wspolczynniku czas*koszt
        public static List<Resource>? FindBestTimeCostFactorResource(int taskID, int maxResourceAmount, TaskType taskType)
        {
            Resource? bestTimeCostResource = null;
            List<Resource> result = new();

            foreach (Resource r in Resources)
            {
                if (TypesManager.IsTaskExecutableByResource(r.resourceType, taskType) && r.taskCost[taskID] != -1)
                {
                    bestTimeCostResource = r;
                    break;
                }
            }

            if (bestTimeCostResource == null) return result;

            foreach (Resource r in Resources)
            {
                if (r.taskCost[taskID] * r.taskTime[taskID] < bestTimeCostResource.taskCost[taskID] * bestTimeCostResource.taskTime[taskID] 
                    && r.taskCost[taskID] != -1
                    && TypesManager.IsTaskExecutableByResource(r.resourceType, taskType)
                    )
                    bestTimeCostResource = r;
            }

            if (TypesManager.IsTaskExecutableByManyResource(taskType))
            {
                for (int i = 0; i < maxResourceAmount; i++)
                    result.Add(bestTimeCostResource);
            }
            else
            {
                result.Add(bestTimeCostResource);
            }

            return result;
        }


        // szuka najlepszego czas*koszt zasobu w już wykorzystanych w drzewie zasobach, zwracając maksymalnie maxResourceAmount zasobów, przy czym, zawsze zwrócone zasoby są tego samego typu
        // sortuje zasoby według ich czasu zwolnienia, tzn, zasob ktory najszybciej będzie wolny, będzie zwrócony 
        public static List<Resource>? FindExistingTimeCostFactorResource(List<UsedResource> usedResources, int taskID, TaskType taskType, int maxResourceAmount)
        {
            if (taskType == TaskType.DEDICATED_TASK || taskType == TaskType.COMMON_DEDICATED_TASK)
                return new();

            List<Resource> result = new();
            List<UsedResource> timeCostResources = new();
            List<UsedResource> commonResources = FindCommonUsedResources(usedResources);
            Resource? timeCostResource = null;

            foreach (UsedResource ur in commonResources)
            {
                if (ur.resource == null) continue;

                if (timeCostResource == null)
                {
                    if (TypesManager.IsTaskExecutableByResource(ur.resource.resourceType, taskType) && ur.resource.taskCost[taskID] != -1)
                    {
                        timeCostResource = ur.resource;
                        timeCostResources.Add(ur);
                        continue;
                    }

                }
                else
                {
                    if (ur.resource.taskCost[taskID] * ur.resource.taskTime[taskID] < timeCostResource.taskCost[taskID] * timeCostResource.taskTime[taskID] && TypesManager.IsTaskExecutableByResource(ur.resource.resourceType, taskType) && ur.resource.taskCost[taskID] != -1)
                    {
                        timeCostResource = ur.resource;
                        timeCostResources = new();
                    }

                    if (ur.resource.resourceID == timeCostResource.resourceID)
                        timeCostResources.Add(ur);
                }

            }

            // tutaj mamy juz w result liste najszybszych zasobow
            List<UsedResource> sorted = timeCostResources.OrderBy(r => r.LastJobEndsAt).ToList();

            if (TypesManager.IsTaskExecutableByManyResource(taskType))
            {
                int realMaxAmount = Math.Min(sorted.Count, maxResourceAmount);
                for (int i = 0; i < realMaxAmount; i++)
                {
                    var item = sorted[i];
                    if (item.resource != null)
                        result.Add(item.resource);
                }
                    
            }
            else
            {
                if (sorted.Count > 0)
                {
                    var item = sorted[0];
                    if (item.resource != null)
                        result.Add(item.resource);
                }
            }

            return result;
        }


        // zwraca najrzadziej alokowany istniejący zasob ogolny, w liczbie maksymalnie maxResourceAmount
        public static List<Resource> FindExistingLeastAllocatedResource(List<UsedResource> usedResources, int taskID, TaskType taskType, int maxResourceAmount)
        {
            if (taskType == TaskType.DEDICATED_TASK || taskType == TaskType.COMMON_DEDICATED_TASK)
                return new();

            Dictionary<UsedResource, int> dict = new();

            foreach (UsedResource ur in usedResources)
            {
                if (ur.resource == null) continue;

                if (ur.resource.resourceType == ResourceType.COMMON)
                {
                    dict.Add(ur, ur.WorkingTimes.Count);
                }
            }

            // jesli nie ma zadnego zasobu ogolnego
            if (dict.Count == 0)
            {
                return new();
            }

            var resultDict = dict
                .GroupBy(x => new
                {
                    x.Value,
                    x.Key.resource!.resourceID
                })
                .OrderBy(g => g.Key.Value)
                .Select(g => new
                {
                    V = g.Key,
                    C = g.Count(),
                    O = g.Select(p => p.Key) // usedResource
                })
                .ToList();


            List<Resource> result = new();
            foreach (var r in resultDict)
            {
                var firstElement = r.O.FirstOrDefault();

                if (firstElement != null && firstElement.resource != null)
                {
                    if (firstElement.resource.taskTime[taskID] != -1)
                    {
                        if (TypesManager.IsTaskExecutableByManyResource(taskType))
                        {
                            int realMaxAmount = Math.Min(r.O.Count(), maxResourceAmount);
                            int i = 0;
                            foreach (var res in r.O)
                            {
                                if (res.resource == null) continue;

                                result.Add(res.resource);
                                i++;
                                if (i == realMaxAmount)
                                    break;
                            }
                        }
                        else
                        {
                            result.Add(firstElement.resource);
                        }
                        break;
                    }
                }
            }

            return result;
        }


        // zwraca najszybciej dostepne zasoby ogolne, patrzac po koncu poprzedniego zadania zasobu, maksymalnie maxResourceAmount zasobów
        public static List<Resource> FindExistingFastestFreeResource(List<UsedResource> usedResources, int maxResourceAmount, TaskType taskType)
        {
            if (taskType == TaskType.DEDICATED_TASK || taskType == TaskType.COMMON_DEDICATED_TASK)
                return new();

            List<Resource> result = new();
            List<UsedResource> commonResources = FindCommonUsedResources(usedResources);
            List<UsedResource> sortedByLastJobEnd = commonResources.OrderBy(ur => ur.LastJobEndsAt).ToList();

            if (sortedByLastJobEnd.Count == 0) return new();

            Resource? firstResource = sortedByLastJobEnd[0].resource;
            if (firstResource == null) return new();

            List<UsedResource> resourcesOfFastestType = sortedByLastJobEnd.FindAll(ur => ur.resource?.resourceID == firstResource.resourceID);

            int realMaxAmount = 0;
            if (TypesManager.IsTaskExecutableByManyResource(taskType))
            {
                realMaxAmount = Math.Min(resourcesOfFastestType.Count, maxResourceAmount);
            }
            else
            {
                realMaxAmount = Math.Min(resourcesOfFastestType.Count, 1);
            }

            for (int i = 0; i < realMaxAmount; i++)
            {
                var item = resourcesOfFastestType[i];
                if (item.resource != null)
                    result.Add(item.resource);
            }

            return result;
        }
    
        // helper
        private static List<UsedResource> FindCommonUsedResources(List<UsedResource> usedResources)
        {
            return usedResources.FindAll(r => r.resource?.resourceType == ResourceType.COMMON);
        }
    
        public static int NumberOfCommonResources(List<UsedResource> usedResources)
        {
            int result = 0;
            foreach (var ur in usedResources)
            {
                if (ur.resource == null) continue;
                if (ur.resource.resourceType == ResourceType.COMMON)
                    result++;
            }
                
            return result;
        }
        public static int NumberOfDedicatedResources(List<UsedResource> usedResources)
        {
            int result = 0;
            foreach (var ur in usedResources)
            {
                if (ur.resource == null) continue;
                if (ur.resource.resourceType == ResourceType.DEDICATED)
                    result++;
            }
                
            return result;
        }
    }
}


