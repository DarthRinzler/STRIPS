using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPlan
{
    public class State
    {
        public Dictionary<UInt64, Predicate> Predicates { get; private set; }
        public Dictionary<uint, Predicate[]> Properties { get; private set; }
        public Dictionary<uint, Predicate[]> Names { get; private set; }
        public Dictionary<uint, Predicate[]> Values { get; private set; }

        public State(Dictionary<UInt64, Predicate> predicates)
        {
            Predicates = predicates;

            Names = Predicates
                .GroupBy(pred => pred.Value.NameId)
                .ToDictionary(g => g.Key, g => g.Select(v => v.Value).ToArray());

            Properties = Predicates
                .GroupBy(pred => pred.Value.PropertyId)
                .ToDictionary(g => g.Key.Value, g => g.Select(v => v.Value).ToArray());

            Values = Predicates
                .GroupBy(pred => pred.Value.ValueId)
                .ToDictionary(g => g.Key.Value, g => g.Select(v => v.Value).ToArray());
        }

        public bool SatisfiesPrecondition(ActionInst action)
        {
            foreach (var p in action.Definition.PositivePre)
            {
                var pred = p.GetPredicate(action.Parameters);
                if (!Predicates.ContainsKey(pred.Id)) return false;
            }

            foreach (var p in action.Definition.NegativePre)
            {
                var pred = p.GetPredicate(action.Parameters);
                if (Predicates.ContainsKey(pred.Id)) return false;
            }

            return true;
        }

        public void ApplyActionMutate(ActionInst action)
        {
            var positiveEffects = action.Definition.PositivePost.Select(p => p.GetPredicate(action.Parameters));
            foreach (var p in positiveEffects)
            {
                Predicates[p.Id] = p;
            }

            var negativeEffects = action.Definition.NegativePost.Select(p => p.GetPredicate(action.Parameters));
            foreach (var p in negativeEffects)
            {
                if (Predicates.ContainsKey(p.Id))
                {
                    Predicates.Remove(p.Id);
                }
            }
        }

        public State ApplyAction(ActionInst action)
        {
            var predicates = new Dictionary<UInt64, Predicate>(this.Predicates);

            var positiveEffects = action.Definition.PositivePost.Select(p => p.GetPredicate(action.Parameters));
            foreach (var p in positiveEffects)
            {
                predicates[p.Id] = p;
            }

            var negativeEffects = action.Definition.NegativePost.Select(p => p.GetPredicate(action.Parameters));
            foreach (var p in negativeEffects)
            {
                if (predicates.ContainsKey(p.Id))
                {
                    predicates.Remove(p.Id);
                }
            }

            return new State(predicates);
        }

        public IEnumerable<ActionInst> GetAllActions(List<ActionDef> actionDefinitions)
        {
            return actionDefinitions.SelectMany(GetActionForActionDef);
        }

        private IEnumerable<ActionInst> GetActionForActionDef(ActionDef ad)
        {
            ParamSet knownSet = null;

            // Each N+V gets mapped to a paramIdx
            foreach (PredicateDef predDef in ad.PositivePre)
            {
                // R L R
                if (predDef.Name.Idx.HasValue && predDef.Property.Value.Id.HasValue && predDef.Value.Value.Idx.HasValue)
                {
                    // If first Predicate
                    if (knownSet == null)
                    {
                        var paramList = Properties[predDef.Property.Value.Id.Value]
                            .Select(pred => new[] {
                                new Param(pred.NameId, predDef.Name.Idx.Value),
                                new Param(pred.ValueId.Value, predDef.Value.Value.Idx.Value)
                            })
                            .ToList();

                        knownSet = new ParamSet(paramList);
                    }
                    // If NO satisfactoryParams left, return 
                    else if (!knownSet.Params.Any())
                    {
                        break;
                    }
                    else
                    {
                        // Join known param with new params
                        var newParams = Properties[predDef.Property.Value.Id.Value]
                            .Select(pred => new[] {
                            new Param(pred.NameId, predDef.Name.Idx.Value),
                            new Param(pred.ValueId.Value, predDef.Value.Value.Idx.Value)
                            })
                            .ToList();

                        var newSet = new ParamSet(newParams);

                        var next = knownSet.Intersect(newSet);
                        knownSet = next;
                    }
                }
                // R L L
                else if (predDef.Name.Idx.HasValue && predDef.Property.Value.Id.HasValue && predDef.Value.Value.Id.HasValue)
                {

                }
            }

            return null;
        }

        private List<Param[]> GetParams(PredicateDef pre, List<Param[]> foundSoFar, bool invert=false)
        {
            // N P V
            if (pre.Property.HasValue && pre.Value.HasValue)
            {
                // R L R
                if (pre.Name.Idx.HasValue && pre.Property.Value.Id.HasValue && pre.Value.Value.Idx.HasValue)
                {
                    if (foundSoFar.Any())
                    {
                        
                    }
                    else
                    {
                        var tuples = Properties[pre.Property.Value.Id.Value]
                            .Select(p => new[] {
                            new Param(p.NameId, pre.Name.Idx.Value),
                            new Param(p.ValueId.Value, pre.Value.Value.Idx.Value)
                            })
                            .ToList();
                        return tuples;
                    }
                }
                // R R R
                else if (pre.Name.Idx.HasValue && pre.Property.Value.Idx.HasValue && pre.Value.Value.Idx.HasValue)
                {
                }
                // R L L
                else if (pre.Name.Idx.HasValue && pre.Property.Value.Id.HasValue && pre.Value.Value.Id.HasValue)
                {

                }
                // L R R
                else if (pre.Name.Id.HasValue && pre.Property.Value.Idx.HasValue && pre.Value.Value.Idx.HasValue)
                {

                }
            }
            // N P
            else if (pre.Property.HasValue)
            {

            }
            // N
            else
            {

            }

            return null;
        }

        public override string ToString()
        {
            return String.Join("\n", Predicates.Select(p => p.Value.ToString()));
        }
    }

    struct Param
    {
        public uint Value;
        public int Idx;

        public Param(uint value, int idx)
        {
            Value = value;
            Idx = idx;
        }

        public override string ToString()
        {
            return Ids.IdToName[Value];
        }
    }

    class ParamSet 
    {
        public List<Param[]> Params { get; private set; }

        public ParamSet(List<Param[]> p)
        {
            Params = p;
        }

        public ParamSet Intersect(ParamSet other)
        {
            var keyIdxs = Params
                .First()
                .Select(p => p.Idx)
                .Intersect(other.Params.First().Select(p => p.Idx))
                .OrderBy(i => i)
                .ToHashSet();

            Func<Param[], string> GetKey = (array) => {
                StringBuilder sb = new StringBuilder();
                foreach (var a in array.OrderBy(a => a.Idx))
                {
                    if (keyIdxs.Contains(a.Idx))
                    {
                        sb.Append(a.Idx+"."+a+"&");
                    }
                }
                return sb.ToString();
            };

            var ad = Params.GroupBy(GetKey).ToDictionary(g => g.Key, g => g.ToArray());
            var bd = other.Params.GroupBy(GetKey).ToDictionary(g => g.Key, g => g.ToArray());

            var keys = ad.Keys.Intersect(bd.Keys).ToArray();
            var r = new List<Param[]>();

            foreach (var k in keys)
            {
                var avl = ad[k];
                var bvl = bd[k];

                foreach (var av in avl)
                {
                    foreach (var bv in bvl)
                    {
                        var pr = new Param[av.Length + bv.Length - keyIdxs.Count];
                        foreach (var p in av)
                        {
                            pr[p.Idx] = p;
                        }
                        foreach (var p in bv)
                        {
                            pr[p.Idx] = p;
                        }
                        r.Add(pr);
                    }
                }
            }

            return new ParamSet(r);
        }
    }
}
