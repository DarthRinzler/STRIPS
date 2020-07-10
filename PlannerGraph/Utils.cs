using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlannerGraph
{
    public static class Ids
    {
        private static List<string> IdToName = new List<string>();
        private static Dictionary<string, uint> NameToId = new Dictionary<string, uint>();

        private static uint s_idCounter = 1;

        public static uint GetId(string name)
        {
            uint id;
            if (NameToId.TryGetValue(name, out id))
            {
                return id;
            }
            else
            {
                id = s_idCounter++;
            }

            NameToId[name] = id;
            IdToName[(int)id] = name;
            return id;
        }

        public static string GetName(uint id)
        {
            return IdToName[(int)id];
        }
    }
}
