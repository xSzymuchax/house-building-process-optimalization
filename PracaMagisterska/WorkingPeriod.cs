using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracaMagisterska
{
    public class WorkingPeriod
    {
        public double startAt;
        public double endAt;
        public string taskName;

        public WorkingPeriod(double startAt, double endAt, string taskName)
        {
            this.startAt = startAt;
            this.endAt = endAt;
            this.taskName = taskName;
        }
    }
}
