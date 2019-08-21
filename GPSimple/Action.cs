using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSimple
{
    public struct ActionDefinition
    {
        public string Name { get; set; }
        public HashSet<Proposition> PositivePre { get; set; }
        public HashSet<Proposition> NegativePre { get; set; }
        public HashSet<Proposition> PositivePost { get; set; }
        public HashSet<Proposition> NegativePost { get; set; }
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

    class Action
    {
    }
}
