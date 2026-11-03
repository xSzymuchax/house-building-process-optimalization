using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracaMagisterska
{
    static class ChannelsManager
    {
        public static List<Channel> Channels = new();

        public static void SetChannels(List<Channel> channels)
        {
            Channels = channels;
        }

        public static UsedChannel GetNewChannel(int channelID)
        {
            if (channelID < Channels.Count)
                return new(Channels[channelID]);
            return new(Channels[0]);
        }

        public static List<UsedChannel> GetEveryChannel()
        {
            List<UsedChannel> result = new();
            Channels.ForEach(c => result.Add(new UsedChannel(c)));
            return result;
        }

        public static List<UsedChannel> ResetChannels(List<UsedChannel> usedChannels)
        {
            foreach (UsedChannel usedChannel in usedChannels)
            {
                usedChannel.ResetCheckingData();
            }

            return usedChannels;
        }

        public static (int channel, double start, double end) FindFastestPossibleCheck(List<UsedChannel> usedChannels, double startAt, double edgeLabel, int resourceID)
        {
            // znalezc pierwszego kontrolera ktory ma uprawnienia
            // (na pewno jakis istnieje) -> na poczatku przydzielany jest kazdy jeden, potem na drodze ewolucji moze zniknac, co jest sprawdzane
            int bestChannel = -1;
            for (int i=0;i<usedChannels.Count;i++)
                if (usedChannels[i].Channel.qualificationsList[resourceID] == 1)
                {
                    bestChannel = i;
                    break;
                }    
    
            // teraz szukamy rzeczywistego najszybszego kontrolera dla zadania
            double bestStart = double.MaxValue;
            double bestEnd = double.MaxValue;

            for (int i=0; i < usedChannels.Count; i++)
            {
                if (usedChannels[i].Channel.qualificationsList[resourceID] == 0)
                    continue;

                double duration = edgeLabel / usedChannels[i].Channel.Bandwidth;
                var period = usedChannels[i].FindFreePeriod(startAt, duration);
                if (period.Item2 < bestEnd)
                {
                    bestStart = period.Item1;
                    bestEnd = period.Item2;
                    bestChannel = i;
                }
            }

            return new(bestChannel, bestStart, bestEnd);
        }
    }
}
