using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracaMagisterska
{
    class Channel
    {
        public string ChannelName;
        public int ChannelCost; // doliczany raz
        public double Bandwidth; // etykieta krawedzi / bandwith -> czas jaki nalezy dodac do czasu wykonania
        public List<int> qualificationsList;

        public Channel(string channelName, int channelCost, double bandwidth)
        {
            ChannelName = channelName;
            ChannelCost = channelCost;
            Bandwidth = bandwidth;
            qualificationsList = new();
        }

        public Channel(Channel other)
        {
            ChannelName = other.ChannelName;
            ChannelCost = other.ChannelCost;
            Bandwidth = other.Bandwidth;
            qualificationsList = other.qualificationsList;
        }

        public void ShowChannel()
        {
            string log = $"Name: {ChannelName}\nCost: {ChannelCost}\nBandwidth: {Bandwidth}\nQualifications:";
            foreach (int q in qualificationsList) log += $" {q.ToString()}";
            Debug.WriteLine(log);
        }
    }
}
