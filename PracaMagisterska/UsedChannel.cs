using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracaMagisterska
{
    class UsedChannel
    {
        public Channel Channel;
        public List<WorkingPeriod> ChekingTimes;
        public double LastCheckEndsAt;
        public double GeneratedCost;

        public UsedChannel(Channel channel)
        {
            Channel = new(channel);
            ChekingTimes = new();
            LastCheckEndsAt = 0;
            GeneratedCost += Channel.ChannelCost;
        }

        public UsedChannel(UsedChannel other)
        {
            Channel = other.Channel;
            ChekingTimes = new(other.ChekingTimes);
            LastCheckEndsAt = other.LastCheckEndsAt;
            GeneratedCost = other.GeneratedCost;
        }

        public void ResetCheckingData()
        {
            ChekingTimes = new();
            LastCheckEndsAt = 0;
        }

        public double CheckingTime(double edgeValue)
        {
            return (double)edgeValue / (double)Channel.Bandwidth;
        }

        public void AddWorkingPeriod(double startAt, double endAt, string taskName)
        {
            ChekingTimes.Add(new(startAt, endAt, taskName));
            ChekingTimes = ChekingTimes.OrderBy(x => x.startAt).ToList();

            if (endAt >= LastCheckEndsAt)
                LastCheckEndsAt = endAt;
        }

        // skorzystamy z tego, ze zadania wykonywane przez kontrolera są ulozone po kolei
        public ValueTuple<double, double> FindFreePeriod(double startAt, double duration)
        {
            double possibleStart = startAt;
            foreach (var period in ChekingTimes)
            {
                if (possibleStart + duration <= period.startAt)
                    return new(possibleStart, possibleStart + duration);
                if (possibleStart < period.endAt)
                    possibleStart = period.endAt;
            }

            return new(possibleStart, possibleStart + duration);
        }

        public double WillBeFreeAt()
        {
            return LastCheckEndsAt;
        }

        public string GetWorkingPeriodsString()
        {
            string result = "";
            foreach (var wt in ChekingTimes)
            {
                result += $"<{wt.startAt},{wt.endAt}>";
            }
            return result;
        }
    }
}
