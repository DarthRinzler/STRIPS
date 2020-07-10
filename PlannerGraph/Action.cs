using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlannerGraph
{
    public class ActionInst
    { 
        public ActionDef Definition { get; } 
        public State[] Parameters { get; }

        public ActionInst(ActionDef def, State[] parameters)
        {
            Definition = def;
            Parameters = parameters;
        }
    }

    public class ActionDef
    {
        public string Name { get; }
        public Variable[] Signature { get; }
        public VariableRelation[] Pre { get; }
        public VariableRelation[] Post { get; }

        public ActionDef(string name, Variable[] signature, VariableRelation[] pre, VariableRelation[] post)
        {
            Signature = signature;
            Name = name;
            Pre = pre;
            Post = post;
        }

        public ActionDef RebindVariables(Variable[] newVars)
        {
            var rebindMap = newVars
                .Select((nv, idx) => new { New = newVars[idx], Old = Signature[idx] })
                .ToDictionary(pair => pair.Old.Name, pair => pair.New);

            var newPre = Pre.Select(preNr => preNr.ReBind(rebindMap)).ToArray();
            var newPost = Post.Select(postNr => postNr.ReBind(rebindMap)).ToArray();
            var ret = new ActionDef(Name, newVars, newPre, newPost);
            return ret;
        }

        public override string ToString()
        {
            var signatureStr = String.Join(" ", Signature.Select(p => p.ToString()));
            var preStr = String.Join("\n\t", Pre.Select(p => p.ToString()));
            var postStr = String.Join("\n\t", Pre.Select(p => p.ToString()));

            return $"{Name}({signatureStr})\n\t(Pre\n\t\t({preStr}))\n\t(Post\n\t\t({postStr}))";
        }
    }
}
