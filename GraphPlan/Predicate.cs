using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPlan
{
    public struct PredicateDef
    {
        public CtParameter Name { get; set; }
        public CtParameter? Property { get; set; }
        public CtParameter? Value { get; set; }
        public bool Negated { get; set; }

        public PredicateDef(CtParameter name, CtParameter? property=null, CtParameter? value=null, bool negated=false)
        {
            Name = name;
            Property = property;
            Value = value;
            Negated = negated;
        }

        public Predicate GetPredicate(uint[] rtParams)
        {
            uint eName = Name.Idx.HasValue ? rtParams[Name.Idx.Value] : Name.Id.Value;
            uint? eProperty = null;
            uint? eValue = null;

            if (Property.HasValue)
            {
                eProperty = Property.Value.Idx.HasValue ? rtParams[Property.Value.Idx.Value] : Property.Value.Id;
            }

            if (Value != null)
            {
                eValue = Value.Value.Idx.HasValue ? rtParams[Value.Value.Idx.Value] : Value.Value.Id;
            }

            return new Predicate(eName, eProperty, eValue);
        }
    }

    //[x][21][21][21]
    //Name,Prop,Value,
    public struct Predicate
        : IEqualityComparer<Predicate>
    {
        public UInt64 Id { get; private set; }
        public uint NameId { get; private set; }
        public uint? PropertyId { get; private set; }
        public uint? ValueId { get; private set; }

        private static uint MaxVal = (uint)(Math.Pow(2, 21) - 1);

        public Predicate(uint nameId, uint? propId = 0, uint? valId = 0)
        {
            NameId = nameId;
            PropertyId = propId;
            ValueId = valId;
            Id = 0;
            Id = GenerateId(this);
        }

        public static UInt64 GenerateId(Predicate p)
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

        public bool Equals(Predicate x, Predicate y)
        {
            return x.NameId == y.NameId && x.PropertyId == y.PropertyId && x.ValueId == y.ValueId;
        }

        public int GetHashCode(Predicate obj)
        {
            return this.GetHashCode();
        }

        public override string ToString()
        {
            var name = Ids.IdToName[NameId];
            if (ValueId != 0)
            {
                var value = Ids.IdToName[ValueId.Value];
                var prop = Ids.IdToName[PropertyId.Value];
                return $"{name}_{prop}_{value}";
            }
            else if (PropertyId != 0)
            {
                var prop = Ids.IdToName[PropertyId.Value];
                return $"{name}_{prop}";
            }
            else
            {
                return name;
            }
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
