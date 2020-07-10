using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planner
{
    public static class Ids
    {
        public static Dictionary<uint, string> IdToName = new Dictionary<uint, string>();
        public static Dictionary<string, uint> NameToId = new Dictionary<string, uint>();

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
            IdToName[id] = name;
            return id;
        }
    }

    public static class Utils
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> THIS)
        {
            HashSet<T> ret = new HashSet<T>();
            foreach (var e in THIS) ret.Add(e);
            return ret;
        }

        public static void AddRange<T>(this HashSet<T> THIS, IEnumerable<T> toAdd)
        {
            foreach (var e in toAdd) THIS.Add(e); 
        }

        public static string GetTabs(int count)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++) sb.Append("\t");
            return sb.ToString();
        }
    }
}
