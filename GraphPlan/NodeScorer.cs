using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPlan
{
    public class NodeScorer
    {
        private State _endState;
        private State _startState;
        private IEnumerable<ActionDefinition> _actionDefs;
        private IEnumerable<ActionDefinition> _reverseActionDefs;

        public NodeScorer(State start, State end, IEnumerable<ActionDefinition> actionDefs)
        {
            _endState = end;
            _startState = start;
            _actionDefs = actionDefs;

            _reverseActionDefs = CalculateReverseActionDefs(actionDefs);
            var reverseActions = _endState
                .GetAllActions(_reverseActionDefs)
                .ToArray();

            var PropDefDependentActions = reverseActions
                .SelectMany(ra => ra.Definition.PositivePostconditions.Concat(ra.Definition.NegativePostconditions))
                .Select(GetDependentActions);
        }

        private IEnumerable<ActionDefinition> GetDependentActions(PropositionDefinition pd)
        {
            return Enumerable.Empty<ActionDefinition>(); 
        }

        private IEnumerable<ActionDefinition> CalculateReverseActionDefs(IEnumerable<ActionDefinition> actionDefs)
        {
            // Reverse all actionable actions
            return actionDefs
                .Where(ad => ad.IsActionable)
                .Select(ReverseActionDef)
                .ToArray();
        }

        private ActionDefinition ReverseActionDef(ActionDefinition actionDef)
        {
            var reversedPosPre = new HashSet<PropositionDefinition>();
            var reversedNegPre = new HashSet<PropositionDefinition>();
            var reversedPosPost = new HashSet<PropositionDefinition>();
            var reversedNegPost = new HashSet<PropositionDefinition>();

            // Copy over all positive preconditions that are NOT negated in the negative postconditions
            /*
            foreach (var posPre in actionDef.PositivePreconditions)
            {
                if (actionDef.NegativePostconditions.All(negPost => !negPost.EqualsIgnoreNegation(posPre)))
                {
                    reversedPosPre.Add(posPre);
                }
            }

            // Copy over all negative preconditions that are NOT negated in the positive postconditions
            foreach (var negPre in actionDef.NegativePreconditions)
            {
                if (actionDef.PositivePostconditions.All(posPost => !posPost.EqualsIgnoreNegation(negPre)))
                {
                    reversedNegPre.Add(negPre);
                }
            }
            */

            // Copy over postConditions as preConditions
            foreach (var posPost in actionDef.PositivePostconditions)
            {
                reversedPosPre.Add(posPost);
            }
            foreach (var negPost in actionDef.NegativePostconditions)
            {
                reversedNegPre.Add(negPost);
            }

            // Copy over preConditions as postConditions
            foreach (var posPre in actionDef.PositivePreconditions)
            {
                reversedPosPost.Add(posPre);
            }
            foreach (var negPre in actionDef.NegativePreconditions)
            {
                reversedNegPost.Add(negPre);
            }

            // Add back unaltered preconditions
            return new ActionDefinition(
                $"_{actionDef.Name}_reversed",
                actionDef.CtParams,
                reversedPosPre,
                reversedNegPre,
                reversedPosPost,
                reversedNegPost
            );
        }

        public double GetNodeScore(Node node)
        {
            return 0;
        }
    }
}
