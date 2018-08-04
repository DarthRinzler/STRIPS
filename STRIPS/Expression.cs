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
        public abstract bool GetAllFailExprs(SObject[] runtimeParams, SObject world, List<Expression> failExprs);
		public abstract void Apply(SObject[] runtimeParams, SObject world, bool invert);
        public abstract string Print(SObject[] parameters);
	}

	public class AndExpression : Expression
	{
        public Expression[] Expressions;

		public AndExpression(params Expression[] exprs)
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

        public override bool GetAllFailExprs(SObject[] runtimeParams, SObject world, List<Expression> failExprs)
        {
            bool failed = false;
            foreach (var expr in Expressions)
            {
                if (!expr.GetAllFailExprs(runtimeParams, world, failExprs))
                {
                    failed = true;
                    failExprs.Add(expr);
                }
            }
            return failed;
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
    }

	public class NotExpression : Expression
	{
		public Expression Expr { get; set; }

		public NotExpression(Expression expr)
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

        public override bool GetAllFailExprs(SObject[] runtimeParams, SObject world, List<Expression> failExprs)
        {
            return !Expr.GetAllFailExprs(runtimeParams, world, failExprs);
        }

		public override void Apply(SObject[] parameters, SObject world, bool invert)
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

        public override bool GetAllFailExprs(SObject[] runtimeParams, SObject world, List<Expression> failExprs)
        {
            bool ret = false;
            foreach (var expr in Expressions)
            {
                ret |= expr.GetAllFailExprs(runtimeParams, world, failExprs);
            }

            if (!ret)
            {
                failExprs.Add(this);
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

		public override bool Evaluate(SObject[] runtimeParams, SObject world, out Expression failExpr)
		{
            failExpr = null;
			SObject sobj = runtimeParams[ObjectIdx];
            if (!sobj.Properties.ContainsKey(ObjectName))
            {
                failExpr = this;
                return false;
            }
            else return true;
		}

        public override bool GetAllFailExprs(SObject[] runtimeParams, SObject world, List<Expression> failExprs)
        {
			SObject sobj = runtimeParams[ObjectIdx];
            return sobj.Properties.ContainsKey(ObjectName);
        }

		public override void Apply(SObject[] parameters, SObject world, bool invert)
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

    public class ReferenceExpression : Expression
    {
        private List<KV> _params;

        public ReferenceExpression(List<KV> parameters)
        {
            _params = parameters;
        }

        public override void Apply(SObject[] runtimeParams, SObject world, bool invert)
        {
            SObject key = world;

            for (int i=0; i<_params.Count; i++)
            {
                var p = _params[i];
                string keyName = null;
                if (p.Idx >= 0)
                {
                    keyName = runtimeParams[p.Idx].Name;
                }
                else
                {
                    keyName = p.Key;
                }

                SObject val = null;
                if (!invert)
                { 
                    if (!key.Properties.TryGetValue(keyName, out val))
                    {
                        key[keyName] = new SObject(keyName);
                    }
                }
                else
                {
                    if (key.Properties.TryGetValue(keyName, out val) && i == _params.Count -1)
                    {
                        key.Properties.Remove(keyName);
                        return;
                    }
                }
                key = val;
            }
        }

        public override bool Evaluate(SObject[] runtimeParams, SObject world, out Expression failExpr)
        {
            failExpr = null;
            SObject key = world;

            for (int i=0; i<_params.Count; i++)
            {
                var p = _params[i];
                string keyName = null;
                if (p.Idx >= 0)
                {
                    keyName = runtimeParams[p.Idx].Name;
                }
                else
                {
                    keyName = p.Key;
                }

                if (!key.Properties.TryGetValue(keyName, out key))
                {
                    failExpr = this;
                    return false;
                }
            }

            return true;
        }

        public override bool GetAllFailExprs(SObject[] runtimeParams, SObject world, List<Expression> failExprs)
        {
            SObject key = world;

            for (int i=0; i<_params.Count; i++)
            {
                var p = _params[i];
                string keyName = null;
                if (p.Idx >= 0)
                {
                    keyName = runtimeParams[p.Idx].Name;
                }
                else
                {
                    keyName = p.Key;
                }

                if (!key.Properties.TryGetValue(keyName, out key))
                {
                    return false;
                }
            }

            return true;
        }

        public override string Print(SObject[] parameters)
        {
            return _params
                .Select(p => p.Idx >= 0 ? parameters[p.Idx].Name : p.Key)
                .Aggregate((a, e) => a + " " + e);
        }

        public override string ToString()
        {
            return _params.Select(p => p.Key).Aggregate((a, e) => a + " " + e);
        }
    }

    public class KV
    {
        public string Key { get; set; }
        public int Idx { get; set; }
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

        public override bool Evaluate(SObject[] parameters, SObject world, out Expression failExpr)
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
                    if (world.Properties.TryGetValue(PropertyValue, out pval))
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

		public override void Apply(SObject[] parameters, SObject world, bool invert)
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
                if (world.Properties.TryGetValue(PropertyValue, out pval))
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
