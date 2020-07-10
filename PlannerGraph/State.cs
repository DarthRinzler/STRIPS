using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlannerGraph
{
    public class State
    {
        private HashSet<Fact> Facts { get; }
        private Lazy<Dictionary<uint, Fact[]>> A { get; }
        private Lazy<Dictionary<uint, Fact[]>> Rel { get; }
        private Lazy<Dictionary<uint, Fact[]>> B { get; }

        public State(HashSet<Fact> facts)
        {
            Facts = facts;
            A = new Lazy<Dictionary<uint, Fact[]>>(() => Facts
                .GroupBy(f => f.BId)
                .ToDictionary(g => g.Key, g => g.ToArray()));

            Rel = new Lazy<Dictionary<uint, Fact[]>>(() => Facts 
                .GroupBy(f => f.RelId)
                .ToDictionary(g => g.Key, g => g.ToArray()));

            B = new Lazy<Dictionary<uint, Fact[]>>(() => Facts
                .GroupBy(f => f.BId)
                .ToDictionary(g => g.Key, g => g.ToArray()));
        }

        public State()
            : this(new HashSet<Fact>())
        { }

        public void AddFact(Fact f)
        {
            Facts.Add(f);
        }

        public State ApplyAction(ActionInst action)
        {
            var newFacts = new HashSet<Fact>(Facts);

            // Verify preconditions
            foreach (var preCondition in action.Definition.Pre)
            {
                var fact = preCondition.Evaluate();
                if (preCondition.Negated)
                {
                    if (Facts.Contains(fact)) throw new Exception($"Cannot apply action {action.Definition.Name}: '{fact}' should be FALSE");
                }
                else
                {
                    if (!Facts.Contains(fact)) throw new Exception($"Cannot apply action {action.Definition.Name}: '{fact}' should be TRUE");
                }
            }

            // Set postConditions
            foreach (var postCondition in action.Definition.Post)
            {
                var fact = postCondition.Evaluate();
                if (postCondition.Negated)
                {
                    newFacts.Remove(fact);
                }
                else
                {
                    newFacts.Add(fact);
                }
            }

            return new State(newFacts);
        }

        public override string ToString()
        {
            return String.Join("\n", Facts.Select(f => f.ToString()));
        }
    }
}
