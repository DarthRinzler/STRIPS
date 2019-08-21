using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPlan {

    public class State2
    {
        public Dictionary<UInt64, PropositionNode> PositivePropositions { get; private set; }
        public Dictionary<UInt64, PropositionNode> NegativePropositions { get; private set; }

        public State2(Dictionary<ulong, PropositionNode> positivePropositions, Dictionary<ulong, PropositionNode> negativePropositions)
        {
            PositivePropositions = positivePropositions;
            NegativePropositions = negativePropositions;
        }
    }

    public class State {
        public Dictionary<UInt64, Proposition> Propositions { get; private set; }
        public Dictionary<uint, Proposition[]> Properties { get; private set; }
        public Dictionary<uint, Proposition[]> Names { get; private set; }
        public Dictionary<uint, Proposition[]> Values { get; private set; }

        public State(Dictionary<UInt64, Proposition> propositions) {
            Propositions = propositions;

            Names = Propositions
                .GroupBy(prop => prop.Value.NameId)
                .ToDictionary(g => g.Key, g => g.Select(v => v.Value).ToArray());

            Properties = Propositions
                .GroupBy(prop => prop.Value.PropertyId)
                .ToDictionary(g => g.Key, g => g.Select(v => v.Value).ToArray());

            Values = Propositions
                .GroupBy(prop => prop.Value.ValueId)
                .ToDictionary(g => g.Key, g => g.Select(v => v.Value).ToArray());
        }

        public bool SatisfiesPrecondition(Action action) {
            foreach (var propositionDefinition in action.Definition.PositivePreconditions) {
                var proposition = propositionDefinition.GetProposition(action.Parameters);
                if (!Propositions.ContainsKey(proposition.Id)) return false;
            }

            foreach (var p in action.Definition.NegativePreconditions) {
                var prop = p.GetProposition(action.Parameters);
                if (Propositions.ContainsKey(prop.Id)) return false;
            }

            return true;
        }

        public void ApplyActionMutate(Action action) {
            var positiveEffects = action.Definition.PositivePost.Select(p => p.GetProposition(action.Parameters));
            foreach (var p in positiveEffects) {
                Propositions[p.Id] = p;
            }

            var negativeEffects = action.Definition.NegativePost.Select(p => p.GetProposition(action.Parameters));
            foreach (var p in negativeEffects) {
                if (Propositions.ContainsKey(p.Id)) {
                    Propositions.Remove(p.Id);
                }
            }
        }

        public State ApplyAction(Action action) {
            var propositions = new Dictionary<UInt64, Proposition>(this.Propositions);

            var positiveEffects = action.Definition.PositivePost.Select(p => p.GetProposition(action.Parameters));
            foreach (var p in positiveEffects) {
                propositions[p.Id] = p;
            }

            var negativeEffects = action.Definition.NegativePost.Select(p => p.GetProposition(action.Parameters));
            foreach (var p in negativeEffects) {
                if (propositions.ContainsKey(p.Id)) {
                    propositions.Remove(p.Id);
                }
            }

            return new State(propositions);
        }

        public IEnumerable<Action> GetAllActions(List<ActionDefinition> actionDefinitions) {
            return actionDefinitions.SelectMany(GetActionForActionDef);
        }

        private IEnumerable<Action> GetActionForActionDef(ActionDefinition ad) {
            ParamSet knownSet = null;
            var names = Propositions
                .Select(p => p.Value.NameId)
                .ToArray();

            // If action has no parameters
            if (ad.CtParams.Count == 0) {
                yield return new Action(ad, new uint[] { });
                yield break;
            }

            // Each N+V gets mapped to a paramIdx
            foreach (PropositionDefinition propDef in ad.PositivePreconditions) {
                IEnumerable<Proposition> propositions = null;
                int[] valueIdxs = null;

                // Exit if no matches found
                if (knownSet != null && !knownSet.Params.Any()) {
                    break;
                }
                // R L R
                else if (propDef.Name.Idx.HasValue && propDef.Property.Id.HasValue && propDef.Value.Idx.HasValue) {
                    uint property = propDef.Property.Id.Value;
                    if (!Properties.ContainsKey(property)) {
                        yield break;
                    }

                    propositions = Properties[property];
                    valueIdxs = new[] { propDef.Name.Idx.Value, propDef.Value.Idx.Value };
                }
                // R L L
                else if (propDef.Name.Idx.HasValue && propDef.Property.Id.HasValue && propDef.Value.Id.HasValue) {
                    uint prop = propDef.Property.Id.Value;
                    if (!Properties.ContainsKey(prop)) {
                        yield break;
                    }

                    propositions = Properties[prop].Where(p => p.ValueId == propDef.Value.Id.Value);
                    valueIdxs = new[] { propDef.Name.Idx.Value };
                }
                // R R R
                else if (propDef.Name.Idx.HasValue && propDef.Property.Idx.HasValue && propDef.Value.Idx.HasValue) {
                    propositions = Propositions.Values;
                    valueIdxs = new[] { propDef.Name.Idx.Value, propDef.Property.Idx.Value, propDef.Value.Idx.Value };
                }
                else {
                    throw new NotImplementedException();
                }

                // Convert Preds to Param objects
                var validParams = propositions  
                    .Select(proposition => {
                        var ret = new uint[ad.CtParams.Count];
                        if (propDef.Name.IsVariableRef) ret[propDef.Name.Idx.Value] = proposition.NameId;
                        if (propDef.Property.IsVariableRef) ret[propDef.Property.Idx.Value] = proposition.PropertyId;
                        if (propDef.Value.IsVariableRef) ret[propDef.Value.Idx.Value] = proposition.ValueId;
                        return ret;
                    })
                    .ToList();

                // If first Proposition 
                if (knownSet == null) {
                    knownSet = new ParamSet(validParams, valueIdxs);
                }
                // Join known param with new params
                else {
                    var newSet = new ParamSet(validParams, valueIdxs);
                    knownSet = knownSet.Join(newSet);
                    //Console.WriteLine(knownSet.Params.Count);
                }
            }

            if (knownSet != null) {
                knownSet = knownSet.Expand(ad, names);
            }

            foreach (PropositionDefinition propDef in ad.NegativePreconditions) {
                IEnumerable<Proposition> props = null;
                int[] valueIdxs = null;

                // Exit if no matches found
                if (knownSet != null && !knownSet.Params.Any()) {
                    break;
                }
                // R L R
                else if (propDef.Name.IsVariableRef && propDef.Property.Id.HasValue && propDef.Value.IsVariableRef) {
                    uint prop = propDef.Property.Id.Value;
                    if (!Properties.ContainsKey(prop)) {
                        yield break;
                    }

                    props = Properties[prop];
                    valueIdxs = new[] { propDef.Name.Idx.Value, propDef.Value.Idx.Value };
                }
                // R L L
                else if (propDef.Name.Idx.HasValue && propDef.Property.Id.HasValue && propDef.Value.Id.HasValue) {
                    uint prop = propDef.Property.Id.Value;
                    if (!Properties.ContainsKey(prop)) {
                        yield break;
                    }

                    props = Properties[prop]
                        .Where(p => p.ValueId == propDef.Value.Id.Value);
                    valueIdxs = new[] { propDef.Name.Idx.Value };
                }
                // R R R
                else {
                    props = Propositions.Values;
                    valueIdxs = new[] { propDef.Name.Idx.Value, propDef.Property.Idx.Value, propDef.Value.Idx.Value };
                }

                // Convert Propositions to Param objects
                var toRemove = props  
                    .Select(prop => {
                        var ret = new uint[ad.CtParams.Count];
                        if (propDef.Name.Idx.HasValue) ret[propDef.Name.Idx.Value] = prop.NameId;
                        if (propDef.Property.Idx.HasValue) ret[propDef.Property.Idx.Value] = prop.PropertyId;
                        if (propDef.Value.Idx.HasValue) ret[propDef.Value.Idx.Value] = prop.ValueId;
                        return ret;
                    })
                    .ToList();

                // If first Proposition 
                if (knownSet == null) {
                    knownSet = new ParamSet(toRemove, valueIdxs);
                }
                // Remove invalid propositions from KnownSet
                else {
                    var newSet = new ParamSet(toRemove, valueIdxs);
                    knownSet = knownSet.Except(newSet);
                }
            }

            if (knownSet != null) {
                knownSet = knownSet.Expand(ad, names);
                foreach (var action in knownSet.Params.Select(p => new Action(ad, p))) { 
                    yield return action;
                }
            }
        }

        public override string ToString() {
            return String.Join(" \n", Propositions.Select(p => p.Value.ToString()));
        }
    }

    public class Param : IEqualityComparer<Param> {
        public uint Value;
        public int Idx;

        public Param(uint value, int idx) {
            Value = value;
            Idx = idx;
        }

        public bool Equals(Param x, Param y) {
            return x.Value == y.Value && x.Idx == y.Idx;
        }

        public int GetHashCode(Param obj) {
            return obj.Idx.GetHashCode() ^ obj.Value.GetHashCode();
        }

        public override string ToString() {
            return Ids.IdToName[Value];
        }
    }

    public class ParamSet 
    {
        public List<uint[]> Params { get; private set; }
        public int[] ValueIndexes { get; private set; }
        public int[] AllIdxs { get; private set; }

        public ParamSet(List<uint[]> p, int[] valueIdxs) {
            ValueIndexes = valueIdxs;
            if (p.Any()) {
                AllIdxs = Enumerable
                    .Range(0, p.First().Length)
                    .ToArray();
            }
            Params = p;
        }

        public ParamSet(List<uint[]> p) {
            ValueIndexes = Enumerable
                .Range(0, p.First().Length)
                .ToArray();
            AllIdxs = ValueIndexes;
            Params = p;
        }

        public ParamSet Join(ParamSet other) {
            var joinIndexes = ValueIndexes
                .Intersect(other.ValueIndexes)
                .ToArray();

            if (!joinIndexes.Any()) {
                
            }

            var p = Params
                .Join(
                    other.Params,
                    k => GetKey(k, joinIndexes),
                    k => GetKey(k, joinIndexes),
                    Join
                )
                .Where(pa => pa != null)
                .ToList();

            var newIndexes = ValueIndexes
                .Union(other.ValueIndexes)
                .ToArray();

            return new ParamSet(p, newIndexes);
        }

        private uint[] Join(uint[] a, uint[] b) {
            var ret = new uint[a.Length];
            HashSet<uint> values = new HashSet<uint>();
            for (int i=0; i<ret.Length; i++) {
                if (a[i] != 0) ret[i] = a[i];
                else if (b[i] != 0) ret[i] = b[i];
                if (ret[i] != 0 && values.Contains(ret[i])) {
                    return null;
                }
                values.Add(ret[i]);
            }
            return ret;
        }

        private string GetKey(uint[] a, int[] joinIndexes) {
            //var ret = String.Join("|", joinIndexes.Select(ji => a[ji]+"."+ji));
            var ret = String.Join("|", joinIndexes.Select(ji => Ids.IdToName[a[ji]]+"."+ji));
            return ret;
        }

        public ParamSet Except(ParamSet other) {
            var joinIndexes = ValueIndexes
                .Intersect(other.ValueIndexes)
                .ToArray();

            var ad = Params
                .GroupBy(p => GetKey(p, joinIndexes))
                .ToDictionary(k => k.Key, k => k.ToList());

            foreach (var op in other.Params) {
                var key = GetKey(op, joinIndexes);
                if (ad.ContainsKey(key)) {
                    //Console.WriteLine(key);
                    ad.Remove(key);
                }
            }

            var ret = ad.SelectMany(g => g.Value).ToList();
            var newIndexes = ValueIndexes
                .Union(other.ValueIndexes)
                .ToArray();

            return new ParamSet(ret, newIndexes);
        }

        public ParamSet Expand(ActionDefinition ad, uint[] names) {
            var ret = new List<uint[]>();

            if (!Params.Any() || Params.First().All(p => p != 0)) {
                return this;
            }

            var added = new HashSet<string>();
            foreach (var paramArray in Params) {
                var expanded = ExpandVariables(paramArray, 0, names, added);
                ret.AddRange(expanded);
            }
            return new ParamSet(ret);
        }

        private IEnumerable<uint[]> ExpandVariables(uint[] paramArray, int idx, uint[] names, HashSet<string> added) {
            for (int i=idx; i<paramArray.Length; i++) {
                if (paramArray[i] == 0) {
                    foreach (var name in names.Where(n => !paramArray.Contains(n))) {
                        var pa = paramArray.ToArray();
                        pa[i] = name;
                        foreach (var expanded in ExpandVariables(pa, i + 1, names, added)) {
                            yield return expanded;
                        }
                    }

                    yield break; 
                }
            }

            var key = GetKey(paramArray, AllIdxs);
            if (!added.Contains(key)) {
                added.Add(key);
                yield return paramArray;
            }
        }

        public override string ToString() {
            return String.Join("|", Params.Select(pa => String.Join("_", pa.Select(p => p > 0 ? Ids.IdToName[p] : "?"))));
        }
    }
}
