using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPlan {

    class Program
    {
        static void Main(string[] args)
        {
            Parser p = new Parser();

            p.ParseActions("data/BaseActions.txt", false);
            p.ParseActions("data/worldActions.txt");

            var start = p.ParseState("data/worldStart.txt");
            var end = p.ParseState("data/worldEnd.txt");

            Cmd(start, p.AllActions.Values);
            
            var plan = FindPlan(start, end, p.AllActions.Values);

            foreach (var action in plan)
            {
                Console.WriteLine(action);
            }
        }

        private static IEnumerable<Action> FindPlan(State start, State end, IEnumerable<ActionDefinition> actionDefs)
        {
            NodeScorer scorer = new NodeScorer(start, end, actionDefs);
            HashSet<State> visitedStates = new HashSet<State>();

            // Breads first search on child states 
            Queue<Node> bfsQueue = new Queue<Node>();
            bfsQueue.Enqueue(new Node(start, null, null));
            while(bfsQueue.Any())
            {
                Node current = bfsQueue.Dequeue();

                var availableActions = current.State
                    .GetAllActions(actionDefs);

                var childrenNodes = availableActions
                    .Select(action => new Node(current.State.ApplyAction(action), current, action))
                    .Where(node => !visitedStates.Contains(node.State))
                    .OrderByDescending(scorer.GetNodeScore);

                foreach (var childNode in childrenNodes)
                {
                    if (childNode.State.SatisfiesState(end))
                    {
                        return (childNode.TracePath());
                    }

                    visitedStates.Add(childNode.State);
                    bfsQueue.Enqueue(childNode);
                }
            }

            return null;
        }

        private static void Cmd(State current, IEnumerable<ActionDefinition> actions)
        {
            while(true)
            {
                var availableActions = current 
                    .GetAllActions(actions)
                    .ToList();

                Console.WriteLine("Enter Action:");

                // Read Action
                var input = Console.ReadLine().Trim();
                if (input == "a")  // a prints all available actions
                {
                    foreach (var a in availableActions)
                    {
                        Console.WriteLine(a.ToString());
                    }
                }
                // Print state
                else if (input == "s")
                {
                    Console.WriteLine(current.ToString()); 
                }
                // Find and Apply Action
                else
                {
                    // Find Action
                    var action = availableActions.FirstOrDefault(a => a.ToString().Equals(input, StringComparison.InvariantCultureIgnoreCase));
                    if (action == null)
                    {
                        Console.WriteLine($"Unable to find action {input}");
                        continue;
                    }

                    // Apply Action
                    current = current.ApplyAction(action);
                }
            }
        }
    }

    public class Node
    { 
        public State State { get; } 
        public Action ParentAction { get; }
        public Node ParentNode { get; }

        public Node(State state, Node parentNode, Action parentAction)
        {
            State = state;
            ParentNode = parentNode;
            ParentAction = parentAction;
        }

        public IEnumerable<Action> TracePath()
        {
            return BackTrackPath().Reverse();
        }

        private IEnumerable<Action> BackTrackPath()
        {
            Node current = this;
            while(current.ParentAction != null)
            {
                yield return current.ParentAction;
                current = current.ParentNode;
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
