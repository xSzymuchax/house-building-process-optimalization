using PracaMagisterska;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracaMagisterska
{
    class GeneticOperators
    {
        // szansa na mutację ilosci zasobów, jeśli nie ilość mutuje, to strategia
        public double RESOURCE_AMOUNT_MUTATE_CHANCE = 0.6;

        //szansa na zmianę liczby kontrolerów, zawsze zmienia się o 1, jesli usuwa kontrolera, sprawdza czy osobnik będzie legalny, jeśli nie -> zmienia inaczej
        public double CHANNEL_AMOUNT_MUTATE_CHANCE = 0.3;
        public GeneticOperators() { }

        public List<Graph> Crossover(Graph g1, Graph g2) 
        {
            Random rand = new();
            int crossNode = rand.Next(g1.NodesAmount()); 
            GraphNode? selectedNode = g1.GetNode(crossNode);

            if (selectedNode == null)
                return new();

            // towrzenei nowych osobników
            List<GraphNode> g1Nodes = new();
            List<GraphNode> g2Nodes = new();

            foreach (var node in g1.GetNodes())
                g1Nodes.Add(new GraphNode(node));

            foreach (var node in g2.GetNodes())
                g2Nodes.Add(new GraphNode(node));

            List<Graph> newTrees = new();
            newTrees.Add(new Graph(g1Nodes, g1.GetEdges()));
            newTrees.Add(new Graph(g2Nodes, g2.GetEdges()));

            // BFS 
            string selectedNodeName = selectedNode.TaskName;
            List<int> nodesToChange = g1.BFSGetNodeIndexes(selectedNodeName);

            // zmiana node miedzy drzewami
            foreach (int index in nodesToChange)
            {
                var g2Node = g2.GetNode(index);
                var g1Node = g1.GetNode(index);

                if (g2Node == null || g1Node == null) continue;

                newTrees[0].ChangeNode(index, new GraphNode(g2Node));
                newTrees[1].ChangeNode(index, new GraphNode(g1Node));
            }

            newTrees[0].FindNodesParents();
            newTrees[1].FindNodesParents();

            // skopiowanie kontrolerow
            newTrees[0].SetUsedChannels(g1.GetUsedChannels());
            newTrees[0].SetUsedChannelsAmounts(g1.GetUsedChannelsAmounts());
            newTrees[1].SetUsedChannels(g1.GetUsedChannels());
            newTrees[1].SetUsedChannelsAmounts(g1.GetUsedChannelsAmounts());

            return newTrees;
        }

        private List<GraphNode> CriticalPath(Graph g)
        {
            GraphNode? slowest = g.LastCompletedNode();

            if (slowest == null) return new();
            List<GraphNode> path = g.GetPathToRoot(slowest);
            return path;
        }

        public Graph Mutation(Graph g, List<Func<ResourceAllocationContext, List<Resource>>> commonAllocationFunctions, List<Func<ResourceAllocationContext, List<Resource>>> dedicatedAllocationFunctions) 
        {
            Random rand = new();
            GraphNode? gn = null;

            // wybór node            
            if (rand.NextDouble() <= 0.6) // 60% mutacji zadania w najwolniejszej sceizce
            {
                var criticalPath = CriticalPath(g);
                var weights = criticalPath.Select(n =>
                {
                    double time = n.MyTaskEndTime - n.MyTaskStartTime;
                    return TypesManager.IsTaskExecutableByManyResource(n.TaskType) ? time : time / 4; // zadania 1 zasobowe powinny miec niską wagę
                }).ToList();
                double totalWeigth = weights.Sum();
                double w = rand.NextDouble() * totalWeigth;

                double progres = 0;
                for (int i = 0; i < criticalPath.Count; i++)
                {
                    progres += weights[i];
                    if (w <= progres)
                    {
                        gn = criticalPath[i];
                        break;
                    }
                }
            }
            else // 30% mutacja w losowym zadaniu
            {
                int nodeToMutate = rand.Next(g.GetNodes().Count);  
                gn = g.GetNode(nodeToMutate);
            }

            if (gn == null) return g;

            // wybór mutacji
            double chance = rand.NextDouble();
            // 70% na mutacje liczby zasobow jesli sie da
            if (TypesManager.IsTaskExecutableByManyResource(gn.TaskType) && chance <= RESOURCE_AMOUNT_MUTATE_CHANCE)
            {
                gn!.MutateMaxResourcesAmount();
            }
            else // a jesli nie to mutacja strategi
            {
                var current = gn.ResourceAllocationFunction;
                while (true)
                {
                    Func<ResourceAllocationContext, List<Resource>>? newResourceAllocationFunction = null;
                    if (gn == g.GetNode(0))
                        newResourceAllocationFunction = commonAllocationFunctions[rand.Next(0, 3)]; // pierwsze 3 przydzielają nowe zasoby
                    else if (gn.TaskType == TaskType.DEDICATED_TASK || gn.TaskType == TaskType.COMMON_DEDICATED_TASK)
                        newResourceAllocationFunction = dedicatedAllocationFunctions[rand.Next(0, dedicatedAllocationFunctions.Count)];
                    else
                        newResourceAllocationFunction = commonAllocationFunctions[rand.Next(0, commonAllocationFunctions.Count)];

                    if (current != newResourceAllocationFunction)
                    {
                        gn.SetResourceAllocationFunction(newResourceAllocationFunction);
                        break;
                    }
                }
            }

            // mutacja liczby kontrolerów
            if (rand.NextDouble() < CHANNEL_AMOUNT_MUTATE_CHANCE)
            {
                // kopie oryginałów
                List<UsedChannel> usedChannels = new(g.GetUsedChannels());
                List<int> usedChannelsAmounts = new(g.GetUsedChannelsAmounts());

                while (true)
                {
                    var ucCopy = new List<UsedChannel>(usedChannels);
                    var ucaCopy = new List<int>(usedChannelsAmounts);

                    int channelsAmountToMutate = rand.Next(usedChannelsAmounts.Count);
                    int correspondingChannel = 0;

                    for (int i = 0; i < channelsAmountToMutate; i++)
                    {
                        correspondingChannel += ucaCopy[i];
                    }

                    if (rand.NextDouble() < 0.5)
                    {
                        //Debug.WriteLine("NA_PLUS");
                        ucaCopy[channelsAmountToMutate]++;
                        ucCopy.Insert(correspondingChannel, new UsedChannel(ChannelsManager.GetNewChannel(channelsAmountToMutate)));
                    }
                    else
                    {
                        if (ucaCopy[channelsAmountToMutate] == 0)
                            continue;

                        //Debug.WriteLine("NA_MINUS");
                        ucaCopy[channelsAmountToMutate]--;
                        ucCopy.RemoveAt(correspondingChannel);
                    }

                    g.SetUsedChannels(ucCopy);
                    g.SetUsedChannelsAmounts(ucaCopy);

                    if (g.HasAllQualificationsForResourceChecks())
                        break;
                }
            }

            return new Graph(g);
        }
        public Dictionary<string, List<Graph>> Selection(List<KeyValuePair<double, Graph>> populationWithScore, int pi, int phi, int omega, int psi) 
        {
            Dictionary<string, List<Graph>> result = new();
            populationWithScore.Sort((a,b) => a.Key.CompareTo(b.Key));
            result.Add("FIRST", new()); // pierwszy osobnik zawsze zostanie wybrany do nowej populacji
            result["FIRST"].Add(new(populationWithScore[0].Value));

            //wyznaczanie prawdopodobienstwa wyboru
            List<KeyValuePair<double, Graph>> populationWithProbability = new();

            for (int r = 0; r < populationWithScore.Count; r++)
            {
                populationWithProbability.Add(new((double)(pi - r) / pi, populationWithScore[r].Value));
            }

            // przejscie przez osobniki z wybieraniem 
            Random rand = new();
            int needed = 0;
            int current = 0;
            double chance = 0;

            // krzyzowanie
            result.Add("CROSSOVER", new());
            needed = psi;
            current = 0;
            while (needed > 0)
            {
                if (current == populationWithProbability.Count)
                    current = 0;

                chance = rand.NextDouble();
                if (chance <= populationWithProbability[current].Key)
                {
                    if (!result["CROSSOVER"].Contains(populationWithProbability[current].Value))
                    {
                        result["CROSSOVER"].Add(populationWithProbability[current].Value);
                        needed--;
                    }
                }
                current += 1;
            }

            // klonowanie
            result.Add("CLONING", new());
            needed = phi;
            current = 0;
            while (needed > 0)
            {
                if (current == populationWithProbability.Count)
                    current = 0;

                chance = rand.NextDouble();
                if (chance <= populationWithProbability[current].Key)
                {
                    if (!result["CLONING"].Contains(populationWithProbability[current].Value))
                    {
                        result["CLONING"].Add(populationWithProbability[current].Value);
                        needed--;
                    }
                }
                current += 1;
            }
            // mutacja
            result.Add("MUTATION", new());
            needed = omega;
            current = 0;
            while (needed > 0)
            {
                if (current == populationWithProbability.Count)
                    current = 0;

                chance = rand.NextDouble();
                if (chance <= populationWithProbability[current].Key)
                {
                    if (!result["MUTATION"].Contains(populationWithProbability[current].Value))
                    {
                        result["MUTATION"].Add(populationWithProbability[current].Value);
                        needed--;
                    }
                }
                current += 1;
            }
            return result;
        }
        
        public Graph Cloning(Graph g) 
        {
            return new Graph(g);
        }
    }
}