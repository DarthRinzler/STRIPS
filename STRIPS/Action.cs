using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STRIPS
{
	public class Action
	{
        private static string[] padding = new string[] { "", "\t", "\t\t", "\t\t\t", "\t\t\t\t" };

		public string Name { get; private set; }
        public List<string> Parameters { get; private set; }
		public Conjunction Precondition { get; private set; }
		public Conjunction Effect { get; private set; }

		public Action(string name, List<string> parameters, Conjunction precondition, Conjunction effect)
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

        public IList<ActionInst> GetActionInstances(SObject world)
        {
            var p = new SObject[Parameters.Count];
            var candidates = new List<ActionInst>();
            Combinations(p, world, 0, candidates);
            return candidates;
        }

        public void Combinations(SObject[] parameters, SObject world, int candidateParamIdx, List<ActionInst> candidates)
        {
            foreach (SObject candidateParam in world.Properties.Values)
            {
                // Skip duplicate parameters
                for (int i = 0; i < candidateParamIdx; i++) if (parameters[i].Name == candidateParam.Name) goto cont;

                parameters[candidateParamIdx] = candidateParam;

                //Console.Write("\n{0}{1}:", padding[candidateParamIdx], candidateParam.Name);
                // Filter out parameters that are invalid
                if (!IsCandidateParam(parameters, candidateParamIdx, world))
                {
                    //Console.WriteLine("no");
                    continue;
                }

                // If all parameters are valid candidates
                if (candidateParamIdx == parameters.Length - 1)
                {
                    var parameterListCandidate = new SObject[parameters.Length];
                    Array.Copy(parameters, parameterListCandidate, parameters.Length);
                    ActionInst inst = new ActionInst(this, parameterListCandidate);
                    candidates.Add(inst);
                    //Console.WriteLine("yes");
                }
                // Else try out next parameter candidate
                else
                {
                    Combinations(parameters, world, candidateParamIdx + 1, candidates);
                }

                cont:;
            }
        }

        public bool IsCandidateParam(SObject[] parameters, int candidateParamIdx, SObject world)
        {
            foreach (Expression expr in Precondition.Expressions)
            {
                if (expr is Predicate)
                {
                    var predicate = expr as Predicate;
                    if (!CandidateValid(parameters, predicate, candidateParamIdx, world, false))
                    {
                        return false;
                    }
                }
                else if (expr is NotExpression)
                {
                    var notExpr = expr as NotExpression;
                    if (!CandidateValid(parameters, notExpr.Expr, candidateParamIdx, world, true))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool CandidateValid(SObject[] parameters, Predicate predicate, int candidateParamIdx, SObject world, bool invert)
        {
            var refParams = predicate.Params;
            SObject last = null;
            SObject cur = null;

            // Filter out predicates not referencing this candidateParam
            for (int candidateRefIdx = 0; candidateRefIdx < refParams.Count; candidateRefIdx++)
            {
                KV key = refParams[candidateRefIdx];

                // If reference is literal 
                if (key.ParamIdx < 0)
                {
                    if (last != null)
                    {
                        if (!last.TryGetValue(key.Name, out cur))
                        {
                            return invert;
                        }
                    }
                    else
                    {
                        cur = world[key.Name];
                    }
                }
                // If reference has candidate
                else if (key.ParamIdx <= candidateParamIdx)
                {
                    cur = parameters[key.ParamIdx];
                }
                // else we cannot rule it out
                else
                {
                    return true;
                }

                if (last != null)
                {
                    if (!last.ContainsKey(cur.Name))
                    {
                        return invert;
                    }
                }

                last = cur;
            }

            return !invert;
        }

        public override string ToString()
        {
            return String.Format("{0}", this.Name);
        }
    }

    public class ActionInst
    {
        public Action Definition { get; set; }
        public SObject[] Parameters { get; set; }

        public ActionInst(Action def, SObject[] parameters)
        {
            Definition = def;
            Parameters = parameters;
        }

        public override string ToString()
        {
            return String.Format("{0} {1}", Definition.Name, String.Join(" ", Parameters.Select(p => p.Name)));
        }
    }
}
