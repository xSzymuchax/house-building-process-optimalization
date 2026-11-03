using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracaMagisterska
{
    class ResultSaver
    {
        public static int counter = 0;
        public ResultSaver()
        {

        }

        public string SaveResult(Graph g, int alpha, double beta, double gamma, double delta, double wc, double wk, double penalty, string strategyName, int iterations)
        {
            counter++;
            string log = "";
            log += $"ALPHA: {alpha}\nBETA: {beta}\nGAMMA: {gamma}\nDELTA: {delta}\nCW: {wc}\nKW: {wk}\nPENALTY: {penalty}\nSTRATEGY: {strategyName}\nITERATIONS: {iterations}\n|\n";
            log += Summary(g);
            log += ByResources(g);
            log += ByTasks(g);
            log += ByChannels(g);

            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string filename = $"RESULT_{timestamp.ToString()}.txt";
            File.WriteAllText(filename, log);

            return log;
        }

        public string SaveResult2(Graph g, int alpha, double beta, double gamma, double delta, double wc, double wk, double penalty, string strategyName, int iterations)
        {
            counter++;
            string log = "";
            log += $"ALPHA: {alpha}\nBETA: {beta}\nGAMMA: {gamma}\nDELTA: {delta}\nCW: {wc}\nKW: {wk}\nPENALTY: {penalty}\nSTRATEGY: {strategyName}\nITERATIONS: {iterations}\n|\n";
            log += Summary(g);
            log += ByResources(g);
            log += ByTasks(g);
            log += ByChannels(g);

            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string filename = $"RESULT_{timestamp.ToString()}_{counter}.txt";

            Directory.CreateDirectory("RESULTS");
            File.WriteAllText(Path.Combine("RESULTS", filename), log);

            return log;
        }

        private string Summary(Graph g)
        {
            string log = $"""        
                SUMMARY:
                    TIME: {g.GetCompletionTime()}
                    COST: {g.GetCompletionCost()}
                    RESOURCES_COUNT: 
                	    DEDICATED: {ResourcesManager.NumberOfDedicatedResources(g.GetUsedResources())}
                	    COMMON: {ResourcesManager.NumberOfCommonResources(g.GetUsedResources())}
                """;

            return log;
        }

        private string ByResources(Graph g)
        {
            List<UsedResource> usedResources = g.GetUsedResources();
            string dedicatedLog = "";
            string commonLog = "";
            string tempLog = "";
            string tasksLog = "";
            for (int i = 0; i < usedResources.Count; i++)
            {
                tasksLog = "";
                
                for (int j = 0; j < usedResources[i].WorkingTimes.Count; j++)
                {
                    tasksLog += $"                {usedResources[i].WorkingTimes[j].taskName} -> <{usedResources[i].WorkingTimes[j].startAt}, {usedResources[i].WorkingTimes[j].endAt}>\n";
                }

                if (usedResources[i].resource == null) continue;

                tempLog = $"""
                            {usedResources[i].name}:
                                ID: {usedResources[i].resource?.resourceID}
                                COST: {usedResources[i].GeneratedCost}
                                TASKS: 
                    {tasksLog}
                    """;

                if (usedResources[i].resource?.resourceType == ResourceType.DEDICATED)
                    dedicatedLog += tempLog;
                else
                    commonLog += tempLog;
            }

            string log = $"""
                

                BY_RESOURCES:
                    DEDICATED:
                {dedicatedLog}
                    COMMON:
                {commonLog}
                """;

            return log;
        }

        private string ByTasks(Graph g)
        {
            string log = """
                BY_TASKS:

                """;

            for (int i = 0; i < g.GetNodes().Count; i++)
            {
                GraphNode? gn = g.GetNode(i);
                if (gn == null) continue;
                
                string urLog = "";
                foreach (UsedResource ur in gn.UsedResource)
                {
                    urLog += ur.name + " ";
                }
                log += $"\t{gn.TaskName}:\n\t\tTIME: {gn.MyTaskEndTime - (gn.MyTaskStartTime)}\n\t\tSTART_TIME: {gn.MyTaskStartTime}\n\t\tEND_TIME: {gn.MyTaskEndTime}\n\t\tRESOURCES: {urLog}\n";
            }

            return log;
        }

        private string ByChannels(Graph g)
        {
            string log = "BY_CHANNELS\n";

            foreach (UsedChannel uc in g.GetUsedChannels())
            {
                log += $"\t{uc.Channel.ChannelName}:\n\t\tCHECKS_AT:\n";
                for (int i = 0; i<uc.ChekingTimes.Count; i++)
                {
                    log += $"\t\t\t{uc.ChekingTimes[i].taskName}: <{uc.ChekingTimes[i].startAt}, {uc.ChekingTimes[i].endAt}>\n";
                }
            }

            return log;
        }
    }
}
