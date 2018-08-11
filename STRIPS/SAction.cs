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

        public IList<SObject[]> GetActionInstances(SObject world)
        {
            var p = new SObject[Parameters.Count];
            var candidates = new List<SObject[]>();
            Combinations(p, world, 0, candidates);
            return candidates;
        }

        /*
         *  foreach parameter p:
         *      foreach predicate d in PRE that contains p:
         *          if (d.idx == 0) check right
         *
         * NEED TO FILTER OUT INVALID FIRST PARAMETERS:w
         * 
         */
        string[] padding = new string[] { "", "\t", "\t\t" };
        public void Combinations(SObject[] parameters, SObject world, int candidateParamIdx, List<SObject[]> candidates)
        {
            foreach (SObject candidateParam in world.Properties.Values)
            {
                // Skip duplicate parameters
                for (int i = 0; i < candidateParamIdx; i++) if (parameters[i].Name == candidateParam.Name) goto cont;

                Console.WriteLine("{0}{1}", padding[candidateParamIdx], candidateParam.Name);
                // Filter out parameters that are invalid
                if (!IsCandidateParam(parameters, candidateParam, candidateParamIdx))
                {
                    continue;
                }

                parameters[candidateParamIdx] = candidateParam;

                // If all parameters are valid candidates
                if (candidateParamIdx == parameters.Length - 1)
                {
                    var parameterListCandidate = new SObject[parameters.Length];
                    Array.Copy(parameters, parameterListCandidate, parameters.Length);
                    candidates.Add(parameterListCandidate);
                }
                // Else try out next parameter candidate
                else
                {
                    Combinations(parameters, world, candidateParamIdx + 1, candidates);
                }

                cont:;
            }
        }

        public bool IsCandidateParam(SObject[] parameters, SObject candidateParam, int candidateParamIdx)
        {
            foreach (Expression expr in Precondition.Expressions)
            {
                if (expr is Predicate)
                {
                    var predicate = expr as Predicate;
                    if (!CandidateValid(parameters, predicate, candidateParam, candidateParamIdx))
                    {
                        return false;
                    }
                }
                else if (expr is NotExpression)
                {
                    var notExpr = expr as NotExpression;
                    if (CandidateValid(parameters, notExpr.Expr, candidateParam, candidateParamIdx))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool CandidateValid(SObject[] parameters, Predicate predicate, SObject candidateParam, int candidateParamIdx)
        {
            var refParams = predicate.Params;

            // Filter out predicates not referencing this candidateParam
            for (int candidateRefIdx = 0; candidateRefIdx < refParams.Count; candidateRefIdx++)
            {
                // If reference to candidateParam is found in predicate
                if (refParams[candidateRefIdx].ParamIdx == candidateParamIdx)
                {
                    // First Ref
                    if (candidateRefIdx == 0)
                    {
                        // If second parameter is chosen, filter on its value
                        if (candidateParamIdx > 0 && parameters.Length > 1)
                        {
                            var second = refParams[1];

                            string secondName = second.ParamIdx < 0 ? second.Key : parameters[second.ParamIdx].Name;
                            var matchingFirst = SObject.Refs[secondName];
                            if (matchingFirst.All(f => f.Name != candidateParam.Name))
                            {
                                return false; 
                            }
                        }
                    }
                    // Second Ref
                    else if (candidateRefIdx == 1)
                    {
                        // Filter out objects that do not reference candidate obj
                        var objs = SObject.Refs[candidateParam.Name];

                        var first = refParams[0];
                        var firstName = first.ParamIdx < 0 ? first.Key : parameters[first.ParamIdx].Name;
                        if (objs.All(o => o.Name != firstName))
                        {
                            return false;
                        }

                        // If third parameter is chose, filter on its value
                        if (candidateParamIdx > 1 && parameters.Length > 2)
                        {
                            var third = refParams[2];

                            string thirdName = third.ParamIdx < 0 ? third.Key : parameters[third.ParamIdx].Name;
                            var matchindSecond = SObject.Refs[thirdName];
                            if (matchindSecond.All(f => f.Name != candidateParam.Name))
                            {
                                return false;
                            }
                        }
                    }
                    // Third Ref
                    else if (candidateRefIdx == 2)
                    {
                        // Filter out objects that do not reference candidate obj
                        var objs = SObject.Refs[candidateParam.Name];

                        var second = refParams[1];

                        string secondName = null;
                        if (second.ParamIdx < 0)
                        {
                            secondName = second.Key;
                        }
                        else if (second.ParamIdx <= candidateParamIdx)
                        {
                            secondName = parameters[second.ParamIdx].Name;
                        }

                        if (objs.All(o => o.Name != secondName))
                        {
                            return false;
                        }
                    }
                    break;
                }
            }

            return true;
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
