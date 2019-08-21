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
        public HashSet<PropositionDefinition> PositivePreconditions { get; set; }
        public HashSet<PropositionDefinition> NegativePreconditions { get; set; }
        public HashSet<PropositionDefinition> PositivePost { get; set; }
        public HashSet<PropositionDefinition> NegativePost { get; set; }
        public IList<uint> CtParams { get; set; }

        public ActionDefinition(
            string name, 
            IList<uint> ctParams, 
            HashSet<PropositionDefinition> posPre, 
            HashSet<PropositionDefinition> negPre, 
            HashSet<PropositionDefinition> posPost, 
            HashSet<PropositionDefinition> negPost)
        {
            Name = name;
            CtParams = ctParams;
            PositivePreconditions = posPre;
            NegativePreconditions = negPre;
            PositivePost = posPost;
            NegativePost = negPost;
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
            return $"{Definition.Name} {paramStrs}";
        }
    }
}
