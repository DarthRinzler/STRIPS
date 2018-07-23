using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STRIPS
{
	public abstract class Expression
	{
		public abstract bool Evaluate(SObject[] parameters, Dictionary<string, SObject> world, out Expression failExpr);
		public abstract void Apply(SObject[] parameters, Dictionary<string, SObject> world, bool invert);
        public abstract string Print(SObject[] parameters);
	}

	public class AndExpression : Expression
	{
        public Expression[] Expressions;

		public AndExpression(params Expression[] exprs)
		{
            Expressions = exprs;
		}

		public override bool Evaluate(SObject[] parameters, Dictionary<string, SObject> world, out Expression failExpr)
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

		public override void Apply(SObject[] parameters, Dictionary<string, SObject> world, bool invert)
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
    }

	public class NotExpression : Expression
	{
		public Expression Expr { get; set; }

		public NotExpression(Expression expr)
		{
			Expr = expr;
		}

		public override bool Evaluate(SObject[] parameters, Dictionary<string, SObject> world, out Expression failExpr)
		{
            failExpr = null;
			if (Expr.Evaluate(parameters, world, out failExpr))
            {
                failExpr = this;
                return false;
            }
            return true;
		}

		public override void Apply(SObject[] parameters, Dictionary<string, SObject> world, bool invert)
		{
			Expr.Apply(parameters, world, !invert);
		}

        public override string Print(SObject[] parameters)
        {
            return "NOT("+Expr.Print(parameters)+")";
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

		public override bool Evaluate(SObject[] parameters, Dictionary<string, SObject> world, out Expression failExpr)
		{
            failExpr = null;
            bool ret = false;
            foreach (var expr in Expressions)
            {
                ret |= expr.Evaluate(parameters, world, out failExpr);
            }
            if (!ret)
            {
                failExpr = this;
                return false;
            }
            else return true;
		}

		public override void Apply(SObject[] parameters, Dictionary<string, SObject> world, bool invert)
		{
            var idx = _rand.Next(0, Expressions.Length);
            Expressions[idx].Apply(parameters, world, invert);
		}

        public override string Print(SObject[] parameters)
        {
            return "OR(\r\n"+Expressions.Select(e => e.Print(parameters)).Aggregate((a, e) => a + "\r\n" + e)+"\r\n)";
        }
    }

	public class BooleanPredicateExpression : Expression
	{
		public string ObjectName { get; }
		public string PropertyName { get; }
        private int ObjectIdx { get; set; }

		public BooleanPredicateExpression(string objectName, string propertyName, int objectIdx)
		{
			ObjectName = objectName;
			PropertyName = propertyName;
            ObjectIdx = objectIdx;
		}

		public override bool Evaluate(SObject[] parameters, Dictionary<string, SObject> world, out Expression failExpr)
		{
            failExpr = null;
			SObject sobj = parameters[ObjectIdx];
            if (!sobj.Properties.ContainsKey(ObjectName))
            {
                failExpr = this;
                return false;
            }
            else return true;
		}

		public override void Apply(SObject[] parameters, Dictionary<string, SObject> world, bool invert)
		{
			SObject sobj = parameters[ObjectIdx];
            if (!invert) sobj.Properties[ObjectName] = null;
            else sobj.Properties.Remove(ObjectName);
		}

        public override string Print(SObject[] parameters)
        {
            return parameters[ObjectIdx].Name+ "." + PropertyName;
        }
    }

	public class KeyValuePredicateExpression : Expression
	{
		public string ObjectName { get; }
		public string PropertyName { get; }
		public string PropertyValue { get; }
        private int ObjectIdx { get; set; }
        private int? PropertyValueIdx { get; set; }

		public KeyValuePredicateExpression(string objectName, string propertyName, string propertyValue, int objectIdx, int? propertyValueIdx = null)
		{
			ObjectName = objectName;
			PropertyName = propertyName;
			PropertyValue = propertyValue;
            ObjectIdx = objectIdx;
            PropertyValueIdx = propertyValueIdx;
		}

        public override bool Evaluate(SObject[] parameters, Dictionary<string, SObject> world, out Expression failExpr)
        {
            failExpr = null;
            SObject sobj = parameters[ObjectIdx];
            SObject val = null;
            bool ret = false;

            if (sobj.Properties.TryGetValue(PropertyName, out val))
            {
                // Reference
                if (PropertyValueIdx.HasValue)
                {
                    ret = val.Name == parameters[PropertyValueIdx.Value].Name;
                    if (!ret)
                    {
                        failExpr = this;
                    }
                }
                // Literal
                else
                {
                    SObject pval = null;
                    if (world.TryGetValue(PropertyValue, out pval))
                    {
                        ret = val == pval; 
                        if (!ret)
                        {
                            failExpr = this;
                        }
                    }
                }
            }
            return ret;
        }

		public override void Apply(SObject[] parameters, Dictionary<string, SObject> world, bool invert)
		{
            SObject sobj = parameters[ObjectIdx];

            // Reference
            if (PropertyValueIdx.HasValue)
            {
                if (invert) 
                {
                    if (sobj[PropertyName].Name == parameters[PropertyValueIdx.Value].Name)
                    {
                        sobj.Properties.Remove(PropertyName);
                    }
                }
                else
                {
                    sobj[PropertyName] = parameters[PropertyValueIdx.Value];
                }
            }
            // Literal
            else
            {
                SObject pval = null;
                if (world.TryGetValue(PropertyValue, out pval))
                {
                    if (invert && sobj[PropertyName] == pval)
                    {
                        sobj.Properties.Remove(PropertyName);
                    }
                    else sobj[PropertyName] = pval;
                }
            }
		}

        public override string Print(SObject[] parameters)
        {
            var sobj = parameters[ObjectIdx];
            string pval = PropertyValue;

            if (PropertyValueIdx.HasValue)
            {
                pval = parameters[PropertyValueIdx.Value].Name;
            }

            return String.Format("{0}.{1} {2}", sobj.Name, PropertyName, pval);
        }
    }
}
