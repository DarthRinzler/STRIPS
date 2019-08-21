using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPlan {

    public class PropositionLayer
    {
        public Dictionary<UInt64, PropositionNode> PositivePropositions { get; set; }        
        public Dictionary<UInt64, PropositionNode> NegativePropositions { get; set; }        
        public Dictionary<string, ActionDefinition> AvailableActions { get; set; }

        private Dictionary<uint, PropositionNode[]> _positiveNames;
        private Dictionary<uint, PropositionNode[]> _positiveProperties;
        private Dictionary<uint, PropositionNode[]> _positiveValues;

        private Dictionary<uint, PropositionNode[]> _negativeNames;
        private Dictionary<uint, PropositionNode[]> _negativeProperties;
        private Dictionary<uint, PropositionNode[]> _negativeValues;

        public PropositionLayer(
            Dictionary<string, ActionDefinition> availableActions, 
            Dictionary<ulong, PropositionNode> positivePropositions,
            Dictionary<ulong, PropositionNode> negativePropositions)
        {
            PositivePropositions = positivePropositions;
            NegativePropositions = negativePropositions;

            // Names
            _positiveNames = PositivePropositions.Values
                .GroupBy(p => p.Proposition.NameId)
                .ToDictionary(g => g.Key, g => g.ToArray());
            _negativeNames = NegativePropositions.Values
                .GroupBy(p => p.Proposition.NameId)
                .ToDictionary(g => g.Key, g => g.ToArray());

            // Properties
            _positiveProperties = PositivePropositions.Values
                .GroupBy(p => p.Proposition.PropertyId)
                .ToDictionary(g => g.Key, g => g.ToArray());
            _negativeProperties = NegativePropositions.Values
                .GroupBy(p => p.Proposition.PropertyId)
                .ToDictionary(g => g.Key, g => g.ToArray());

            // Values
            _positiveValues = PositivePropositions.Values
                .GroupBy(p => p.Proposition.ValueId)
                .ToDictionary(g => g.Key, g => g.ToArray());
            _negativeValues = NegativePropositions.Values
                .GroupBy(p => p.Proposition.ValueId)
                .ToDictionary(g => g.Key, g => g.ToArray());

            AvailableActions = availableActions;
        }

        public IEnumerable<ActionNode> GetAllActions()
        {
            return AvailableActions
                .Values
                .SelectMany(CalculateActionNodes);
        }

        public IEnumerable<ActionNode> CalculateActionNodes(ActionDefinition actionDefinition)
        {
            ParamSet knownSet = null;
            uint[] allNames = PositivePropositions.Values
                .Select(p => p.Proposition.NameId)
                .Concat(NegativePropositions.Values.Select(p => p.Proposition.NameId))
                .Distinct()
                .ToArray();

            // If Action has no parameters
            if (!actionDefinition.CtParams.Any())
            {
                yield return new ActionNode(new Action(actionDefinition, new uint[] { }));
                yield break;
            }

            // foreach positive precondition
            foreach (var positivePrecondition in actionDefinition.PositivePreconditions)
            {
                IEnumerable<PropositionNode> propositions = null;
                int[] valueIdxs = null;

                // Exit if not matches found
                if (knownSet != null && !knownSet.Params.Any())
                {
                    break;
                }
                // R L R
                else if (
                    positivePrecondition.Name.IsVariableRef && 
                    positivePrecondition.Property.IsLiteralRef && 
                    positivePrecondition.Value.IsVariableRef)
                {
                    uint property = positivePrecondition.Property.Id.Value;
                    if (!_positiveProperties.ContainsKey(property))
                    {
                        yield break;
                    }

                    propositions = _positiveProperties[property];
                    valueIdxs = new[] { positivePrecondition.Name.Idx.Value, positivePrecondition.Value.Idx.Value };
                }
                // R L L
                else if (
                    positivePrecondition.Name.IsVariableRef && 
                    positivePrecondition.Property.IsLiteralRef && 
                    positivePrecondition.Value.IsLiteralRef)
                {
                    uint property = positivePrecondition.Property.Id.Value;
                    uint value = positivePrecondition.Value.Id.Value;
                    if (!_positiveProperties.ContainsKey(property) || !_positiveValues.ContainsKey(value))
                    {
                        yield break;
                    }

                    propositions = _positiveProperties[property]
                        .Where(p => p.Proposition.ValueId == positivePrecondition.Value.Id.Value);
                    valueIdxs = new[] { positivePrecondition.Name.Idx.Value };
                }
                // R R R
                else if (
                    positivePrecondition.Name.IsVariableRef && 
                    positivePrecondition.Property.IsVariableRef && 
                    positivePrecondition.Value.IsVariableRef)
                {
                    propositions = PositivePropositions.Values;
                    valueIdxs = new[] {
                        positivePrecondition.Name.Idx.Value,
                        positivePrecondition.Property.Idx.Value,
                        positivePrecondition.Value.Idx.Value
                    };
                }
                else
                {
                    throw new NotImplementedException();
                }

                // Convert Propositions to Param objects
                var validParams = propositions
                    .Select(proposition => {
                        var ret = new uint[actionDefinition.CtParams.Count];
                        if (positivePrecondition.Name.IsVariableRef) ret[positivePrecondition.Name.Idx.Value] = proposition.Proposition.NameId;
                        if (positivePrecondition.Property.IsVariableRef) ret[positivePrecondition.Property.Idx.Value] = proposition.Proposition.PropertyId;
                        if (positivePrecondition.Value.IsVariableRef) ret[positivePrecondition.Value.Idx.Value] = proposition.Proposition.ValueId;
                        return ret;
                    })
                    .ToList();

                // If first 
                if (knownSet == null)
                {
                    knownSet = new ParamSet(validParams, valueIdxs);
                }
                // Join known param with new params
                else
                {
                    var newSet = new ParamSet(validParams, valueIdxs);
                    knownSet = knownSet.Join(newSet);
                }
            }

            if (knownSet != null)
            {
                knownSet = knownSet.Expand(actionDefinition, allNames);
                foreach (var action in knownSet.Params.Select(p => new Action(actionDefinition, p)))
                {
                    yield return new ActionNode(action);
                }
            }
        }
    }

    public class ActionLayer
    {
        public List<ActionNode> Actions { get; set; }

        public ActionLayer()
        {
            Actions = new List<ActionNode>();
        }
    }

    public class PropositionNode : IEqualityComparer<PropositionNode>
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

        public bool Equals(PropositionNode x, PropositionNode y)
        {
            return x.Proposition.Id == y.Proposition.Id;
        }

        public int GetHashCode(PropositionNode obj)
        {
            return obj.Proposition.Id.GetHashCode();
        }
    }

    public class ActionNode
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
            State2 start = p.ParseState2("data/TicTacStart.txt");
            State2 end = p.ParseState2("data/TicTacEnd.txt");
            var actions = p.ParseActions("data/TicTacActions.txt");

            Search(start, end, actions);
            //Run(state, actionDefs);
        }

        private static void Search(State2 start, State2 end, Dictionary<string, ActionDefinition> actionDefinitions)
        {
            PropositionLayer pl = new PropositionLayer(actionDefinitions, start.PositivePropositions, start.NegativePropositions);
            var allActionNodes = pl.GetAllActions().ToList();
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
