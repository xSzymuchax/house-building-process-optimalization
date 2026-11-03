using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace PracaMagisterska
{
    class Resource
    {
        public ResourceType resourceType;
        public int resourceCost;
        public List<int> taskCost;
        public List<int> taskTime;
        public int resourceID; // nie chodzi o osobne obiekty, tylko o typ zasobu, np, 3 z kolei wczytany zasob, moze byc wiele instancji z tym samym ID
        public static int resourceIDGenerator=0;

        public Resource(ResourceType resourceType, int resourceCost)
        {
            this.resourceType = resourceType;
            taskCost = new();
            taskTime = new();
            this.resourceCost = resourceCost;
            resourceID = resourceIDGenerator++;
        }

        public static void ResetIDGenerator()
        {
            resourceIDGenerator = 0;
        }

        public Resource(Resource other)
        {
            this.resourceType = other.resourceType;
            this.resourceCost = other.resourceCost;
            this.taskCost = other.taskCost;
            this.taskTime = other.taskTime;
            this.resourceID = other.resourceID;
        }

        public void AddCost(int cost)
        {
            taskCost.Add(cost);
        }

        public void AddTime(int time)
        {
            taskTime.Add(time);
        }

        public void ShowResource()
        {
            string log = $"Type: {resourceType}\nCost: {resourceCost}\nID: {resourceID}\n(";

            for (int i = 0; i < taskTime.Count; i++)
            {
                log += $"{taskTime[i]},{taskCost[i]} ";
            }
            log += ")\n";
            Debug.WriteLine(log);
        }

        public string GetShowResourceString()
        {
            string log = $"Type: {resourceType}\nCost: {resourceCost}\n(";

            for (int i = 0; i < taskTime.Count; i++)
            {
                log += $"{taskTime[i]},{taskCost[i]} ";
            }
            log += ")\n";

            return log;
        }
    }
}

