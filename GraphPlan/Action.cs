using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPlan
{

    public struct ActionDefinition
    {
        public string Name { get; set; }
        public HashSet<PropositionDefinition> PositivePreconditions { get; }
        public HashSet<PropositionDefinition> NegativePreconditions { get; }
        public HashSet<PropositionDefinition> PositivePostconditions { get; }
        public HashSet<PropositionDefinition> NegativePostconditions { get; }
        public Dictionary<string,int> CtParams { get; }

        public bool IsDependent { get; }
        public bool IsAutoExecute { get; }

        public ActionDefinition(
            string name, 
            Dictionary<string,int> ctParams, 
            HashSet<PropositionDefinition> posPre, 
            HashSet<PropositionDefinition> negPre, 
            HashSet<PropositionDefinition> posPost, 
            HashSet<PropositionDefinition> negPost,
            bool isDependent=false,
            bool isAutoExecute=false)
        {
            Name = name;
            CtParams = ctParams;
            PositivePreconditions = posPre;
            NegativePreconditions = negPre;
            PositivePostconditions = posPost;
            NegativePostconditions = negPost;
            IsDependent = isDependent;
            IsAutoExecute = isAutoExecute;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Action
    {
        public uint[] Parameters { get; set; }
        public ActionDefinition Definition { get; set; }

        public Action(ActionDefinition def, uint[] parameters)
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
