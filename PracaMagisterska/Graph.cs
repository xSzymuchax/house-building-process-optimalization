using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracaMagisterska
{
    class Graph
    {
        private List<GraphNode> _nodes;
        private Dictionary<string, List<Edge>> _edges;
        private List<UsedResource> _usedResources;
        private List<UsedChannel> _usedChannels;
        private List<int> _usedChannelsAmounts;

        private bool lastCompletedFound = false;
        private GraphNode lastCompletedNode;

        public Graph()
        {
            _nodes = new();
            _edges = new();
            _usedResources = new();
            _usedChannels = new();
            _usedChannelsAmounts = new();
        }

        public Graph(List<GraphNode> nodes, Dictionary<string, List<Edge>> edges)
        {
            _nodes = nodes;
            _edges = edges;
            _usedResources = new();
            _usedChannels = new();
            _usedChannelsAmounts = new();
        }

        public Graph(Graph other)
        {
            _nodes = new();
            foreach (var item in other.GetNodes())
                _nodes.Add(new GraphNode(item));

            _usedResources = new();
            foreach (var item in other.GetUsedResources())
                _usedResources.Add(new UsedResource(item));

            _usedChannels = new();
            foreach (var item in other.GetUsedChannels())
                _usedChannels.Add(new UsedChannel(item));

            _usedChannelsAmounts = new();
            foreach (var item in other.GetUsedChannelsAmounts())
                _usedChannelsAmounts.Add(item);

            _edges = other._edges; 
            FindNodesParents();
        }

        public void AddNode(string taskName)
        {
            _nodes.Add(new GraphNode(taskName));
            _edges.Add(taskName, new());
        }

        public List<GraphNode> GetNodes()
        {
            return _nodes;
        }

        public List<GraphNode> GetPathToRoot(GraphNode startNode)
        {
            List<GraphNode> nodes = new();
            GraphNode? n = startNode;

            while (n != null)
            {
                nodes.Add(n);
                n = n.MyParent;
                
            }

            return nodes;
        }

        public GraphNode? LastCompletedNode()
        {
            if (lastCompletedFound)
                return lastCompletedNode;
            return null;
        }

        public List<UsedResource> GetUsedResources()
        {
            return _usedResources;
        }

        public List<UsedChannel> GetUsedChannels()
        {
            return _usedChannels;
        }

        public List<int> GetUsedChannelsAmounts()
        {
            return _usedChannelsAmounts;
        }

        public void SetUsedChannelsAmounts(List<int> amounts)
        {
            _usedChannelsAmounts = amounts;
        }

        public void SetUsedChannels(List<UsedChannel> usedChannels)
        {
            _usedChannels = usedChannels;
        }

        public bool HasAllQualificationsForResourceChecks()
        {
            for (int i = 0; i < ResourcesManager.Resources.Count; i++)
            {
                bool found = false;
                foreach (UsedChannel uc in _usedChannels)
                {
                    if (uc.Channel.qualificationsList[i] == 1)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                    continue;
                return false;
            }
            return true;
        }

        public GraphNode? GetNode(int index)
        {
            if (index < _nodes.Count && index >= 0)
                return _nodes[index];
            return null;
        }

        public void ChangeNode(int index, GraphNode node)
        {
            if (index < 0 || index > _nodes.Count)
                return;
            _nodes[index] = node;
        }

        public double GetCompletionTime()
        {
            lastCompletedNode = _nodes[0];
            double max = _nodes[0].MyTaskEndTime;
            foreach (var n in _nodes)
                if (max < n.MyTaskEndTime)
                {
                    max = n.MyTaskEndTime;
                    lastCompletedNode = n;
                }

            lastCompletedFound = true;
            return max;
        }

        public double GetCompletionCost()
        {
            double costSum = 0;
            foreach (UsedResource ur in _usedResources)
                costSum += ur.GeneratedCost;

            foreach (UsedChannel uc in _usedChannels)
                costSum += uc.GeneratedCost;

            return costSum;
        }

        public List<int> BFSGetNodeIndexes(string startNodeName) 
        {
            List<int> result = new();
            Queue<string> nodesQueue = new();
            nodesQueue.Enqueue(startNodeName);
            
            string nodeName;
            while (nodesQueue.Count != 0)
            {
                nodeName = nodesQueue.Dequeue();
                result.Add(int.Parse(nodeName.Split('_')[1]));

                foreach (Edge e in _edges[nodeName])
                {
                    nodesQueue.Enqueue(e.To);
                } 
            }

            return result;
        }

        public int NodesAmount()
        {
            return _nodes.Count;
        }

        public void AddEdge(string from, string to, int label)
        {
            _edges[from].Add(new Edge()
            {
                From = from,
                To = to,
                Label = label
            });
        }

        public Dictionary<string, List<Edge>> GetEdges()
        {
            return _edges;
        }

        public void ShowGraph()
        {
            Debug.WriteLine("SHOW_GRAPH");
            foreach (var node in _nodes)
            {
                string log = $"{node.TaskName}  -> {{ ";

                foreach (Edge e in _edges[node.TaskName])
                {
                    log += $"{e.To}({e.Label}) ";
                }
                log += "}";

                Debug.WriteLine(log);
                Debug.WriteLine($"MyParent: {node.MyParent?.TaskName}\n");
            }
        }

        public void ShowUsedResources()
        {
            string log = "UsedResources:";
            foreach (UsedResource ur in _usedResources)
            {
                log += $"\nres -> {ur.GeneratedCost} {ur.GetWorkingPeriodsString()}";
            }
            Debug.WriteLine(log);
        }

        public void ShowUsedChannels()
        {
            string log = "UsedChannels:";
            foreach (UsedChannel uc in _usedChannels)
            {
                log += $"\ncha -> {uc.GeneratedCost} {uc.GetWorkingPeriodsString()}\nTasks Checked: {uc.ChekingTimes.Count}";
            }
            Debug.WriteLine(log);
        }

        public void ShowTaskResourceConnections()
        {
            Debug.WriteLine("TASK-RESOURCES");
            string t_r_log = "";
            foreach (var node in _nodes)
            {
                t_r_log += $"{node.TaskName} - {node.UsedResource.Count}\n";
            }
            Debug.WriteLine(t_r_log);
        }

        public void FindNodesParents()
        {
            List<int> result = new();
            Queue<int> nodesQueue = new();
            nodesQueue.Enqueue(0);

            int currentNode = -1 ;
            while (nodesQueue.Count != 0)
            {
                currentNode = nodesQueue.Dequeue();
                foreach (Edge e in _edges[_nodes[currentNode].TaskName])
                {
                    GraphNode node = _nodes[int.Parse(e.To.Split('_')[1])];
                    node.MyParent = _nodes[currentNode];
                    node.LabelValueFromParent = e.Label;
                    nodesQueue.Enqueue(node.TaskID);
                }
            }
        }

        // drzewo rozpinające tworzymy poprzez przejście bfs
        // chcemy żeby miało jak najmniej poziomów, co pozwoli na zwiększenie liczby jednocześnie wykonywanych zadań
        public Graph GetMinimalSpanningTree()
        {
            List<Edge> allEgdes = new();
            Dictionary<string, List<Edge>> selectedEdges = new();

            foreach (var node in _nodes)
                selectedEdges[node.TaskName] = new();

            Queue<string> queue = new();
            HashSet<string> visited = new();

            queue.Enqueue(_nodes[0].TaskName);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                foreach (Edge e in _edges[current])
                {
                    if (!visited.Contains(e.To))
                    {
                        visited.Add(e.To);
                        queue.Enqueue(e.To);
                        selectedEdges[e.From].Add(e);
                    }
                }
            }

            List<GraphNode> newNodes = new();
            foreach (GraphNode gn in _nodes)
                newNodes.Add(new GraphNode(gn));

            Graph newGraph = new Graph(newNodes, selectedEdges);
            newGraph.FindNodesParents();
            newGraph.ShowGraph();
            return newGraph;
        }

        public void ExecuteNodesAllocationFunctions() 
        {
            _usedChannels = ChannelsManager.ResetChannels(_usedChannels);

            _usedResources = new();
            UsedResource.ResetNamesIndexer();
            Queue<GraphNode> queue = new();
            queue.Enqueue(_nodes[0]);

            while (queue.Count > 0)
            {
                GraphNode gn = queue.Dequeue();
                gn.ExecuteResourceAllocationFunction(_usedResources, _usedChannels);
                foreach (var e in _edges[gn.TaskName])
                {
                    GraphNode child = _nodes.First(n => n.TaskName == e.To);
                    queue.Enqueue(child);
                }
            }
        }

        // uruchamia od momentu wykrycia nieskonczonego czasu zadania. uzywane przy naprawie osobnika
        public void ExecuteNodesAllocationFunctionsFromFirstInvalidTask()
        {
            _usedChannels = ChannelsManager.ResetChannels(_usedChannels);

            Queue<GraphNode> queue = new();
            queue.Enqueue(_nodes[0]);
            bool invalidFound = false;

            while (queue.Count > 0)
            {
                GraphNode gn = queue.Dequeue();
                
                if (gn.MyTaskEndTime == double.MaxValue)
                    invalidFound = true;

                if (invalidFound)
                    gn.ExecuteResourceAllocationFunction(_usedResources, _usedChannels);
                
                foreach (var e in _edges[gn.TaskName])
                {
                    GraphNode child = _nodes.First(n => n.TaskName == e.To);
                    queue.Enqueue(child);
                }
            }
        }
    }
}
