using System;
using System.Collections.Generic;
using System.Text;

namespace PlannerGraph
{
    public struct Fact 
        : IEqualityComparer<Fact>
    {
        public UInt64 Id { get; }
        public uint AId { get; }
        public uint RelId { get; }
        public uint BId { get; }
        public bool Negated { get; }

        private static uint MaxVal = (uint)(Math.Pow(2, 21) - 1);

        public Fact(uint aId, uint relId, uint bId, bool negated=false)
        {
            AId = aId;
            RelId = relId;
            BId = bId;
            Id = 0;
            Negated = negated;
            Id = GenerateId(this);
        }

        public static UInt64 GenerateId(Fact p)
        {
            return GenerateId(p.AId, p.RelId, p.BId, p.Negated);
        }

        public static UInt64 GenerateId(uint a, uint rel, uint b, bool negated)
        {
            UInt64 id = a;
            id = id << 21;

            id = id | rel;
            id = id << 21;

            id = id | b;
            id = id << 1;

            id = id | (negated ? (ulong)0 : (ulong)1);

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

        public override string ToString()
        {
            var a = Ids.GetName(AId);
            var rel = Ids.GetName(RelId);
            var b = Ids.GetName(BId);
            return $"{a} {rel} {b}";
        }

        public Fact Clone()
        {
            return new Fact(AId, RelId, BId, Negated);
        }
    }
}
