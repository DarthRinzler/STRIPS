using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlannerGraph
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
            var ret = new Dictionary<string, ActionDef>();
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
                        var actionDef = ParseActionDef(tok, ret);
                        ret.Add(actionDef.Name, actionDef);
                    }
                }
            }

            return ret;
        }

        private void ParseImport(Tokenizer tok)
        {
            tok.Consume(TokenType.Import);
            string libName = tok.Consume(TokenType.Id).Value;

            if (!_libs.ContainsKey(libName))
            {
                var lib = ParseActionFile(libName+".txt");
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

            var ret = new Fact(
                Ids.GetId(tok.Consume(TokenType.Id).Value),
                Ids.GetId(tok.Consume(TokenType.Id).Value),
                Ids.GetId(tok.Consume(TokenType.Id).Value)
            );

            tok.Consume(TokenType.RParen);

            if (!truth) tok.Consume(TokenType.RParen);
            return ret;
        }

        private ActionDef ParseActionDef(Tokenizer tok, Dictionary<string, ActionDef> parsedActions)
        {
            var pre = new List<VariableRelation>();
            var post = new List<VariableRelation>();

            // Action Name
            string name = tok.Consume(TokenType.Id).Value;

            // Signature
            var paramVars = ParseSignature(tok);

            // Optional Dependent Actions
            while (tok.PeekType() == TokenType.LParen)
            {
                tok.Consume(TokenType.LParen);

                // Dependent Action
                if (tok.PeekType() == TokenType.Id)
                {
                    var reboundActionDef = ParseDependentAction(tok, parsedActions, paramVars);
                    pre.AddRange(reboundActionDef.Pre);
                    post.AddRange(reboundActionDef.Post);
                }
                // Pre Expression
                else if (tok.PeekType() == TokenType.Pre)
                {
                    tok.Consume(TokenType.Pre);
                    if (tok.PeekToken().Type != TokenType.RParen)
                    {
                        var preRelations = ParseNodeRelations(paramVars, tok);
                        pre.AddRange(preRelations);
                    }
                }
                // Post Expression
                else if (tok.PeekType() == TokenType.Post)
                {
                    tok.Consume(TokenType.Post);
                    if (tok.PeekToken().Type != TokenType.RParen)
                    {
                        var postRelations = ParseNodeRelations(paramVars, tok);
                        post.AddRange(postRelations);
                    }
                }

                tok.Consume(TokenType.RParen);
            }

            tok.Consume(TokenType.RParen);

            var ret = new ActionDef(
                name,
                paramVars,
                pre.ToArray(),
                post.ToArray()
            );

            return ret;
        }

        private ActionDef ParseDependentAction(
            Tokenizer tok,
            Dictionary<string, ActionDef> parsedActions,
            Variable[] parentSignature)
        {
            var depActionName = tok.Consume(TokenType.Id).Value;

            ActionDef depActionDef;
            if (!parsedActions.TryGetValue(depActionName, out depActionDef))
            {
                foreach (var lib in _libs.Values)
                {
                    if (lib.TryGetValue(depActionName, out depActionDef))
                    {
                        break;
                    }
                }
            }

            if (depActionDef == null) throw new Exception($"Parse unable to find Action: {depActionName}");

            Variable[] depSignature = ParseSignature(tok);
            var parentSignatureMap = parentSignature.ToDictionary(v => v.Name);

            Variable[] newVars = depSignature
                .Select(dvar => 
                    parentSignatureMap.ContainsKey(dvar.Name) ? 
                    parentSignatureMap[dvar.Name] : 
                    new Variable(dvar.Name))
                .ToArray();

            var reboundActionDef = depActionDef.RebindVariables(newVars);
            return reboundActionDef;
        }

        private Variable[] ParseSignature(Tokenizer tok)
        {
            var ret = new List<Variable>();
            tok.Consume(TokenType.LParen);
            while (tok.PeekToken().Type != TokenType.RParen)
            {
                string name = tok.Consume(TokenType.Id).Value;
                Variable nv = new Variable(name);
                ret.Add(nv);
            }
            tok.Consume(TokenType.RParen);
            return ret.ToArray();
        }

        private IEnumerable<VariableRelation> ParseNodeRelations(Variable[] paramVars, Tokenizer tok)
        {
            var nodeRelations = new List<VariableRelation>();
            while (tok.PeekToken().Type != TokenType.RParen)
            {
                VariableRelation relation = ParseNodeRelation(paramVars, tok);
                nodeRelations.Add(relation);
            }

            return nodeRelations;
        }

        private VariableRelation ParseNodeRelation(Variable[] paramVars, Tokenizer tok)
        {
            bool negated = false;
            tok.Consume(TokenType.LParen);

            if (tok.PeekType() == TokenType.Not)
            {
                tok.Consume(TokenType.Not);
                tok.Consume(TokenType.LParen);
                negated = true;
            }

            string aStr = tok.Consume(TokenType.Id).Value;
            string relStr = tok.Consume(TokenType.Id).Value;
            string bStr = tok.Consume(TokenType.Id).Value;
            tok.Consume(TokenType.RParen);

            if (negated) tok.Consume(TokenType.RParen);

            var paramVarsMap = paramVars.ToDictionary(pv => pv.Name);
            Func<string, Variable> GetNodeVar = (name) =>
            {
                if (paramVarsMap.ContainsKey(name))
                {
                    return paramVarsMap[name];
                }
                else
                {
                    return new Variable(name);
                }
            };

            return new VariableRelation(GetNodeVar(aStr), GetNodeVar(relStr), GetNodeVar(bStr));
        }
    }
}
