using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planner {

    public class State : IEqualityComparer<State> {
        public Dictionary<UInt64, Fact> Facts { get; }
        public Lazy<Dictionary<uint, Fact[]>> Rel { get; }
        public Lazy<Dictionary<uint, Fact[]>> A { get; }
        public Lazy<Dictionary<uint, Fact[]>> B { get; }

        private Lazy<int> _hashCode;

        public State()
            : this(new Dictionary<ulong, Fact>())
        { }

        public void AddFact(Fact f)
        {
            Facts.Add(f.Id, f);
        }

        public State(Dictionary<UInt64, Fact> facts) {
            Facts = facts;

            A = new Lazy<Dictionary<uint, Fact[]>>(() => Facts
                .GroupBy(prop => prop.Value.AId)
                .ToDictionary(g => g.Key, g => g.Select(v => v.Value).ToArray()));

            Rel = new Lazy<Dictionary<uint, Fact[]>>(() => Facts
                .GroupBy(prop => prop.Value.RelId)
                .ToDictionary(g => g.Key, g => g.Select(v => v.Value).ToArray()));

            B = new Lazy<Dictionary<uint, Fact[]>>(() => Facts
                .GroupBy(prop => prop.Value.BId)
                .ToDictionary(g => g.Key, g => g.Select(v => v.Value).ToArray()));

            _hashCode = new Lazy<int>(() => (int)Facts
                .Select(p => p.Key)
                .Aggregate((a, e) => a ^ e));
        }

        public State ApplyAction(Action action) {
            var facts = new Dictionary<UInt64, Fact>(this.Facts);

            var positiveEffects = action.Definition.PositivePostconditions.Select(p => p.GetProposition(action.Parameters));
            foreach (var p in positiveEffects) {
                facts[p.Id] = p;
            }

            var negativeEffects = action.Definition.NegativePostconditions.Select(p => p.GetProposition(action.Parameters));
            foreach (var p in negativeEffects) {
                if (facts.ContainsKey(p.Id)) {
                    facts.Remove(p.Id);
                }
            }

            return new State(facts);
        }

        public IEnumerable<Action> GetAllActions(IEnumerable<ActionDef> actionDefinitions) 
        {
            return actionDefinitions.SelectMany(GetActionsForActionDef);
        }

        public IEnumerable<Action> GetActionsForActionDef(ActionDef ad) 
        {
            ParamSet knownSet = null;
            var names = Facts
                .Select(p => p.Value.AId)
                .ToArray();

            // If action has no parameters
            if (ad.Parameters.Length == 0) {
                yield break;
            }

            // Each N+V gets mapped to a paramIdx
            foreach (VariableRelation varRelation in ad.PositivePreconditions) {
                IEnumerable<Fact> facts = null;
                int[] valueIdxs = null;

                // Exit if no matches found
                if (knownSet != null && !knownSet.Params.Any()) {
                    break;
                }
                // R L R
                else if (varRelation.A.IsFree && varRelation.Rel.IsBound && varRelation.B.IsFree) {
                    uint property = varRelation.Rel.Id;
                    if (!Rel.Value.ContainsKey(property)) {
                        yield break;
                    }

                    facts = Rel.Value[property];
                    valueIdxs = new[] { varRelation.A.Idx, varRelation.B.Idx };
                }
                // R L L
                else if (varRelation.A.IsFree && varRelation.Rel.IsBound && varRelation.B.IsBound) {
                    uint prop = varRelation.Rel.Id;
                    if (!Rel.Value.ContainsKey(prop)) {
                        yield break;
                    }

                    facts = Rel.Value[prop].Where(p => p.BId == varRelation.B.Id);
                    valueIdxs = new[] { varRelation.A.Idx };
                }
                // L L L
                else if (varRelation.A.IsBound && varRelation.Rel.IsBound && varRelation.B.IsBound)
                {
                    throw new NotImplementedException();    
                }
                // R R R
                else if (varRelation.A.IsFree && varRelation.Rel.IsFree && varRelation.B.IsFree) {
                    facts = Facts.Values;
                    valueIdxs = new[] { 
                        varRelation.A.Idx, 
                        varRelation.Rel.Idx, 
                        varRelation.B.Idx 
                    };
                }
                else {
                    throw new NotImplementedException();
                }

                // Convert Preds to Param objects
                var validParams = facts  
                    .Select(proposition => {
                        var ret = new uint[ad.Parameters.Length];
                        if (varRelation.A.IsFree) ret[varRelation.A.Idx] = proposition.AId;
                        if (varRelation.Rel.IsFree) ret[varRelation.Rel.Idx] = proposition.RelId;
                        if (varRelation.B.IsFree) ret[varRelation.B.Idx] = proposition.BId;
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
                }
            }

            if (knownSet != null) {
                knownSet = knownSet.Expand(names);
            }

            foreach (VariableRelation varRelation in ad.NegativePreconditions) {
                IEnumerable<Fact> props = null;
                int[] valueIdxs = null;

                // Exit if no matches found
                if (knownSet != null && !knownSet.Params.Any()) {
                    break;
                }
                // R L R
                else if (varRelation.A.IsFree && varRelation.Rel.IsBound && varRelation.B.IsFree) {
                    uint prop = varRelation.Rel.Id;
                    if (!Rel.Value.ContainsKey(prop)) {
                        break;
                    }

                    props = Rel.Value[prop];
                    valueIdxs = new[] { varRelation.A.Idx, varRelation.B.Idx };
                }
                // R L L
                else if (varRelation.A.IsFree && varRelation.Rel.IsBound && varRelation.B.IsBound) {
                    uint prop = varRelation.Rel.Id;
                    if (!Rel.Value.ContainsKey(prop)) {
                        break;
                    }

                    props = Rel.Value[prop]
                        .Where(p => p.BId == varRelation.B.Id);
                    valueIdxs = new[] { varRelation.A.Idx };
                }
                // L L L
                else if (varRelation.A.IsBound && varRelation.Rel.IsBound && varRelation.B.IsBound)
                {
                    throw new NotImplementedException();    
                }
                // R R R
                else {
                    props = Facts.Values;
                    valueIdxs = new[] { varRelation.A.Idx, varRelation.Rel.Idx, varRelation.B.Idx };
                }

                // Convert Propositions to Param objects
                var toRemove = props  
                    .Select(prop => {
                        var ret = new uint[ad.Parameters.Length];
                        if (varRelation.A.IsFree) ret[varRelation.A.Idx] = prop.AId;
                        if (varRelation.Rel.IsFree) ret[varRelation.Rel.Idx] = prop.RelId;
                        if (varRelation.B.IsFree) ret[varRelation.B.Idx] = prop.BId;
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
                knownSet = knownSet.Expand(names);
                foreach (var action in knownSet.Params.Select(p => new Action(ad, p))) { 
                    yield return action;
                }
            }
        }

        public override string ToString() {
            return String.Join(" \n", Facts.Select(p => p.Value.ToString()));
        }

        public bool Equals(State x, State y)
        {
            return
                x.Facts.Count() == y.Facts.Count() &&
                x.Facts.All(xp => y.Facts.ContainsKey(xp.Key));
        }

        public bool SatisfiesState(State goal)
        {
            return
                goal.Facts.All(gp => Facts.ContainsKey(gp.Key));
        }

        public int GetHashCode(State obj)
        {
            return _hashCode.Value;
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
        public List<uint[]> Params { get; }
        public int[] ValueIndexes { get; }
        public int[] AllIdxs { get; }

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
            ValueIndexes = p.Any() ? Enumerable
                .Range(0, p.First().Length)
                .ToArray() : new int[0];
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

        public ParamSet Expand(uint[] names) {
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
