using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPlan
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser p = new Parser();
            var state = p.ParseState("data/obj1.txt");
            var actionDefs = p.ParseActions("data/act1.txt");

            var t = state
                .GetAllActions(actionDefs.Values.ToList())
                .ToList();

            ActionInst a = new ActionInst(actionDefs["move"], new uint[] {
                Ids.NameToId["player"],
                Ids.NameToId["home"],
                Ids.NameToId["road"],
            });

            var state2 = state.ApplyAction(a);

            Console.WriteLine(state.ToString());
        }
    }

    public static class Ids
    {
        public static Dictionary<uint, string> IdToName = new Dictionary<uint, string>();
        public static Dictionary<string, uint> NameToId = new Dictionary<string, uint>();

        private static uint s_idCounter = 1;

        public static uint GetId(string name)
        {
            uint id;
            if(NameToId.TryGetValue(name, out id))
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
}
