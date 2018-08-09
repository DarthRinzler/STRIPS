using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STRIPS
{
	public class ActionDef
	{
		public string Name { get; private set; }
        public List<string> Parameters { get; private set; }
		public Conjunction Precondition { get; private set; }
		public Conjunction Effect { get; private set; }

		public ActionDef(string name, List<string> parameters, Conjunction precondition, Conjunction effect)
		{
			Name = name;
            Parameters = parameters;
            Precondition = precondition;
            Effect = effect;
		}

		public bool CanApply(SObject[] parameters, SObject world)
		{
            Expression e = null;
			return Precondition.Evaluate(parameters, world, out e);
		}

		public bool CanApply(SObject[] parameters, SObject world, out Expression failExpr)
		{
			return Precondition.Evaluate(parameters, world, out failExpr);
		}

		public void Apply(SObject[] parameters, SObject world)
		{
			Effect.Apply(parameters, world, false);
		}

        public IEnumerable<SObject[]> GetActionInstances(SObject world)
        {
            var p = new SObject[Parameters.Count];
            return Combinations(p, world, 0)
                .Where(c => CanApply(c, world));
        }

        public IEnumerable<SObject[]> Combinations(SObject[] parameters, SObject world, int idx)
        {
            if (idx == parameters.Length)
            {
                yield return parameters;
            }
            else
            {
                foreach (var obj in world.Properties.Values)
                {
                    foreach (var expr in Precondition.Expressions)
                    {
                        if (expr is Predicate)
                        {
                            var predicate = expr as Predicate;

                            var t = predicate.Params.Where(p => p.Idx == idx);
                            Console.WriteLine(t);
                        }
                        else if (expr is NotExpression)
                        {

                        }
                    }

                    parameters[idx] = obj;
                    var ret = Combinations(parameters, world, idx + 1);
                    foreach (var r in ret)
                    {
                        yield return r;
                    }
                }
            }
        }
	}

    public class ActionInst
    {
        public ActionDef Definition { get; set; }
        public SObject[] Parameters { get; set; }

        public ActionInst(ActionDef def, SObject[] parameters)
        {
            Definition = def;
            Parameters = parameters;
        }
    }
}
