using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPlan
{
    public struct ActionDef
    {
        public string Name { get; set; }
        public HashSet<PredicateDef> PositivePre { get; set; }
        public HashSet<PredicateDef> NegativePre { get; set; }
        public HashSet<PredicateDef> PositivePost { get; set; }
        public HashSet<PredicateDef> NegativePost { get; set; }
        public IList<uint> CtParams { get; set; }

        public ActionDef(
            string name, 
            IList<uint> ctParams, 
            HashSet<PredicateDef> posPre, 
            HashSet<PredicateDef> negPre, 
            HashSet<PredicateDef> posPost, 
            HashSet<PredicateDef> negPost)
        {
            Name = name;
            CtParams = ctParams;
            PositivePre = posPre;
            NegativePre = negPre;
            PositivePost = posPost;
            NegativePost = negPost;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public struct ActionInst
    {
        public uint[] Parameters { get; set; }
        public ActionDef Definition { get; set; }

        public ActionInst(ActionDef def, uint[] parameters)
        {
            Definition = def;
            Parameters = parameters;
        }

        public override string ToString()
        {
            string paramStrs = String.Join(",", Parameters.Select(p => Ids.IdToName[p]));
            return $"{Definition.Name}: {paramStrs}";
        }
    }
}
