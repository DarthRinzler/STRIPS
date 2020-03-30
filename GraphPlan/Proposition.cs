using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPlan
{
    public struct PropositionDefinition : IEqualityComparer<PropositionDefinition>
    {
        public CtParameter Name { get; }
        public CtParameter Property { get; }
        public CtParameter Value { get; }
        public bool Negated { get; }

        public PropositionDefinition(CtParameter name, CtParameter property, CtParameter value, bool negated)
        {
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

        public bool Equals(PropositionDefinition x, PropositionDefinition y)
        {
            return 
                x.Name.Equals(y.Name) && 
                x.Property.Equals(y.Property) && 
                x.Value.Equals(y.Value) && 
                x.Negated == y.Negated;
        }

        public bool EqualsIgnoreNegation(PropositionDefinition other)
        {
            return
                Name.Equals(other.Name) &&
                Property.Equals(other.Property) &&
                Value.Equals(other.Value);
        }

        public int GetHashCode(PropositionDefinition obj)
        {
            return
                obj.Name.GetHashCode() ^ obj.Property.GetHashCode() ^ obj.Value.GetHashCode() ^ Negated.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Name.ParamName} {Property.ParamName} {Value.ParamName}";
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

    //[x][21][21][21]
    //Name,Prop,Value,
    public struct Proposition
        : IEqualityComparer<Proposition>
    {
        public UInt64 Id { get; }
        public uint NameId { get; }
        public uint PropertyId { get;}
        public uint ValueId { get; }
        public bool Truth { get; }

        private static uint MaxVal = (uint)(Math.Pow(2, 21) - 1);

        public Proposition(uint nameId, uint propId, uint valId, bool truth=true)
        {
            NameId = nameId;
            PropertyId = propId;
            ValueId = valId;
            Id = 0;
            Truth = truth;
            Id = GenerateId(this);
        }

        public static UInt64 GenerateId(Proposition p)
        {
            return GenerateId(p.NameId, p.PropertyId, p.ValueId, p.Truth);
        }

        public static UInt64 GenerateId(uint name, uint property, uint value, bool truth)
        {
            UInt64 id = name;
            id = id << 21;

            id = id | property;
            id = id << 21;

            id = id | value;
            id = id << 1;

            id = id | (truth ? (ulong)1 : (ulong)0);

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
            return $"{name} {prop} {value}";
        }

        public IEnumerable<ActionDefinition> GetDependentActionDefs(IEnumerable<ActionDefinition> actionDefinitions)
        {
            var prop = this;
            // Get list of all actions that can produce this proposition
            var dependentActions = actionDefinitions
                .Where(a => a.PositivePostconditions.Any(propDef => prop.IsIntanceOf(propDef)));

            return dependentActions;
        }

        public bool IsIntanceOf(PropositionDefinition propDef)
        {
            bool ret =  
                (propDef.Name.IsVariableRef || this.NameId == propDef.Name.Id.Value) &&
                (propDef.Property.IsVariableRef || this.PropertyId == propDef.Property.Id.Value) &&
                (propDef.Value.IsVariableRef || this.ValueId == propDef.Value.Id.Value);
            return ret;
        }
    }

    public struct CtParameter : IEquatable<CtParameter>
    {
        public uint? Id { get; set; }
        public int? Idx { get; set; }

        public bool IsVariableRef { get { return Idx.HasValue; } }
        public bool IsLiteralRef { get { return !Idx.HasValue; } }

        public string ParamName { get; set; }

        public bool Equals(CtParameter other)
        {
            return
                (Id.HasValue ? Id.Value == other.Id.Value : true) &&
                (Idx.HasValue ? Idx.Value == other.Idx.Value : true);
        }

        public override int GetHashCode()
        {
            return Id.HasValue ? Id.GetHashCode() : Idx.GetHashCode();
        }
    }
}
