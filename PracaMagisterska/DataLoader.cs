using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PracaMagisterska
{
    class DataLoader
    {
        public bool isDataValid;
        public Graph? Graph;
        public List<Resource>? Resources;
        public List<Channel>? Channels;

        public DataLoader()
        {
            isDataValid = false;
            Graph = null;
            Resources = null;
            Channels = null;
        }

        public void LoadFromFile()
        {
            isDataValid = false;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Plik tekstowy (*.txt)|*.txt";

            if (ofd.ShowDialog() == true)
            {
                string filename = ofd.FileName;
                string content = File.ReadAllText(filename);
                string[] parts = content.Split('@');

                if (parts.Length != 6)
                {
                    MessageBox.Show($"Plik \"{filename}\" nie zawiera wymaganych sekcji.");
                    return;
                }

                if (!LoadGraph(parts[1]))
                {
                    MessageBox.Show($"Plik \"{filename}\" zawiera niepoprawną definicję zadań (@tasks.");
                    return;
                }

                if (!LoadResources(parts[2]))
                {
                    MessageBox.Show($"Plik \"{filename}\" zawiera niepoprawną definicję zasobów (@proc).");
                    return;
                }

                if (!LoadTimes(parts[3]))
                {
                    MessageBox.Show($"Plik \"{filename}\" nie zawiera niepoprawną definicję czasów zadań (@times).");
                    return;
                }

                if (!LoadCosts(parts[4]))
                {
                    MessageBox.Show($"Plik \"{filename}\" nie zawiera niepoprawną definicję kosztów zadań (@cost).");
                    return;
                }

                if (!LoadComm(parts[5]))
                {
                    MessageBox.Show($"Plik \"{filename}\" zawiera niepoprawną definicję kanałów komunikacji (@comm).");
                    return;
                }

                if (!AreTasksCompletable())
                {
                    MessageBox.Show($"Wykryto przynajmniej jedno niewykonalne zadanie.");
                    return;
                }

                if (!AreControllersCompetent())
                {
                    MessageBox.Show($"Wykryto przynajmniej jedno zadanie dla którego żaden kontroler nie ma uprawnień.");
                    return;
                }
                isDataValid = true;
            }
        }

        public void LoadFromFile(string filename = "")
        {
            string content = File.ReadAllText(filename);
            string[] parts = content.Split('@');

            if (parts.Length != 6)
            {
                MessageBox.Show($"Plik \"{filename}\" nie zawiera wymaganych sekcji.");
                return;
            }

            if (!LoadGraph(parts[1]))
            {
                MessageBox.Show($"Plik \"{filename}\" zawiera niepoprawną definicję zadań (@tasks.");
                return;
            }

            if (!LoadResources(parts[2]))
            {
                MessageBox.Show($"Plik \"{filename}\" zawiera niepoprawną definicję zasobów (@proc).");
                return;
            }

            if (!LoadTimes(parts[3]))
            {
                MessageBox.Show($"Plik \"{filename}\" nie zawiera niepoprawną definicję czasów zadań (@times).");
                return;
            }

            if (!LoadCosts(parts[4]))
            {
                MessageBox.Show($"Plik \"{filename}\" nie zawiera niepoprawną definicję kosztów zadań (@cost).");
                return;
            }

            if (!LoadComm(parts[5]))
            {
                MessageBox.Show($"Plik \"{filename}\" zawiera niepoprawną definicję kanałów komunikacji (@comm).");
                return;
            }

            if (!AreTasksCompletable())
            {
                MessageBox.Show($"Wykryto przynajmniej jedno niewykonalne zadanie.");
                return;
            }

            if (!AreControllersCompetent())
            {
                MessageBox.Show($"Wykryto przynajmniej jedno zadanie dla którego żaden kontroler nie ma uprawnień.");
                return;
            }
            isDataValid = true;

        }

        private bool LoadGraph(string graphData)
        {
            string[] tokens = graphData.Split('\n');
            string tmp = string.Join(" ", tokens);
            tokens = tmp.Split(' ');
            Graph graph = new();

            try
            {
                int tasksAmount = int.Parse(tokens[1]);

                int currentToken = 2;
                for (int i = 0; i < tasksAmount; i++)
                {
                    string newNode = tokens[currentToken++];
                    graph.AddNode(newNode);

                    int neighboursAmount = int.Parse(tokens[currentToken++]);
                
                    if (neighboursAmount > 0)
                    {
                        for (int j = 0; j < neighboursAmount; j++)
                        {
                            string nextNode = tokens[currentToken++];
                            string[] split1 = nextNode.Split('(');
                            string[] split2 = split1[1].Split(')');
                            graph.AddEdge(newNode, $"{split1[0]}", int.Parse(split2[0]));
                        }
                    }
                }
                Graph = graph;
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
            
        }

        private bool LoadResources(string resourcesData) {
            Resources = new();
            Resource.ResetIDGenerator();
            string[] tokens = resourcesData.Split('\n');
            string tmp = string.Join(" ", tokens);
            tokens = tmp.Split(' ');

            try
            {
                int currentToken = 1;
                int resourcesAmount = int.Parse(tokens[currentToken++]);

                for (int i=0;i<resourcesAmount;i++)
                {
                    int resourceCost = int.Parse(tokens[currentToken++]);
                    currentToken++; // int availability -> zawsze 0, nie ograniczamy dostepnosci, pomijamy
                    ResourceType rt = (ResourceType)int.Parse(tokens[currentToken++]);
                    Resources.Add(new(rt, resourceCost));
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
        }

        private bool LoadTimes(string timesData) 
        {
            string[] tokens = timesData.Split('\n');
            string tmp = string.Join(" ", tokens);
            tokens = tmp.Split(' ');

            try
            {
                if (Graph == null || Resources == null)
                    return false;

                int currentToken = 1;

                for (int i=0;i< Graph.NodesAmount(); i++)
                {
                    for (int j = 0; j < Resources.Count; j++)
                    {
                        int time = int.Parse(tokens[currentToken++]);
                        Resources[j].AddTime(time);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
        }
        
        private bool LoadCosts(string costsData) {
            string[] tokens = costsData.Split('\n');
            string tmp = string.Join(" ", tokens);
            tokens = tmp.Split(' ');

            try
            {
                if (Graph == null || Resources == null)
                    return false;

                int currentToken = 1;

                for (int i = 0; i < Graph.NodesAmount(); i++)
                {
                    for (int j = 0; j < Resources.Count; j++)
                    {
                        int cost = int.Parse(tokens[currentToken++]);
                        Resources[j].AddCost(cost);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
        }
        
        private bool LoadComm(string commData) 
        {
            Channels = new();
            string[] tokens = commData.Split('\n');
            string tmp = string.Join(" ", tokens);
            tokens = tmp.Split(' ');

            try
            {
                int currentToken = 1;
                int channelsAmount = int.Parse(tokens[currentToken++]);

                for (int i = 0; i < channelsAmount; i++)
                {
                    string name = tokens[currentToken++];
                    int cost = int.Parse(tokens[currentToken++]);
                    double bandwidth = double.Parse(tokens[currentToken++]);

                    Channels.Add(new Channel(name, cost, bandwidth));

                    if (Resources == null) return false;

                    for (int j = 0; j < Resources.Count; j++)
                    {
                        int qual = int.Parse(tokens[currentToken++]);
                        Channels[i].qualificationsList.Add(qual);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
        }
    
        private bool AreTasksCompletable()
        {
            if (Graph == null) return false;
            if (Resources == null) return false;

            int tasksAmount = Graph.NodesAmount();

            List<bool> isTaskCompletable = new();
            for (int i = 0; i < tasksAmount; i++)
                isTaskCompletable.Add(false);

            foreach (Resource r in Resources)
            {
                for (int i=0;i<tasksAmount;i++)
                {
                    if (r.taskTime[i] != -1)
                        isTaskCompletable[i] = true;
                }
            }

            for (int i = 0; i < tasksAmount; i++)
            {
                if (!isTaskCompletable[i])
                    return false;
            }

            return true;
        }

        private bool AreControllersCompetent()
        {
            if (Resources == null) return false;
            if (Channels == null) return false;

            int resourceAmount = Resources.Count; 

            List<bool> isResourceCheckable= new();
            for (int i = 0; i < resourceAmount; i++)
                isResourceCheckable.Add(false);

            foreach (Channel c in Channels)
            {
                for (int i = 0; i < resourceAmount; i++)
                {
                    if (c.qualificationsList[i] != -1)
                        isResourceCheckable[i] = true;
                }
            }

            for (int i = 0; i < resourceAmount; i++)
            {
                if (!isResourceCheckable[i])
                    return false;
            }

            return true;
        }
    }
}
