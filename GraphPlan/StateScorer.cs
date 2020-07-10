using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Planner
{
    public class StateScorer
    {
        private State _endState;
        private State _startState;
        private IEnumerable<ActionDef> _actionDefs;
        private Dictionary<ulong, Fact> _variables;

        private List<VariableRelationNode> _rootTree;
        public static Dictionary<string, VariableRelationNode> s_visitedRelations;
        public static HashSet<ActionDefNode> s_visitedActionDefs;
        public static ulong MaxScore = 100000;

        public StateScorer(State start, State end, IEnumerable<ActionDef> actionDefs)
        {
            _endState = end;
            _startState = start;
            _actionDefs = actionDefs;
            _variables = new Dictionary<ulong, Fact>();
            s_visitedActionDefs = new HashSet<ActionDefNode>(new ActionDefNode());
            s_visitedRelations = new Dictionary<string, VariableRelationNode>(new VariableRelationNode());

            _rootTree = _endState.Facts.Values
                .Select(pd => FactToRelationNode(pd, actionDefs, 25))
                .ToList();

            foreach (var node in _rootTree)
            {
                node.Print(0);
            }
        }

        private VariableRelationNode FactToRelationNode(Fact f, IEnumerable<ActionDef> actionDefs, int searchDepth)
        {
            var a = new BoundVariable(f.AId);
            var rel = new BoundVariable(f.RelId);
            var b = new BoundVariable(f.BId);
            var pd = new VariableRelation(a, rel, b, false);

            return new VariableRelationNode(pd, actionDefs, searchDepth);
        }

        public int GetStateScore(State state)
        {
            return _rootTree.Sum(n => n.GetScoreRec(state));
        }
    }

    public class VariableRelationNode : IEqualityComparer<VariableRelationNode>
    { 
        public VariableRelation TargetVariableRelation { get; }
        public List<ActionDefNode> DependentActions { get; private set; }

        private IEnumerable<ActionDef> _actionDefs;

        public VariableRelationNode()
        { }

        public VariableRelationNode(VariableRelation rel, IEnumerable<ActionDef> actionDefs, int maxSearchDepth)
        {
            TargetVariableRelation = rel;
            _actionDefs = actionDefs;

            // If this relation has already been explored
            if (StateScorer.s_visitedRelations.Contains(this))
            {
                this.DependentActions = StateScorer.s_visitedRelations.
                // Dont expore this relation as it has already been explored
                return;
            }
            // If this relation has NOT been explored, explore it
            else
            {
                StateScorer.s_visitedRelations.Add(this);

                if (maxSearchDepth> 0)
                {
                    DependentActions = GetDependentActionDefs(maxSearchDepth - 1).ToList();
                }
            }
        }

        public int GetScoreRec(State state)
        {
            if (TargetVariableRelation.IsFullyBound) 
            {
                var f = TargetVariableRelation.ToFact().Value;
                var found = state.Facts.ContainsKey(f.Id);
                if (TargetVariableRelation.Negated ^ found)
                {
                    int ret = 0;
                    Console.WriteLine($"{TargetVariableRelation}:{ret}");
                    return ret;
                }
            }

            if (DependentActions != null && DependentActions.Any())
            {
                int ret = 1 + DependentActions.Sum(da => da.GetScoreRec(state));
                Console.WriteLine($"{TargetVariableRelation}:{ret}");
                return ret;
            }
            else
            {
                int ret = 1;
                Console.WriteLine($"{TargetVariableRelation}:{ret}");
                return ret;
            }

        }

        private IEnumerable<ActionDefNode> GetDependentActionDefs(int searchDepth)
        {
            var dependentActions = new List<ActionDef>();

            foreach (ActionDef actionDef in _actionDefs)
            {
                var varToLitMap = new Dictionary<UnboundVariable, BoundVariable>();

                foreach (VariableRelation postCondVariableRelation in actionDef.PositivePostconditions)
                {
                    // If this action can produce the target, add rebinds for new action
                    if (IsDependentRelation(TargetVariableRelation, postCondVariableRelation))
                    {
                        if (ShouldBindFreeVar(TargetVariableRelation.A, postCondVariableRelation.A))
                        {
                            varToLitMap.Add((UnboundVariable)postCondVariableRelation.A, (BoundVariable)TargetVariableRelation.A);
                        }
                        if (ShouldBindFreeVar(TargetVariableRelation.Rel, postCondVariableRelation.Rel))
                        {
                            varToLitMap.Add((UnboundVariable)postCondVariableRelation.Rel, (BoundVariable)TargetVariableRelation.Rel);
                        }
                        if (ShouldBindFreeVar(TargetVariableRelation.B, postCondVariableRelation.B))
                        {
                            varToLitMap.Add((UnboundVariable)postCondVariableRelation.B, (BoundVariable)TargetVariableRelation.B);
                        }
                    }
                }

                if (varToLitMap.Any())
                {
                    ActionDef newAction = actionDef.BindVariables(varToLitMap);
                    dependentActions.Add(newAction);
                }
            }

            if (dependentActions.Any())
            {
                return dependentActions
                    .Select(da => new ActionDefNode(da, _actionDefs, searchDepth - 1));
            }
            else
            {
                return Enumerable.Empty<ActionDefNode>();
            }

            // INNER DEPENDENT FUNCTIONS
            bool IsVariableMatch(Variable a, Variable b) {
                return (a.IsBound && b.IsBound && a.Id == b.Id) || b.IsFree;
            };

            bool IsDependentRelation(VariableRelation p, VariableRelation apd) {
                return
                    IsVariableMatch(p.A, apd.A) &&
                    IsVariableMatch(p.Rel, apd.Rel) &&
                    IsVariableMatch(p.B, apd.B) &&
                    p.Negated == apd.Negated;
            }

            bool ShouldBindFreeVar (Variable a, Variable b) {
                return a.IsBound && b.IsFree;
            }
        }

        public void Print(int tabCount)
        {
            var tabs = Utils.GetTabs(tabCount);
            Console.WriteLine($"{tabs}{TargetVariableRelation}");

            if (DependentActions != null)
            {
                foreach (var d in DependentActions)
                {
                    d.Print(tabCount + 1);
                }
            }
        }

        public bool Equals(VariableRelationNode x, VariableRelationNode y)
        {
            return TargetVariableRelation.Equals(x.TargetVariableRelation, y.TargetVariableRelation);
        }

        public int GetHashCode(VariableRelationNode obj)
        {
            return obj.TargetVariableRelation.GetHashCode();
        }
    }

    public class ActionDefNode : IEqualityComparer<ActionDefNode>
    { 
        public ActionDef ActionDef { get; }
        public List<VariableRelationNode> DependentRelationNodes { get; private set; }

        private IEnumerable<ActionDef> _actions;

        public ActionDefNode()
        { }

        public ActionDefNode(ActionDef ad, IEnumerable<ActionDef> actions, int searchDepth)
        {
            ActionDef = ad;
            _actions = actions;

            // If this action has already been explored, use max score and dont expore it further
            if (StateScorer.s_visitedActionDefs.Contains(this))
            {
                StateScorer.s_visitedActionDefs.Add(this);
                return;
            }

            StateScorer.s_visitedActionDefs.Add(this);

            if (searchDepth > 0)
            {
                DependentRelationNodes = GetDependentRelationNodes(searchDepth-1).ToList();
            }
        }

        public int GetScoreRec(State state)
        {
            if (DependentRelationNodes != null && DependentRelationNodes.Any())
            {
                return DependentRelationNodes.Sum(drn => drn.GetScoreRec(state));
            }
            else
            {
                return 0;
            }
        }

        private IEnumerable<VariableRelationNode> GetDependentRelationNodes(int searchDepth)
        {
            if (ActionDef.PreConditions.Any())
            {
                return ActionDef.PreConditions.Select(rel => new VariableRelationNode(rel, _actions, searchDepth));
            }
            else return Enumerable.Empty<VariableRelationNode>();
        }

        public void Print(int tabCount)
        {
            var tabs = Utils.GetTabs(tabCount);
            Console.WriteLine($"{tabs}{ActionDef}");
            if (DependentRelationNodes != null)
            {
                foreach (var d in DependentRelationNodes)
                {
                    d.Print(tabCount + 1);
                }
            }
        }

        public bool Equals(ActionDefNode x, ActionDefNode y)
        {
            return ActionDef.Equals(x.ActionDef, y.ActionDef);
        }

        public int GetHashCode(ActionDefNode obj)
        {
            return obj.ActionDef.GetHashCode();
        }
    }
}
