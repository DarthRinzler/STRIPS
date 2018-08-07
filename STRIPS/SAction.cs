using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STRIPS
{
	public class SAction
	{
		public string Name { get; private set; }
		public Expression Precondition { get; private set; }
		public Expression Effect { get; private set; }

		public SAction(string name, Expression precondition, Expression effect)
		{
			Name = name;
			Precondition = precondition;
			Effect = effect;
		}

		public bool CanApply(SObject[] parameters, SObject world, out Expression failExpr)
		{
			return Precondition.Evaluate(parameters, world, out failExpr);
		}

		public void Apply(SObject[] parameters, SObject world)
		{
			Effect.Apply(parameters, world, false);
		}
	}
}
