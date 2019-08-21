using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSimple
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    public class Proposition
    {
        public ulong Id { get; private set; }
        public string Name { get; private set; }

        private static ulong counter;
        private static Dictionary<string, ulong> s_nameToId = new Dictionary<string, ulong>();
        private static Dictionary<ulong, string> s_idToName = new Dictionary<ulong, string>();

        public Proposition(string name)
        {
            Name = name;
            
            if (!s_nameToId.ContainsKey(name))
            {
                ulong id = ++counter;
                s_nameToId[name] = id;
                s_idToName[id] = name;
            }

            Id = s_nameToId[name];
        }
    }

    public class Action
    {
        public string Name { get; private set; }
    }

    public class PropositionNode
    {
        public Proposition Proposition { get; private set; }
        public PropositionLayer Layer { get; private set; }
        public bool IsTrue { get; private set; }

        public Dictionary<string, ActionNode> Actions { get; private set; }
    }

    public class ActionNode
    {
        public Action Action { get; private set; }
        public ActionLayer Layer { get; private set; }
    }


    public class PropositionLayer
    {
        public Dictionary<ulong, Proposition> Propositions { get; private set; }

        public PropositionLayer(Dictionary<ulong, Proposition> propositions)
        {
            Propositions = propositions;
        }
    }

    public class ActionLayer
    {

    }
}
