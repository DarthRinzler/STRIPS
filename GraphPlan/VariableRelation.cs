using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planner
{
    public struct VariableRelation : IEqualityComparer<VariableRelation>
    {
        public Variable A { get; }
        public Variable Rel { get; }
        public Variable B { get; }
        public bool Negated { get; }

        public bool IsFullyBound { get { return A.IsBound && Rel.IsBound && B.IsBound; } }

        public VariableRelation(Variable a, Variable rel, Variable b, bool negated)
        {
            A = a;
            Rel = rel;
            B = b;
            Negated = negated;
        }

        public Fact GetProposition(uint[] rtParams)
        {
            uint a = A.IsFree? rtParams[A.Idx] : A.Id;
            uint rel = Rel.IsFree? rtParams[Rel.Idx] : Rel.Id;
            uint b = B.IsFree? rtParams[B.Idx] : B.Id;

            return new Fact(a, rel, b);
        }

        public bool Equals(VariableRelation x, VariableRelation y)
        {
            return
                x.A.Equals(y.A) &&
                x.Rel.Equals(y.Rel) &&
                x.B.Equals(y.B) &&
                x.Negated == y.Negated;
        }

        public int GetHashCode(VariableRelation obj)
        {
            return
                obj.A.GetHashCode() ^ obj.Rel.GetHashCode() ^ obj.B.GetHashCode() ^ Negated.GetHashCode();
        }

        public VariableRelation Rebind(Variable[] newVars)
        {
            Func<Variable, Variable> RebindVar = (v) => v.IsFree ? newVars[v.Idx] : v;
            return new VariableRelation(RebindVar(A), RebindVar(Rel), RebindVar(B), Negated);
        }

        public override string ToString()
        {
            string ret = $"{A} {Rel} {B}";
            return Negated ? "NOT " + ret : ret;
        }

        public Fact? ToFact()
        {
            if (IsFullyBound)
            {
                return new Fact(A.Id, Rel.Id, B.Id, !Negated);
            }
            else
            {
                return null;
            }
        }
    }

    //[x][21][21][21]
    //A,Rel,B,
    public struct Fact
        : IEqualityComparer<Fact>, IEquatable<Fact>
    {
        public UInt64 Id { get; }
        public uint AId { get; }
        public uint RelId { get; }
        public uint BId { get; }
        public bool Truth { get; }

        private static uint MaxVal = (uint)(Math.Pow(2, 21) - 1);

        public Fact(uint aId, uint relId, uint bId, bool truth = true)
        {
            AId = aId;
            RelId = relId;
            BId = bId;
            Id = 0;
            Truth = truth;
            Id = GenerateId(this);
        }

        public static UInt64 GenerateId(Fact p)
        {
            return GenerateId(p.AId, p.RelId, p.BId, p.Truth);
        }

        public static UInt64 GenerateId(uint a, uint rel, uint b, bool truth)
        {
            UInt64 id = a;
            id = id << 21;

            id = id | rel;
            id = id << 21;

            id = id | b;
            id = id << 1;

            id = id | (truth ? (ulong)1 : (ulong)0);

            return id;
        }

        public bool Equals(Fact x, Fact y)
        {
            return x.AId == y.AId && x.RelId == y.RelId && x.BId == y.BId;
        }

        public int GetHashCode(Fact obj)
        {
            return Id.GetHashCode();
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            var A = Ids.IdToName[AId];
            var B = Ids.IdToName[BId];
            var Rel = Ids.IdToName[RelId];
            return Truth ? $"{A} {Rel} {B}" : $"NOT {A} {Rel} {B}";

        }

        public override bool Equals(object obj)
        {
            Fact other = (Fact)obj;
            return AId == other.AId && RelId == other.RelId && BId == other.BId;
        }

        public bool Equals(Fact other)
        {
            return AId == other.AId && RelId == other.RelId && BId == other.BId;
        }
    }

    public abstract class Variable : IEquatable<Variable>
    {
        public uint Id { get { return ((BoundVariable)this).Id; } }
        public int Idx { get { return ((UnboundVariable)this).Idx; } }
        public string Name { get; protected set; }
        public abstract bool Equals(Variable other);
        public bool IsFree { get { return this is UnboundVariable; } }
        public bool IsBound { get { return this is BoundVariable; } }
        public abstract Variable Clone();
    }

    public class BoundVariable : Variable
    {
        public uint Id { get; }

        public BoundVariable(uint id)
        {
            Id = id;
            Name = Ids.IdToName[id];
        }

        public override bool Equals(Variable other)
        {
            BoundVariable otherLp = other as BoundVariable;
            if (otherLp == null) return false;
            return otherLp.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override Variable Clone()
        {
            return new BoundVariable(Id);
        }

        public override string ToString()
        {
            return Name.ToLower();
        }
    }

    public class UnboundVariable : Variable
    {
        public int Idx { get; }

        public UnboundVariable(int idx, string name)
        {
            Idx = idx;
            Name = name;
        }

        public override bool Equals(Variable other)
        {
            UnboundVariable otherVp = other as UnboundVariable;
            if (otherVp == null) return false;
            return otherVp.Idx == Idx;
        }

        public override int GetHashCode()
        {
            return Idx.GetHashCode();
        }

        public override Variable Clone()
        {
            return new UnboundVariable(Idx, Name);
        }

        public override string ToString()
        {
            return Name.ToUpper();
        }
    }
}
