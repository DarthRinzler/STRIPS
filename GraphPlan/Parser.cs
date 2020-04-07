using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GraphPlan
{
    public class Parser
    {
        private Dictionary<string, ActionDefinition> _baseActions = new Dictionary<string, ActionDefinition>();

        public State ParseState(string path)
        {
            var propositions = new Dictionary<UInt64, Proposition>();

            using (var sr = new FileStream(path, FileMode.Open))
            {
                var tok = new Tokenizer(sr, path);

                while(tok.PeekToken() != null)
                {
                    var p = ParseProposition(tok);
                    propositions[p.Id] = p;
                }
            }

            return new State(propositions);
        }

        public IEnumerable<ActionDefinition> ParseActions(string path)
        {
            var ret = new List<ActionDefinition>();

            using (var sr = new FileStream(path, FileMode.Open))
            {
                var tok = new Tokenizer(sr, path);
                while(tok.PeekToken() != null)
                {
                    var action = ParseAction(tok);
                    if (!action.IsDependent)
                    {
                        ret.Add(action);
                    }
                }
            }

            return ret;
        }

        private Proposition ParseProposition(Tokenizer tok)
        {
            tok.Consume(TokenType.LParen);

            bool truth = true;
            if (tok.PeekType() == TokenType.Not)
            {
                tok.Consume(TokenType.Not);
                tok.Consume(TokenType.LParen);
                truth = false;
            }

            string name = tok.Consume(TokenType.Id).Value;
            if (name.Contains('.'))
            {
                
            }
            uint nameId = Ids.GetId(name);

            string prop = tok.Consume(TokenType.Id).Value;
            uint propId = Ids.GetId(prop);

            string val = tok.Consume(TokenType.Id).Value;
            uint valId = Ids.GetId(val);
            tok.Consume(TokenType.RParen);

            if (!truth) tok.Consume(TokenType.RParen);

            return new Proposition(nameId, propId, valId, truth);
        }

        private ActionDefinition ParseAction(Tokenizer tok)
        {
            var posPre = new HashSet<PropositionDefinition>();
            var negPre = new HashSet<PropositionDefinition>();
            var posPost = new HashSet<PropositionDefinition>();
            var negPost = new HashSet<PropositionDefinition>();

            // Action Name
            tok.Consume(TokenType.LParen);
            string name = tok.Consume(TokenType.Id).Value;

            // Signature
            var parameters = ParseSignature(tok);

            bool isDependentAction = false;
            bool isAutoExecute = false;

            // Optional Dependent Actions
            while (tok.PeekType() == TokenType.LParen)
            {
                tok.Consume(TokenType.LParen);

                // Dependent Action
                if (tok.PeekType() == TokenType.Id)
                {
                    ParseDependentAction(tok, parameters, posPre, negPre, posPost, negPost);
                }
                // Pre Expression
                else if (tok.PeekType() == TokenType.Pre)
                {
                    tok.Consume(TokenType.Pre);
                    if (tok.PeekToken().Type != TokenType.RParen)
                    {
                        var propDefs = ParseConjuction(parameters, tok);
                        foreach (var pd in propDefs)
                        {
                            var b = pd.Negated ? negPre.Add(pd) : posPre.Add(pd);
                        }
                    }
                }
                // Post Expression
                else if (tok.PeekType() == TokenType.Post)
                {
                    tok.Consume(TokenType.Post);
                    if (tok.PeekToken().Type != TokenType.RParen)
                    {
                        var propDefs = ParseConjuction(parameters, tok);
                        foreach (var pd in propDefs)
                        {
                            var b = pd.Negated ? negPost.Add(pd) : posPost.Add(pd);
                        }
                    }
                }
                // AutoExecute Flag
                else if (tok.PeekType() == TokenType.Auto)
                {
                    tok.Consume(TokenType.Auto);
                    isAutoExecute = true;

                }
                // Dependent Action Flag
                else if (tok.PeekType() == TokenType.Dep)
                {
                    tok.Consume(TokenType.Dep);
                    isDependentAction = true;
                }
                    
                tok.Consume(TokenType.RParen);
            }

            tok.Consume(TokenType.RParen);

            var ret = new ActionDefinition(
                name,
                parameters, 
                posPre, 
                negPre, 
                posPost, 
                negPost,
                isDependentAction,
                isAutoExecute
            );

            // Cache all dependent actions for future parsing
            if (isDependentAction)
            {
                _baseActions.Add(ret.Name, ret);
            }

            return ret;
        }

        private void ParseDependentAction(
            Tokenizer tok, 
            Dictionary<string,int> parameters,
            HashSet<PropositionDefinition> posPre,
            HashSet<PropositionDefinition> negPre,
            HashSet<PropositionDefinition> posPost,
            HashSet<PropositionDefinition> negPost) 
        {
            var dependentActionName = tok.Consume(TokenType.Id).Value;
            if (!_baseActions.ContainsKey(dependentActionName))
            {
                throw new Exception($"Unable to find Dependent Action {dependentActionName}");
            }

            var dependentActionDef = _baseActions[dependentActionName];
            var dependentSignature = ParseSignature(tok);

            var refMapping = dependentSignature
                .Where(kvp => parameters.ContainsKey(kvp.Key))
                .ToDictionary(kvp => kvp.Value, kvp => parameters[kvp.Key]);

            var litMapping = dependentSignature
                .Where(kvp => !parameters.ContainsKey(kvp.Key))
                .ToDictionary(kvp => kvp.Value, kvp => Ids.GetId(kvp.Key));

            var depPosPrecond = dependentActionDef
                .PositivePreconditions
                .Select(p => ReMapPropositionDef(p, refMapping, litMapping));
            foreach (var dp in depPosPrecond) posPre.Add(dp);

            var depNegPrecond = dependentActionDef
                .NegativePreconditions
                .Select(p => ReMapPropositionDef(p, refMapping, litMapping));
            foreach (var dp in depNegPrecond) negPre.Add(dp);

            var depPosPostcond = dependentActionDef
                .PositivePostconditions
                .Select(p => ReMapPropositionDef(p, refMapping, litMapping));
            foreach (var dp in depPosPostcond) posPost.Add(dp);

            var depNegPostcond = dependentActionDef
                .NegativePostconditions
                .Select(p => ReMapPropositionDef(p, refMapping, litMapping));
            foreach (var dp in depNegPostcond) negPost.Add(dp);
        }

        private Dictionary<string, int> ParseSignature(Tokenizer tok)
        {
            var ret = new Dictionary<string, int>();
            tok.Consume(TokenType.LParen);
            int counter = 0;
            while (tok.PeekToken().Type != TokenType.RParen)
            {
                string param = tok.Consume(TokenType.Id).Value;
                ret[param] = counter++;
            }
            tok.Consume(TokenType.RParen);
            return ret;
        }

        private IList<PropositionDefinition> ParseConjuction(Dictionary<string, int> parameters, Tokenizer tok)
        {
            var predicates = new List<PropositionDefinition>();
            while (tok.PeekToken().Type != TokenType.RParen)
            {
                PropositionDefinition pred = ParsePropositionDefinition(parameters, tok);
                predicates.Add(pred);
            }

            return predicates;
        }

        private PropositionDefinition ParsePropositionDefinition(Dictionary<string, int> parameters, Tokenizer tok)
        {
            bool negated = false;
            tok.Consume(TokenType.LParen);

            if (tok.PeekType() == TokenType.Not)
            {
                tok.Consume(TokenType.Not);
                tok.Consume(TokenType.LParen);
                negated = true;
            }

            string nameStr = tok.Consume(TokenType.Id).Value;
            string propStr = tok.Consume(TokenType.Id).Value;
            string valStr = tok.Consume(TokenType.Id).Value;
            tok.Consume(TokenType.RParen);

            if (negated) tok.Consume(TokenType.RParen);

            return GetPropDef(parameters, negated, nameStr, propStr, valStr);
        }

        private static PropositionDefinition GetPropDef(Dictionary<string, int> parameters, bool negated, string nameStr, string propStr, string valStr)
        {
            CtParameter name = new CtParameter();
            name.ParamName = nameStr;
            if (parameters.ContainsKey(nameStr)) name.Idx = parameters[nameStr];
            else name.Id = Ids.GetId(nameStr);

            CtParameter property = new CtParameter();
            property.ParamName = propStr;
            if (parameters.ContainsKey(propStr)) property.Idx = parameters[propStr];
            else property.Id = Ids.GetId(propStr);

            CtParameter value = new CtParameter();
            value.ParamName = valStr;
            if (parameters.ContainsKey(valStr)) value.Idx = parameters[valStr];
            else value.Id = Ids.GetId(valStr);

            return new PropositionDefinition(name, property, value, negated);
        }

        private PropositionDefinition ReMapPropositionDef(PropositionDefinition propDef, Dictionary<int,int> refMapping, Dictionary<int,uint> litMapping)
        {
            CtParameter name = new CtParameter();
            name.ParamName = propDef.Name.ParamName;    
            if (propDef.Name.IsVariableRef && !litMapping.ContainsKey(propDef.Name.Idx.Value)) {
                name.Idx = refMapping[propDef.Name.Idx.Value];
                name.Id = null;
            }
            else if (propDef.Name.IsVariableRef && litMapping.ContainsKey(propDef.Name.Idx.Value))
            {
                name.Id = litMapping[propDef.Name.Idx.Value];
            }
            else {
                name.Id = propDef.Name.Id;
                name.Idx = null;
            }

            CtParameter property = new CtParameter();
            property.ParamName = propDef.Property.ParamName;
            if (propDef.Property.IsVariableRef && !litMapping.ContainsKey(propDef.Property.Idx.Value)) {
                property.Idx = refMapping[propDef.Property.Idx.Value];
                property.Id = null;
            }
            else if (propDef.Property.IsVariableRef && litMapping.ContainsKey(propDef.Property.Idx.Value))
            {
                property.Id = litMapping[propDef.Property.Idx.Value];
            }
            else {
                property.Id = propDef.Property.Id;
                property.Idx = null;
            }

            CtParameter value = new CtParameter();
            value.ParamName = propDef.Value.ParamName;  
            if (propDef.Value.IsVariableRef && !litMapping.ContainsKey(propDef.Value.Idx.Value)) {
                value.Idx = refMapping[propDef.Value.Idx.Value];
                value.Id = null;
            }
            else if (propDef.Value.IsVariableRef && litMapping.ContainsKey(propDef.Value.Idx.Value))
            {
                value.Id = litMapping[propDef.Value.Idx.Value];
            }
            else {
                value.Id = propDef.Value.Id;
                value.Idx = null;
            }

            Func<CtParameter, bool> isValid = (ct) => (ct.Id != null) ^ (ct.Idx != null);
            if (!isValid(name) || !isValid(property) || !isValid(value))
            {
                Console.WriteLine("here");
            }

            var ret = new PropositionDefinition(name, property, value, propDef.Negated);
            return ret;
        }

        private void Error(TokenType expected, TokenType actual)
        {
            throw new Exception(String.Format("Unexpected token found. Expected '{0}' Actual '{1}'", expected, actual));
        }
    }
}
