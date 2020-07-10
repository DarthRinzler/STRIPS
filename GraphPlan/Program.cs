using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Priority_Queue; 

namespace Planner {

    class Program
    {
        static void Main(string[] args)
        {
            Parser p = new Parser("data");

            var actions = p.ParseActionFile("worldActions.txt");
            var start = p.ParseState("worldStart.txt");
            var end = p.ParseState("worldEnd.txt");

            //Cmd(start, actions.Values);
            
            var plan = FindPlan(start, end, actions.Values);

            /*
            foreach (var action in plan)
            {
                Console.WriteLine(action);
            }
            */
        }

        private static IEnumerable<Action> FindPlan(State start, State end, IEnumerable<ActionDef> actionDefs)
        {
            StateScorer scorer = new StateScorer(start, end, actionDefs);
            HashSet<State> visitedStates = new HashSet<State>();

            // Breads first search on child states 
            FastPriorityQueue<StateNode> bfsQueue = new FastPriorityQueue<StateNode>(2000000);

            var initialNode = new StateNode(start, null, null, scorer);
            bfsQueue.Enqueue(initialNode, initialNode.Priority);
            while(bfsQueue.Any())
            {
                StateNode current = bfsQueue.Dequeue();

                var availableActions = current.State
                    .GetAllActions(actionDefs)
                    .ToArray();

                var childrenNodes = availableActions
                    .Select(action => new StateNode(current.State.ApplyAction(action), current, action, scorer))
                    .Where(stateNode => !visitedStates.Contains(stateNode.State));

                foreach (var childNode in childrenNodes)
                {
                    if (childNode.State.SatisfiesState(end))
                    {
                        return (childNode.TracePath());
                    }

                    visitedStates.Add(childNode.State);
                    bfsQueue.Enqueue(childNode, childNode.Priority);
                }
            }

            return null;
        }

        private static void Cmd(State current, IEnumerable<ActionDef> actions)
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

    public class StateNode : FastPriorityQueueNode, IComparable<StateNode>
    { 
        public State State { get; } 
        public Action ParentAction { get; }
        public StateNode ParentNode { get; }

        public StateNode(State state, StateNode parentNode, Action parentAction, StateScorer scorer)
        {
            State = state;
            Priority = scorer.GetStateScore(state);
            ParentNode = parentNode;
            ParentAction = parentAction;
        }

        public IEnumerable<Action> TracePath()
        {
            return BackTrackPath().Reverse();
        }

        private IEnumerable<Action> BackTrackPath()
        {
            StateNode current = this;
            while(current.ParentAction != null)
            {
                yield return current.ParentAction;
                current = current.ParentNode;
            }
        }

        public int CompareTo(StateNode other)
        {
            return this.Priority.CompareTo(other.Priority);
        }
    }
}
