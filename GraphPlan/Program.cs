using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPlan {

    class PropositionLayer
    {
        public Dictionary<UInt64, PropositionNode> Propositions { get; set; }        

        public PropositionLayer()
        {
            Propositions = new Dictionary<UInt64, PropositionNode>();
        }
    }

    class ActionLayer
    {
        public List<ActionNode> Actions { get; set; }

        public ActionLayer()
        {
            Actions = new List<ActionNode>();
        }
    }

    class PropositionNode
    {
        public Proposition Proposition { get; set; }
        public bool Truth { get; set; }
        public List<ActionNode> ActionEdges { get; set; }
        public List<PropositionNode> MutexPropositions { get; set; }

        public PropositionNode(Proposition p, bool truth=true)
        {
            Proposition = p;
            Truth = truth;
            ActionEdges = new List<ActionNode>();
            MutexPropositions = new List<PropositionNode>();
        }
    }

    class ActionNode
    {
        public Action Action { get; set; }
        public List<PropositionNode> PropositionEdges { get; set; }
        public List<ActionNode> MutexActions { get; set; }

        public ActionNode(Action a)
        {
            Action = a;
            PropositionEdges = new List<PropositionNode>();
            MutexActions = new List<ActionNode>();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Parser p = new Parser();
            State start = p.ParseState("data/TicTacStart.txt");
            State end = p.ParseState("data/TicTacEnd.txt");
            var actions = p.ParseActions("data/TicTacActions.txt");

            Search(start, end, actions);
            //Run(state, actionDefs);
        }

        private static void Search(State start, State end, Dictionary<string, ActionDefinition> actionDefinitions)
        {
            var propositions = start.Propositions.ToDictionary(p => p.Key, p => new PropositionNode(p.Value));
            var actionNodes = start
                .GetAllActions(actionDefinitions.Values.ToList())
                .Select(action => new ActionNode(action));

            // Foreach action, get all dependent propositions, and link them
            foreach (var actionNode in actionNodes)
            {
                var action = actionNode.Action;

                // Get Positive Proposition Nodes from Action
                var positivePreconditions = action.Definition.PositivePre
                    .Select(propositionDefinition => propositionDefinition.GetProposition(action.Parameters).Id)
                    .Select(id => propositions[id]);

                // Get Negative Proposition Nodes from Action
                var negativePreconditions = action.Definition.NegativePre
                    .Select(propositionDefinition => propositionDefinition.GetProposition(action.Parameters))
                    .Select(negProp =>
                    {
                        // Add Negative Proposition Nodes if not existing
                        if (!propositions.ContainsKey(negProp.Id))
                        {
                            propositions[negProp.Id] = new PropositionNode(negProp, false);
                        }
                        return propositions[negProp.Id];
                    });

                // Merge positive and negative preconditions
                var dependentPropositions = positivePreconditions.Concat(negativePreconditions);

                // Link each Proposition with current Action Node
                foreach (var pp in dependentPropositions)
                {
                    pp.ActionEdges.Add(actionNode);
                }

            }
        }

        private static void Run(State state, Dictionary<string, ActionDefinition> actionDefs)
        {
            while (true)
            {
                Console.Write(">");
                var cmd = Console.ReadLine().Trim().ToLower();
                var actionName = cmd.Split(' ')[0];
                var actionParams = cmd.Split(' ').Skip(1);

                if (actionParams.Any(ap => !Ids.NameToId.ContainsKey(ap)))
                {
                    Console.WriteLine("Invalid!");
                    continue;
                }

                // Apply Action
                if (actionDefs.ContainsKey(actionName))
                {

                    var ad = actionDefs[actionName];
                    var parameters = actionParams
                        .Select(ap => Ids.NameToId[ap])
                        .ToArray();

                    var actionInst = new Action(ad, parameters);

                    if (state.SatisfiesPrecondition(actionInst))
                    {
                        state = state.ApplyAction(actionInst);
                    }
                    else
                    {
                        Console.WriteLine("Cannot Apply Action");
                    }
                }
                // Print out State
                else if (cmd == "state")
                {
                    Console.WriteLine(state);
                }
                // Else print out all available actions
                else
                {
                    var actions = state
                        .GetAllActions(actionDefs.Values.ToList())
                        .ToList();

                    foreach (var a in actions)
                    {
                        Console.WriteLine(a.ToString());
                        if (!state.SatisfiesPrecondition(a))
                        {
                            Console.WriteLine("Invalid Action: " + a);
                        }
                    }
                }
            }
        }
    }

    public static class Ids {
        public static Dictionary<uint, string> IdToName = new Dictionary<uint, string>();
        public static Dictionary<string, uint> NameToId = new Dictionary<string, uint>();

        private static uint s_idCounter = 1;

        public static uint GetId(string name) {
            uint id;
            if (NameToId.TryGetValue(name, out id)) {
                return id;
            }
            else {
                id = s_idCounter++;
            }

            NameToId[name] = id;
            IdToName[id] = name;
            return id;
        }
    }
}
