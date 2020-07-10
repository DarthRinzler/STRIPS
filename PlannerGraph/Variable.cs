using System;
using System.Collections.Generic;
using System.Text;

namespace PlannerGraph
{
    public struct Variable 
    {
        public string Name { get; }

        public uint Value { get { return _getValue(); } }

        public bool IsBound { get { return _getValue != null; } }

        private Func<uint> _getValue;

        public Variable(string name, Func<uint> getValue=null)
        {
            Name = name;
            _getValue = getValue;
        }

        public void Bind(Func<uint> getValue)
        {
            _getValue = getValue;
        }
    }

    public struct BoundVariable
    { 
           
    }

    public struct UnboundVariable
    { 
    
    }

    public struct VariableRelation
    {
        public Variable A { get; }
        public Variable Rel { get; }
        public Variable B { get; }

        public bool Negated { get; }

        private Fact? _evalutedFact;

        public VariableRelation(Variable a, Variable rel, Variable b, bool negated = false)
        {
            A = a;
            Rel = rel;
            B = b;
            Negated = negated;
            _evalutedFact = null;
        }

        public VariableRelation ReBind(Dictionary<string, Variable> newVars)
        {
            Variable RebindVariable(Variable nv)
            {
                return newVars.ContainsKey(nv.Name) ? newVars[nv.Name] : nv.Clone();
            }

            var a = RebindVariable(A);
            var rel = RebindVariable(Rel);
            var b = RebindVariable(B);
            return new VariableRelation(a, rel, b);
        }

        public override string ToString()
        {
            var ret = $"({A} {Rel} {B})";
            return Negated ? $"(NOT {ret})" : ret;
        }

        public Fact Evaluate()
        {
            Fact ret;
            if (_evalutedFact.HasValue)
            {
                ret = _evalutedFact.Value;
            }
            else if (A.IsBound && Rel.IsBound & B.IsBound)
            {
                _evalutedFact = new Fact(A.Value, Rel.Value, B.Value);
                ret = _evalutedFact.Value;
            }
            else throw new Exception("Cannot Evaluate Relation when variables are still unbound");

            return ret;
        }
    }
}
