using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STRIPS
{
	public abstract class Expression
	{
		public abstract bool Evaluate(SObject[] runtimeParams, SObject world, out Expression failExpr);
		public abstract void Apply(SObject[] runtimeParams, SObject world, bool invert);
        public abstract string Print(SObject[] parameters);
	}

	public class Conjunction : Expression
	{
        public Expression[] Expressions;

		public Conjunction(params Expression[] exprs)
		{
            Expressions = exprs;
		}

		public override bool Evaluate(SObject[] parameters, SObject world, out Expression failExpr)
		{
            failExpr = null;
            foreach (var expr in Expressions)
            {
                if (!expr.Evaluate(parameters, world, out failExpr))
                {
                    failExpr = expr;
                    return false;
                }
            }
            return true;
		}

		public override void Apply(SObject[] parameters, SObject world, bool invert)
		{
            foreach (var expr in Expressions)
            {
                expr.Apply(parameters, world, invert);
            }
		}

        public override string Print(SObject[] parameters)
        {
            return "AND(\r\n"+Expressions.Select(e => e.Print(parameters)).Aggregate((a, e) => a + "\r\n" + e)+"\r\n)";
        }

        public override string ToString()
        {
            return String.Join(" & ", Expressions.Select(e => e.ToString()));
        }
    }

	public class NotExpression : Expression
	{
		public Predicate Expr { get; set; }

		public NotExpression(Predicate expr)
		{
			Expr = expr;
		}

		public override bool Evaluate(SObject[] parameters, SObject world, out Expression failExpr)
		{
            failExpr = null;
			if (Expr.Evaluate(parameters, world, out failExpr))
            {
                failExpr = this;
                return false;
            }
            return true;
		}

		public override void Apply(SObject[] parameters, SObject world, bool invert)
		{
			Expr.Apply(parameters, world, !invert);
		}

        public override string Print(SObject[] parameters)
        {
            return "NOT("+Expr.Print(parameters)+")";
        }

        public override string ToString()
        {
            return "!(" + Expr.ToString() + ")";
        }
    }

	public class OrExpression : Expression
	{
        public Expression[] Expressions;
		private Random _rand;

		public OrExpression(params Expression[] exprs)
		{
            Expressions = exprs;
			_rand = new Random();
		}

		public override bool Evaluate(SObject[] runtimeParams, SObject world, out Expression failExpr)
		{
            failExpr = null;
            bool ret = false;
            foreach (var expr in Expressions)
            {
                ret |= expr.Evaluate(runtimeParams, world, out failExpr);
            }

            if (!ret)
            {
                failExpr = this;
                return false;
            }
            else return true;
		}

		public override void Apply(SObject[] parameters, SObject world, bool invert)
		{
            throw new NotImplementedException();
            //var idx = _rand.Next(0, Expressions.Length);
            //Expressions[idx].Apply(parameters, world, invert);
		}

        public override string Print(SObject[] parameters)
        {
            return "OR(\r\n"+Expressions.Select(e => e.Print(parameters)).Aggregate((a, e) => a + "\r\n" + e)+"\r\n)";
        }
    }

    public class Predicate : Expression
    {
        public List<KV> Params { get; private set; }

        public Predicate(List<KV> parameters)
        {
            Params = parameters;
        }

        public override void Apply(SObject[] runtimeParams, SObject world, bool invert)
        {
            SObject cur = world;

            for (int i=0; i<Params.Count; i++)
            {
                var p = Params[i];
                string nextName = null;
                if (p.ParamIdx >= 0)
                {
                    nextName = runtimeParams[p.ParamIdx].Name;
                }
                else
                {
                    nextName = p.Name;
                }

                SObject next = null;
                if (!invert)
                {
                    if (!cur.TryGetValue(nextName, out next))
                    {
                        if (!world.TryGetValue(nextName, out next))
                        {
                            next = new SObject(nextName);
                        }
                    }
                    cur[nextName] = next;
                }
                else
                {
                    if (cur.TryGetValue(nextName, out next) && i == Params.Count -1)
                    {
                        cur.Remove(nextName);
                        return;
                    }
                }
                cur = next;
            }
        }

        public override bool Evaluate(SObject[] runtimeParams, SObject world, out Expression failExpr)
        {
            failExpr = null;
            SObject key = world;

            for (int i=0; i<Params.Count; i++)
            {
                var p = Params[i];
                string keyName = null;
                if (p.ParamIdx >= 0)
                {
                    if (p.ParamIdx >= runtimeParams.Count())
                    {
                        failExpr = this;
                        return false;
                    }
                    keyName = runtimeParams[p.ParamIdx].Name;
                }
                else
                {
                    keyName = p.Name;
                }

                if (!key.TryGetValue(keyName, out key))
                {
                    failExpr = this;
                    return false;
                }
            }

            return true;
        }

        public override string Print(SObject[] parameters)
        {
            return Params
                .Select(p => p.ParamIdx >= 0 ? parameters[p.ParamIdx].Name : p.Name)
                .Aggregate((a, e) => a + " " + e);
        }

        public override string ToString()
        {
            return Params.Select(p => p.Name).Aggregate((a, e) => a + " " + e);
        }
    }

    public class PropertyIndex
    {
        public SObject Index { get; private set; }

        public PropertyIndex()
        {
            Index = new SObject("Index");
        }
    }

    public class KV
    {
        public string Name { get; set; }
        public int ParamIdx { get; set; }
    }
}
