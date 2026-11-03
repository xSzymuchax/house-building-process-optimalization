using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Msagl.Drawing;

namespace PracaMagisterska
{
    class Visualiser
    {
        PracaMagisterska.Graph dataGraph;
        Microsoft.Msagl.Drawing.Graph? drawableGraph;
        public Microsoft.Msagl.GraphViewerGdi.GViewer? viewer;

        public Visualiser(Graph graph)
        {
            this.dataGraph = graph;
        }

        public void Visualise()
        {
            TranslateToVisualisable();
            viewer?.Dispose();
            viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
            viewer.Graph = drawableGraph;
        }

        private void TranslateToVisualisable()
        {
            drawableGraph = new();

            foreach (var node in dataGraph.GetNodes())
            {
                drawableGraph.AddNode(node.TaskName);
                Color c = TypesManager.FindTaskColor(node.TaskName);
                var n = drawableGraph.FindNode(node.TaskName);
                n.Attr.FillColor = c;
                n.Attr.Shape = Shape.Circle;
            }

            foreach (var edgeList in dataGraph.GetEdges().Values)
            {
                foreach (var edge in edgeList)
                {
                    drawableGraph.AddEdge(edge.From, edge.To).LabelText = edge.Label.ToString();
                }
            }
        }
    }
}
