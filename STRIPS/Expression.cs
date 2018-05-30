using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STRIPS
{
	public abstract class Expression
	{
		public abstract bool Evaluate(SObject world);
		public abstract void Apply(SObject world, bool invert);
	}

	public class AndExpression : Expression
	{
        public Expression[] Expressions;

		public AndExpression(params Expression[] exprs)
		{
            Expressions = exprs;
		}

		public override bool Evaluate(SObject world)
		{
            return Expressions.All(e => e.Evaluate(world));
		}

		public override void Apply(SObject world, bool invert)
		{
            foreach (var expr in Expressions)
            {
                expr.Apply(world, invert);
            }
		}
	}

	public class NotExpression : Expression
	{
		public Expression Expr { get; set; }

		public NotExpression(Expression expr)
		{
			Expr = expr;
		}

		public override bool Evaluate(SObject world)
		{
			return !Expr.Evaluate(world);
		}

		public override void Apply(SObject world, bool invert)
		{
			Expr.Apply(world, !invert);
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

		public override bool Evaluate(SObject world)
		{
            return Expressions.Any(e => e.Evaluate(world));
		}

		public override void Apply(SObject world, bool invert)
		{
            var idx = _rand.Next(0, Expressions.Length);
            Expressions[idx].Apply(world, invert);
		}
	}

	public class BooleanPredicateExpression : Expression
	{
		public string ObjectName { get; }
		public string PropertyName { get; }

		public BooleanPredicateExpression(string objectName, string propertyName)
		{
			ObjectName = objectName;
			PropertyName = propertyName;
		}

		public override bool Evaluate(SObject world)
		{
			SObject sobj = null;
			if (world.Properties.TryGetValue(ObjectName, out sobj))
			{
				return sobj.Properties.ContainsKey(ObjectName);
			}
			return false;
		}

		public override void Apply(SObject world, bool invert)
		{
			SObject sobj = null;
			if (world.Properties.TryGetValue(PropertyName, out sobj))
			{
				if (!invert) sobj.Properties[ObjectName] = null;
				else sobj.Properties.Remove(ObjectName);
			}
		}
	}

	public class KeyValuePredicateExpression : Expression
	{
		public string ObjectName { get; }
		public string PropertyName { get; }
		public string PropertyValue { get; }

		public KeyValuePredicateExpression(string objectName, string propertyName, string propertyValue)
		{
			ObjectName = objectName;
			PropertyName = propertyName;
			PropertyValue = propertyValue;
		}

		public override bool Evaluate(SObject world)
		{
			SObject sobj = null;
			if (world.Properties.TryGetValue(PropertyName, out sobj))
			{
				SObject val = null;
				if (sobj.Properties.TryGetValue(ObjectName, out val))
				{
					return val.Name.Equals(PropertyValue);
				}
			}

			return false;
		}

		public override void Apply(SObject world, bool invert)
		{
			SObject sobj = null;
			if (world.Properties.TryGetValue(PropertyName, out sobj))
			{
				SObject val = null;
				if (sobj.Properties.TryGetValue(ObjectName, out val))
				{
					if (!invert) val.Name = PropertyValue;
					else val.Name = null;
				}
			}
		}
	}
}
