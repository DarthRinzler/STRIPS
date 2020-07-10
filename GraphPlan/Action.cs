using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planner
{
    public struct ActionDef : IEqualityComparer<ActionDef>
    {
        public string Name { get; set; }
        public HashSet<VariableRelation> PositivePreconditions { get; }
        public HashSet<VariableRelation> NegativePreconditions { get; }
        public HashSet<VariableRelation> PositivePostconditions { get; }
        public HashSet<VariableRelation> NegativePostconditions { get; }

        public IEnumerable<VariableRelation> PreConditions { get { return PositivePreconditions.Concat(NegativePreconditions); } }
        public IEnumerable<VariableRelation> PostConditions { get { return PositivePostconditions.Concat(NegativePostconditions); } }
        public IEnumerable<VariableRelation> AllConditions { get { return PreConditions.Concat(PostConditions); } }
        public UnboundVariable[] Parameters { get; }

        public ActionDef(
            string name, 
            UnboundVariable[] parameters,
            HashSet<VariableRelation> posPre, 
            HashSet<VariableRelation> negPre, 
            HashSet<VariableRelation> posPost, 
            HashSet<VariableRelation> negPost)
        {
            Name = name;
            Parameters = parameters;
            PositivePreconditions = posPre;
            NegativePreconditions = negPre;
            PositivePostconditions = posPost;
            NegativePostconditions = negPost;
        }

        public ActionDef RebindVariables(Variable[] newVars, string newActionName)
        {
            UnboundVariable[] parameters = newVars
                .Where(nv => nv.IsFree)
                .Cast<UnboundVariable>()
                .ToArray();

            Func<HashSet<VariableRelation>, HashSet<VariableRelation>> RebindPreconditions = (pcs) => 
                pcs.Select(pc => pc.Rebind(newVars))
                   .ToHashSet();

            var posPre = RebindPreconditions(PositivePreconditions);
            var negPre = RebindPreconditions(NegativePreconditions);
            var posPost = RebindPreconditions(PositivePostconditions);
            var negPost = RebindPreconditions(NegativePostconditions);

            return new ActionDef(newActionName, parameters, posPre, negPre, posPost, negPost);
        }

        public ActionDef BindVariables(Dictionary<UnboundVariable, BoundVariable> bindMap)
        {
            var newVars = Parameters
                .Select(p => bindMap.ContainsKey(p) ? bindMap[p] : p.Clone())
                .ToArray();

            var mappedVars = bindMap
                .Select(kv => new { kv.Key.Idx, kv.Value })
                .OrderBy(g => g.Idx)
                .Select(g => g.Value.Name);
                
            string newName = $"{Name}_{String.Join("_", mappedVars)}";
            return RebindVariables(newVars, newName);
        }

        public override int GetHashCode()
        {
            // THIS MAY BE A PROBLEM? IS NAME GUARANTEED TO BE UNIQUE?
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        public bool Equals(ActionDef x, ActionDef y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(ActionDef obj)
        {
            return obj.Name.GetHashCode();
        }
    }

    public class Action
    {
        public uint[] Parameters { get; set; }
        public ActionDef Definition { get; set; }

        public Action(ActionDef def, uint[] parameters)
        {
            Definition = def;
            Parameters = parameters;
        }

        public override string ToString()
        {
            string paramStrs = String.Join(" ", Parameters.Select(p => Ids.IdToName[p]));
            return $"{Definition.Name} {paramStrs}".Trim();
        }
    }
}
