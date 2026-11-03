using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracaMagisterska
{
    class UsedResource
    {
        public Resource? resource;
        public string name;
        public List<WorkingPeriod> WorkingTimes;
        public int GeneratedCost;
        public double LastJobEndsAt;

        private static int namesIndexer = 0;

        public UsedResource(Resource resource)
        {
            this.resource = new Resource(resource);
            WorkingTimes = new();
            GeneratedCost = 0;
            LastJobEndsAt = 0;

            name = "RES_" + namesIndexer++.ToString();
        }

        public UsedResource(UsedResource other)
        {
            resource = other.resource;
            WorkingTimes = new(other.WorkingTimes);
            GeneratedCost = other.GeneratedCost;
            LastJobEndsAt = other.LastJobEndsAt;
            name = other.name;
        }

        public static void ResetNamesIndexer()
        {
            namesIndexer = 0;
        }

        public void AddWorkingPeriod(double startAt, double endAt, string taskName)
        {
            WorkingTimes.Add(new(startAt, endAt, taskName));
            WorkingTimes = WorkingTimes.OrderBy(x => x.startAt).ToList();
   
            if (endAt >= LastJobEndsAt)
                LastJobEndsAt = endAt;
        }

        public ValueTuple<double, double> FindFreePeriod(double startAt, double duration)
        {
            double possibleStart = startAt;
            foreach (var period in WorkingTimes)
            {
                if (possibleStart + duration <= period.startAt)
                    return new(possibleStart, possibleStart + duration);
                if (possibleStart < period.endAt)
                    possibleStart = period.endAt;
            }

            return new(possibleStart, possibleStart + duration);
        }

        public bool IsFreeAtPeriod(double startAt, double endAt)
        {
            foreach (var wt in WorkingTimes)
            {
                if (startAt < wt.endAt && endAt > wt.startAt)
                    return false;
            }
            return true;
        }

        public void IncreaseGeneratedCost(int amount)
        {
            GeneratedCost += amount;
        }

        public string GetWorkingPeriodsString()
        {
            string result = "";
            foreach (var wt in WorkingTimes)
            {
                result += $"<{wt.startAt}, {wt.endAt}>";
            }
            return result;
        }
    }
}
