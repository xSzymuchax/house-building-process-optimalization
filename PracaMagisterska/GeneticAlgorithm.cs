using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace PracaMagisterska
{
    class GeneticAlgorithm
    {
        private Graph StartingTree;

        private int pi;
        private int alpha;          // wielkosc populacji Pi = n*z*a
        private int n;              // liczba zadań
        private int z;              // liczba zasobów

        private int psi;            // liczba osobników wybranych do krzyżowania (pi * beta)
        private double beta;        // (0,1) - parametr cząstkowy do wyboru osobników 

        private int omega;          // liczba osobników wybranych do mutacji (pi * gamma)
        private double gamma;       // (0,1) - parametr cząstkowy do wyboru osobników 

        private int phi;            // liczba osobników wybieranych do klonowania (pi * delta)
        private double delta;       // (0,1) - parametr cząstkowy do wyboru osobników 

        private double cw;          // waga czasu
        private double kw;          //waga kosztu

        private double penalty_weight;

        private int costRestriction;
        private int timeRestriction;
        private bool isCostRestricted;
        private bool isTimeRestricted;

        private int maxIterations;
        private bool isIterationsRestricted;

        private int maxNoImprovementIterations;
        private bool isNoImprovementRestricted;

        private int timesIterated = 0;


        public GeneticAlgorithm(Graph startingTree)
        {
            StartingTree = startingTree;
            n = startingTree.GetNodes().Count;
            z = ResourcesManager.Resources.Count;
            isCostRestricted = false;
            isTimeRestricted = false;
            isIterationsRestricted = false;
            isNoImprovementRestricted = false;
        }

        public void LoadParameters(int alpha, double beta, double gamma, double delta, double cw, double kw, double penalty)
        {
            this.alpha = alpha;
            this.beta = beta;
            this.gamma = gamma;
            this.delta = delta;
            this.cw = cw;
            this.kw = kw;
            pi = n * z * this.alpha;
            psi = (int)(pi * this.beta);
            omega = (int)(pi * this.gamma);
            phi = (int)(pi * this.delta);
            penalty_weight = penalty;
        }

        public void SetCostRestrictions(int amount)
        {
            isCostRestricted = true;
            costRestriction = amount;
        }

        public void SetTimeRestrictions(int amount)
        {
            isTimeRestricted = true;
            timeRestriction = amount;
        }

        public void SetMaxIterations(int amount)
        {
            isIterationsRestricted = true;
            maxIterations = amount;
        }

        public void SetMaxNoImprovementIterations(int amount)
        {
            isNoImprovementRestricted = true;
            maxNoImprovementIterations = amount;
        }

        public int GetTimesIterated()
        {
            return timesIterated;
        }

        public Graph StartProcessing()
        {
            GeneticOperators go = new();
            List<Func<ResourceAllocationContext, List<Resource>?>> commonAllocationFunctions =
                [NewFastestResource,
                NewChepestResource,
                NewTimeCostFactorResource,
                ExistingFastestResource,
                ExistingCheapestResource,
                ExistingTimeCostFactorResource,
                ExistingFastestFreeResource,
                ExistingLeastAllocatedResource,
                SameAsParentResource
                ];

            List<Func<ResourceAllocationContext, List<Resource>?>> dedicatedAllocationFunctions =
                [NewFastestResource,
                 NewChepestResource,
                 NewTimeCostFactorResource,
                 SameAsParentResource
            ];



            // populacja (czysta)
            List<Graph> population = new();
            for (int i = 0; i < pi; i++)
            {
                population.Add(new Graph(StartingTree));
            }

            // dodanie funkcji wyboru zasobu
            Debug.WriteLine("PRZYDZIELANIE_FUNKCJI_WYBORU_ZASOBÓW");
            Random rand = new();
            foreach (Graph g in population)
                FixIndividual(g, commonAllocationFunctions!, dedicatedAllocationFunctions!);

            Debug.WriteLine("SPRAWDZENIE_CZY_OSOBNIKI_LEGALNE");
            foreach (Graph g in population)
                Debug.WriteLine(CalculateTime(g));


            int currentIteration = 1;
            int timesWithoutProgression = 0;
            double bestScore = double.MaxValue;
            double lastBestScore = double.MaxValue;
            
            while (true)
            {
                if (isIterationsRestricted && currentIteration >= maxIterations)
                {
                    Debug.WriteLine("MAKSYMALNY_LIMIT_ITERACJI - KONCZE");
                    timesIterated = currentIteration;
                    break;
                }

                if (isNoImprovementRestricted && timesWithoutProgression >= maxNoImprovementIterations)
                {
                    Debug.WriteLine("LIMIT_ITERACJI_BEZ_POPRAWY - KONCZE");
                    timesIterated = currentIteration;
                    break;
                }


                //Debug.WriteLine($"////////////////////////////\nPOPULATION: {currentIteration}");
                //Debug.WriteLine($"////////////////////////////\nPOPULATION SIZE: {population.Count}");

                // wykonanie kolejnych funkcji wyboru zasobu (BFS) 
                foreach (Graph g in population)
                    g.ExecuteNodesAllocationFunctions();

                // ocena 
                List<KeyValuePair<double, Graph>> populationWithScore = AdaptationScore(population);
                bestScore = populationWithScore.MinBy(p => p.Key).Key;
                double deltaScore = lastBestScore - bestScore;

                if (deltaScore == 0)
                    timesWithoutProgression++;
                else
                {
                    timesWithoutProgression = 0;
                    Debug.WriteLine($"////////////////////////////\nPOPULATION: {currentIteration}");
                    Debug.WriteLine($"DELTA_SCORE: {deltaScore}");
                    var min = populationWithScore.MinBy(p => p.Key);
                    var max = populationWithScore.MaxBy(p => p.Key);
                    Debug.WriteLine($"MIN: {min.Key}");
                    Debug.WriteLine($"MAX: {max.Key}");
                } 


                // wybór osobników
                Dictionary<string, List<Graph>> selected = go.Selection(populationWithScore, pi, phi, omega, psi);

                // towrzenie nowej populacji
                List<Graph> newPopulation = new();
                int crossed = psi;
                while (crossed >= 2) 
                {
                    Graph parent1 = selected["CROSSOVER"][rand.Next(selected["CROSSOVER"].Count)];
                    Graph parent2 = selected["CROSSOVER"][rand.Next(selected["CROSSOVER"].Count)];
                    var children = go.Crossover(parent1, parent2);
                    newPopulation.Add(children[0]);
                    newPopulation.Add(children[1]);

                    selected["CROSSOVER"].Remove(parent1);
                    selected["CROSSOVER"].Remove(parent2);
                    crossed-=2;
                }

                selected["MUTATION"].ForEach(g => newPopulation.Add(go.Mutation(g, commonAllocationFunctions!, dedicatedAllocationFunctions!)));
                selected["CLONING"].ForEach(g => newPopulation.Add(g));
                newPopulation.Add(selected["FIRST"][0]);

                population = newPopulation;

                // tworzenie nowej populacji
                //Debug.WriteLine($"POPULATION_SIZE: {population.Count}");
                lastBestScore = bestScore;
                currentIteration++;
            }

            foreach (Graph g in population)
            {
                g.ExecuteNodesAllocationFunctions();
                // g.ShowUsedResources();
                //Debug.WriteLine($"COST: {CalculateCost(g)}");
                //Debug.WriteLine($"TIME: {CalculateTime(g)}");
            }

            Debug.WriteLine("//////////////////////////////////////////////Final scores!///////////////////////////////////////");
            List<KeyValuePair<double, Graph>> populationWithScores = AdaptationScore(population);
            populationWithScores.OrderBy(pws => pws.Key).ToList().ForEach(pws => Debug.WriteLine($"{pws.Key}"));

            

            Debug.WriteLine($"BEST IS: ");
            var result = populationWithScores.MinBy(pws => pws.Key).Value;
            population[0].ShowUsedResources();
            //population[0].ShowUsedChannels();
            //population[0].ShowTaskResourceConnections();
            Debug.WriteLine($"COST: {CalculateCost(result)}");
            Debug.WriteLine($"TIME: {CalculateTime(result)}");

            Debug.WriteLine($"WORST IS: ");
            var result2 = populationWithScores.MaxBy(pws => pws.Key).Value;
            Debug.WriteLine($"COST: {CalculateCost(result2)}");
            Debug.WriteLine($"TIME: {CalculateTime(result2)}");

            return result;
        }

        public List<KeyValuePair<double, Graph>> AdaptationScore(List<Graph> population) 
        {
            List<KeyValuePair<double, Graph>> result = new();
            List<double> times = new();
            List<double> costs = new();

            // znajdz czasy i koszty dla grafow
            foreach (Graph g in population)
            {
                costs.Add(CalculateCost(g));
                times.Add(CalculateTime(g));
            }

            // wyliczanie wartosci funkcji przystosowania
            for (int i = 0; i < population.Count; i++)
            {
                double normCost = isCostRestricted ? costs[i] / costRestriction : costs[i];
                double normTime = isTimeRestricted ? times[i] / timeRestriction : times[i];
                double score = kw * normCost + cw * normTime;

                if (isCostRestricted && normCost > 1)
                {
                    score *= (1 + penalty_weight * Math.Pow((normCost - 1), 2));
                    score += 1e6;
                }


                if (isTimeRestricted && normTime > 1)
                {
                    score *= (1 + penalty_weight * Math.Pow((normTime - 1), 2));
                    score += 1e6;
                }

                result.Add(new(score, population[i]));
            }

            return result;
        }

        private double CalculateCost(Graph g) 
        {
            return g.GetCompletionCost();
        }

        private double CalculateTime(Graph g) 
        {
            return g.GetCompletionTime();
        }

        private void FixIndividual(Graph g, List<Func<ResourceAllocationContext, List<Resource?>>> commonAllocationFunctions, List<Func<ResourceAllocationContext, List<Resource?>>> dedicatedAllocationFunctions)
        {
            int r = 0;
            Random rand = new();

            // bazowe przypisanie kontrolerow (kazdy po 1)
            List<UsedChannel> usedChannels = ChannelsManager.GetEveryChannel();
            List<int> usedChannelsAmounts = new();
            usedChannels.ForEach(uc => usedChannelsAmounts.Add(1));
            g.SetUsedChannels(usedChannels);
            g.SetUsedChannelsAmounts(usedChannelsAmounts);


            // przypisywanie funkcji konstrukcyjnych
            while (CalculateTime(g) == double.MaxValue)
            {
                foreach (GraphNode gn in g.GetNodes())
                {
                    if (gn.MyTaskEndTime == double.MaxValue)
                    {
                        if (gn.TaskID == 0)
                        {
                            r = rand.Next(0, 3); // pierwsze 3 funkcje to przydział nowego zasobu
                            gn.SetResourceAllocationFunction(commonAllocationFunctions[r]!);
                        }

                        else if (gn.TaskType == TaskType.DEDICATED_TASK || gn.TaskType == TaskType.COMMON_DEDICATED_TASK)
                        {
                            r = rand.Next(0, dedicatedAllocationFunctions.Count);
                            gn.SetResourceAllocationFunction(dedicatedAllocationFunctions[r]!);
                        }
                        else
                        {
                            r = rand.Next(0, commonAllocationFunctions.Count);
                            gn.SetResourceAllocationFunction(commonAllocationFunctions[r]!);
                        }
                    }
                }

                g.ExecuteNodesAllocationFunctionsFromFirstInvalidTask(); 
            }
            
        }
        
        public List<Resource>? NewFastestResource(ResourceAllocationContext ctx) 
        {
            return ResourcesManager.FindFastestResource(ctx.NodeIndex, ctx.MaxResourcesAllocated, ctx.TaskType);
        }

        public List<Resource>? ExistingFastestResource(ResourceAllocationContext ctx) 
        {
            return ResourcesManager.FindFastestExistingResource(ctx.NodeIndex, ctx.MaxResourcesAllocated, ctx.UsedResources!, ctx.TaskType);
        }

        public List<Resource>? NewChepestResource(ResourceAllocationContext ctx)
        {
            return ResourcesManager.FindCheapestResource(ctx.NodeIndex, ctx.MaxResourcesAllocated, ctx.TaskType);
        }

        public List<Resource>? ExistingCheapestResource(ResourceAllocationContext ctx)
        {
            return ResourcesManager.FindExistingCheapestResource(ctx.UsedResources!, ctx.NodeIndex, ctx.MaxResourcesAllocated, ctx.TaskType);
        }

        public List<Resource>? NewTimeCostFactorResource(ResourceAllocationContext ctx)
        {
            return ResourcesManager.FindBestTimeCostFactorResource(ctx.NodeIndex, ctx.MaxResourcesAllocated, ctx.TaskType);
        }

        public List<Resource>? ExistingTimeCostFactorResource(ResourceAllocationContext ctx)
        {
            return ResourcesManager.FindExistingTimeCostFactorResource(ctx.UsedResources!, ctx.NodeIndex, ctx.TaskType, ctx.MaxResourcesAllocated);
        }

        public List<Resource>? SameAsParentResource(ResourceAllocationContext ctx)
        {
            return ResourcesManager.SameResourceAsParent(ctx.NodeIndex, ctx.UsedResources!, ctx.ParentNode!, ctx.TaskType);
        }

        public List<Resource> ExistingLeastAllocatedResource(ResourceAllocationContext ctx)
        {
            return ResourcesManager.FindExistingLeastAllocatedResource(ctx.UsedResources!, ctx.NodeIndex, ctx.TaskType, ctx.MaxResourcesAllocated);
        }

        public List<Resource> ExistingFastestFreeResource(ResourceAllocationContext ctx)
        {
            return ResourcesManager.FindExistingFastestFreeResource(ctx.UsedResources!, ctx.MaxResourcesAllocated, ctx.TaskType);
        }

    }
}
