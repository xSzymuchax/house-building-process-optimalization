using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracaMagisterska
{
    class ResourceAllocationContext
    {
        public int NodeIndex;
        public List<UsedResource>? UsedResources;
        public GraphNode? ParentNode;
        public int MaxResourcesAllocated;
        public TaskType TaskType;
    }
}
