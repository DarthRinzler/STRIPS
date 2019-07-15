using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPlan
{
    public struct PropositionDefinition
    {
        public Proposition Proposition { get; private set; }
        public CtParameter Name { get; private set; }
        public CtParameter Property { get; private set; }
        public CtParameter Value { get; private set; }
        public bool Negated { get; private set; }

        public PropositionDefinition(Proposition proposition, CtParameter name, CtParameter property, CtParameter value, bool negated=false)
        {
            Proposition = proposition;
            Name = name;
            Property = property;
            Value = value;
            Negated = negated;
        }

        public Proposition GetProposition(uint[] rtParams)
        {
            uint eName = Name.Idx.HasValue ? rtParams[Name.Idx.Value] : Name.Id.Value;
            uint eProperty = Property.Idx.HasValue ? rtParams[Property.Idx.Value] : Property.Id.Value;
            uint eValue = Value.Idx.HasValue ? rtParams[Value.Idx.Value] : Value.Id.Value;

            return new Proposition(eName, eProperty, eValue);
        }
    }

    [Flags]
    public enum NPVFlags
    {
        None = 0,
        Name = 1,
        Property = 2,
        Value = 4
    }

    public struct ActionProposition : IEqualityComparer<ActionProposition>
    {
        public Proposition Proposition { get; private set; }
        public int NameParamIdx { get; private set; }
        public int PropParamIdx { get; private set; }
        public int ValueParamIdx { get; private set; }
        public NPVFlags FreeVariables { get; private set; }
        public ActionProposition(Proposition p, int nameIdx, int propIdx, int valIdx, NPVFlags freeVars) {
            Proposition = p;
            NameParamIdx = nameIdx;
            PropParamIdx = propIdx;
            ValueParamIdx = valIdx;
            FreeVariables = freeVars;
        }

        public bool Equals(ActionProposition x, ActionProposition y)
        {
            if (FreeVariables.HasFlag(NPVFlags.Name))
            {
                if (x.NameParamIdx != y.NameParamIdx || x.Proposition.NameId != y.Proposition.NameId)
                {
                    return false;
                }
            }
            if (FreeVariables.HasFlag(NPVFlags.Property))
            {
                if (x.PropParamIdx != y.PropParamIdx || x.Proposition.PropertyId != y.Proposition.PropertyId)
                {
                    return false;
                }
            }
            if (FreeVariables.HasFlag(NPVFlags.Value))
            {
                if (x.ValueParamIdx != y.ValueParamIdx || x.Proposition.ValueId != y.Proposition.ValueId)
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(ActionProposition obj)
        {
            return Proposition.Id.GetHashCode() ^ ((NameParamIdx + 29) * (PropParamIdx + 57) * (ValueParamIdx + 103));
        }
    }

    //[x][21][21][21]
    //Name,Prop,Value,
    public struct Proposition
        : IEqualityComparer<Proposition>
    {
        public UInt64 Id { get; private set; }
        public uint NameId { get; private set; }
        public uint PropertyId { get; private set; }
        public uint ValueId { get; private set; }

        private static uint MaxVal = (uint)(Math.Pow(2, 21) - 1);

        public Proposition(uint nameId, uint propId = 0, uint valId = 0)
        {
            NameId = nameId;
            PropertyId = propId;
            ValueId = valId;
            Id = 0;
            Id = GenerateId(this);
        }

        public static UInt64 GenerateId(Proposition p)
        {
            return GenerateId(p.NameId, p.PropertyId, p.ValueId);
        }

        public static UInt64 GenerateId(uint name, uint? property, uint? value)
        {
            UInt64 id = name;
            id = id << 21;

            if (property.HasValue) id = id | property.Value;
            id = id << 21;

            if (value.HasValue) id = id | value.Value;

            return id;
        }

        public bool Equals(Proposition x, Proposition y)
        {
            return x.NameId == y.NameId && x.PropertyId == y.PropertyId && x.ValueId == y.ValueId;
        }

        public int GetHashCode(Proposition obj)
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            var name = Ids.IdToName[NameId];
            var value = Ids.IdToName[ValueId];
            var prop = Ids.IdToName[PropertyId];
            return $"{name}_{prop}_{value}";
        }
    }

    public struct CtParameter
    {
        public uint? Id { get; set; }
        public int? Idx { get; set; }

        public CtParameter(uint nameId)
        {
            Id = nameId;
            Idx = null;
        }

        public CtParameter(int idx)
        {
            Id = null;
            Idx = idx;
        }
    }

}
