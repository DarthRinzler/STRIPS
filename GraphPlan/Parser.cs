using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Planner
{
    public class Parser
    {
        private Dictionary<string, Dictionary<string, ActionDef>> _libs;
        private readonly string _libDir;

        public Parser(string libDir)
        {
            _libDir = libDir;
            _libs = new Dictionary<string, Dictionary<string, ActionDef>>();
        }

        public State ParseState(string fileName)
        {
            var ret = new State();

            var path = Path.Combine(_libDir, fileName);
            using (var sr = new FileStream(path, FileMode.Open))
            {
                var tok = new Tokenizer(sr, path);

                while (tok.PeekToken() != null)
                {
                    var f = ParseFact(tok);
                    ret.AddFact(f);
                }
            }

            return ret;
        }

        public Dictionary<string, ActionDef> ParseActionFile(string fileName)
        {
            var parsedActions = new Dictionary<string, ActionDef>();
            var path = Path.Combine(_libDir, fileName);

            using (var sr = new FileStream(path, FileMode.Open))
            {
                var tok = new Tokenizer(sr, path);

                while (tok.PeekToken() != null)
                {
                    tok.Consume(TokenType.LParen);
                    if (tok.PeekType() == TokenType.Import)
                    {
                        ParseImport(tok);
                    }
                    else if (tok.PeekType() == TokenType.Id)
                    {
                        var actionDef = ParseActionDef(tok, parsedActions);
                        parsedActions.Add(actionDef.Name, actionDef);
                    }
                }
            }

            return parsedActions;
        }

        private void ParseImport(Tokenizer tok)
        {
            tok.Consume(TokenType.Import);
            string libName = tok.Consume(TokenType.Id).Value;

            if (!_libs.ContainsKey(libName))
            {
                var lib = ParseActionFile(libName + ".txt");
                _libs[libName] = lib;
            }

            tok.Consume(TokenType.RParen);
        }

        private Fact ParseFact(Tokenizer tok)
        {
            tok.Consume(TokenType.LParen);

            bool truth = true;
            if (tok.PeekType() == TokenType.Not)
            {
                tok.Consume(TokenType.Not);
                tok.Consume(TokenType.LParen);
                truth = false;
            }

            string a = tok.Consume(TokenType.Id).Value;
            uint aId = Ids.GetId(a);

            string rel = tok.Consume(TokenType.Id).Value;
            uint relId = Ids.GetId(rel);

            string b = tok.Consume(TokenType.Id).Value;
            uint bId = Ids.GetId(b);
            tok.Consume(TokenType.RParen);

            if (!truth) tok.Consume(TokenType.RParen);

            return new Fact(aId, relId, bId, truth);
        }

        private ActionDef ParseActionDef(
            Tokenizer tok, 
            Dictionary<string, ActionDef> parsedActions)
        {
            var posPre = new HashSet<VariableRelation>();
            var negPre = new HashSet<VariableRelation>();
            var posPost = new HashSet<VariableRelation>();
            var negPost = new HashSet<VariableRelation>();

            string name = tok.Consume(TokenType.Id).Value;

            // Signature
            var parameters = ParseSignature(tok);
            var paramVars = parameters
                .ToDictionary(k => k.Name);

            // Optional Dependent Actions
            while (tok.PeekType() == TokenType.LParen)
            {
                tok.Consume(TokenType.LParen);

                // Dependent Action
                if (tok.PeekType() == TokenType.Id)
                {
                    var depAction = ParseDependentAction(tok, parsedActions, paramVars);
                    posPre.AddRange(depAction.PositivePreconditions);
                    negPre.AddRange(depAction.NegativePreconditions);
                    posPost.AddRange(depAction.PositivePostconditions);
                    negPost.AddRange(depAction.NegativePostconditions);
                }
                // Pre Expression
                else if (tok.PeekType() == TokenType.Pre)
                {
                    tok.Consume(TokenType.Pre);
                    if (tok.PeekToken().Type != TokenType.RParen)
                    {
                        var propDefs = ParseVariableRelations(paramVars, tok);
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
                        var propDefs = ParseVariableRelations(paramVars, tok);
                        foreach (var pd in propDefs)
                        {
                            var b = pd.Negated ? negPost.Add(pd) : posPost.Add(pd);
                        }
                    }
                }
                    
                tok.Consume(TokenType.RParen);
            }

            tok.Consume(TokenType.RParen);

            var ret = new ActionDef(
                name,
                paramVars.Values.ToArray(),
                posPre, 
                negPre, 
                posPost, 
                negPost
            );

            return ret;
        }

        private ActionDef ParseDependentAction(
            Tokenizer tok, 
            Dictionary<string, ActionDef> parsedActions,
            Dictionary<string, UnboundVariable> parentSignatureMap)
        {
            var depActionName = tok.Consume(TokenType.Id).Value;

            ActionDef dependentActionDef;
            if (!parsedActions.TryGetValue(depActionName, out dependentActionDef))
            {
                foreach (var lib in _libs.Values)
                {
                    if (lib.TryGetValue(depActionName, out dependentActionDef))
                    {
                        break;
                    }
                }
            }

            var depCallSignature = ParseSignature(tok);

            Variable[] newVars = depCallSignature
                .Select((dvar, idx) =>
                    parentSignatureMap.ContainsKey(dvar.Name) 
                        ? parentSignatureMap[dvar.Name] 
                        : new BoundVariable(Ids.GetId(dvar.Name)) as Variable)
                .ToArray();

            string boundVars = String.Join("_", newVars.Where(nv => nv.IsBound).Select(nv => nv.Name));
            string newActionName = $"{depActionName}_{boundVars}";
            var reboundAction = dependentActionDef.RebindVariables(newVars, newActionName);
            return reboundAction;
        }

        private UnboundVariable[] ParseSignature(Tokenizer tok)
        {
            var ret = new List<UnboundVariable>();
            tok.Consume(TokenType.LParen);
            int idx = 0;
            while (tok.PeekToken().Type != TokenType.RParen)
            {
                string name = tok.Consume(TokenType.Id).Value;
                var variable = new UnboundVariable(idx, name);
                ret.Add(variable);
                idx++;
            }
            tok.Consume(TokenType.RParen);
            return ret.ToArray();
        }

        private IList<VariableRelation> ParseVariableRelations(
            Dictionary<string, UnboundVariable> paramVars, 
            Tokenizer tok)
        {
            var predicates = new List<VariableRelation>();
            while (tok.PeekToken().Type != TokenType.RParen)
            {
                VariableRelation pred = ParseVariableRelation(paramVars, tok);
                predicates.Add(pred);
            }

            return predicates;
        }

        private VariableRelation ParseVariableRelation(
            Dictionary<string, UnboundVariable> paramVars,
            Tokenizer tok)
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

            return GetPropDef(paramVars, negated, nameStr, propStr, valStr);
        }

        private static VariableRelation GetPropDef(
            Dictionary<string, UnboundVariable> paramVars,
            bool negated,
            string nameStr,
            string propStr,
            string valStr)
        {
            Func<string, Variable> GetParam = (str) =>
            {
                if (paramVars.ContainsKey(str)) return paramVars[str];
                else return new BoundVariable(Ids.GetId(str));
            };

            Variable name = GetParam(nameStr);
            Variable property = GetParam(propStr);
            Variable value = GetParam(valStr);

            return new VariableRelation(name, property, value, negated);
        }
    }
}
