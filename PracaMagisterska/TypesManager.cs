using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Msagl.Drawing;

namespace PracaMagisterska
{
    public enum ResourceType { COMMON = 1, DEDICATED = 0 }

    public enum TaskType
    {
        DEDICATED_TASK = 0,
        GENERAL_TASK = 1,
        UNIVERSAL_TASK = 2,
        COMMON_DEDICATED_TASK = 3,
        COMMON_GENERAL_TASK = 4
    }

    public static class TypesManager
    {
        public static bool IsTaskExecutableByResource(ResourceType rt, TaskType tt)
        {
            if (rt == ResourceType.DEDICATED)
            {
                if (tt == TaskType.GENERAL_TASK || tt == TaskType.COMMON_GENERAL_TASK || tt == TaskType.COMMON_DEDICATED_TASK || tt == TaskType.DEDICATED_TASK)
                    return true;
            }

            if (rt == ResourceType.COMMON)
            {
                if (tt == TaskType.GENERAL_TASK || tt == TaskType.COMMON_GENERAL_TASK || tt == TaskType.UNIVERSAL_TASK)
                    return true;
            }

            return false;
        }

        public static bool IsTaskExecutableByManyResource(TaskType tt)
        {
            if (tt == TaskType.COMMON_DEDICATED_TASK || tt == TaskType.COMMON_GENERAL_TASK)
                return true;
            return false;
        }

        public static TaskType FindTaskType(string taskName)
        {
            string[] split = taskName.Split('_');

            switch (split[0])
            {
                case "DT":
                    return TaskType.DEDICATED_TASK;
                case "GT":
                    return TaskType.GENERAL_TASK;
                case "UT":
                    return TaskType.UNIVERSAL_TASK;
                case "CDT":
                    return TaskType.COMMON_DEDICATED_TASK;
                case "CGT":
                    return TaskType.COMMON_GENERAL_TASK;
                default:
                    throw new Exception($"Non-existent task type: {split[0]}.");
            }
        }

        public static Color FindTaskColor(string taskName)
        {
            string[] split = taskName.Split('_');

            switch (split[0])
            {
                case "DT":
                    return Color.Orange;
                case "GT":
                    return Color.Green;
                case "UT":
                    return new Color(123, 197, 219);
                case "CDT":
                    return new Color(89, 89, 157);
                case "CGT":
                    return Color.Yellow;
                default:
                    throw new Exception($"Non-existent task type: {split[0]}.");
            }
        }
    }
}
