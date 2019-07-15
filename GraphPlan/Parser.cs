using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPlan
{
    public class Parser
    {
        public State ParseState(string path)
        {
            var predicates = new Dictionary<UInt64, Proposition>();

            using (var sr = new FileStream(path, FileMode.Open))
            {
                var tok = new Tokenizer(sr);

                while(tok.PeekToken() != null)
                {
                    bool g;
                    var p = ParseProposition(tok, out g);
                    predicates[p.Id] = p;
                }
            }

            return new State(predicates);
        }

        public Dictionary<string, ActionDefinition> ParseActions(string path)
        {
            var ret = new Dictionary<string, ActionDefinition>();
            using (var sr = new FileStream(path, FileMode.Open))
            {
                var tok = new Tokenizer(sr);
                while(tok.PeekToken() != null)
                {
                    var obj = ParseAction(tok);
                    ret.Add(obj.Name, obj);
                }
            }
            return ret;
        }

        private Proposition ParseProposition(Tokenizer tok)
        {
            bool b = false;
            return ParseProposition(tok, out b);
        }

        private Proposition ParseProposition(Tokenizer tok, out bool negated)
        {
            tok.Consume(TokenType.LParen);

            negated = false;
            if (tok.PeekType() == TokenType.Not)
            {
                tok.Consume(TokenType.Not);
                tok.Consume(TokenType.LParen);
                negated = true;
            }

            string name = tok.Consume(TokenType.Id).Value;
            uint nameId = Ids.GetId(name);

            string prop = tok.Consume(TokenType.Id).Value;
            uint propId = Ids.GetId(prop);

            string val = tok.Consume(TokenType.Id).Value;
            uint valId = Ids.GetId(val);
            tok.Consume(TokenType.RParen);

            if (negated) tok.Consume(TokenType.RParen);

            return new Proposition(nameId, propId, valId);
        }

        private ActionDefinition ParseAction(Tokenizer tok)
        {
            // Action Name
            tok.Consume(TokenType.LParen);
            string name = tok.Consume(TokenType.Id).Value;

            // Signature
            List<uint> parameters = ParseSignature(tok);

            // Pre Expression
            List<PropositionDefinition> prePreds = new List<PropositionDefinition>();
            tok.Consume(TokenType.LParen);
            tok.Consume(TokenType.Pre);
            if (tok.PeekToken().Type != TokenType.RParen)
            {
                prePreds = ParseConjuction(parameters, tok);
            }
            tok.Consume(TokenType.RParen);

            // Post Expression
            List<PropositionDefinition> postPreds = new List<PropositionDefinition>();
            tok.Consume(TokenType.LParen);
            tok.Consume(TokenType.Post);
            if (tok.PeekToken().Type != TokenType.RParen)
            {
                postPreds = ParseConjuction(parameters, tok);
            }
            tok.Consume(TokenType.RParen);
            tok.Consume(TokenType.RParen);

            var posPre = prePreds.Where(p => !p.Negated).ToHashSet();
            var negPre = prePreds.Where(p => p.Negated).ToHashSet();
            var posPost = postPreds.Where(p => !p.Negated).ToHashSet();
            var negPost = postPreds.Where(p => p.Negated).ToHashSet();

            return new ActionDefinition(
                name,
                parameters, 
                posPre, 
                negPre, 
                posPost, 
                negPost
            );
        }

        private List<uint> ParseSignature(Tokenizer tok)
        {
            var ret = new List<uint>();
            tok.Consume(TokenType.LParen);
            while (tok.PeekToken().Type != TokenType.RParen)
            {
                string param = tok.Consume(TokenType.Id).Value;
                var id = Ids.GetId(param);
                ret.Add(id);
            }
            tok.Consume(TokenType.RParen);
            return ret;
        }

        private List<PropositionDefinition> ParseConjuction(List<uint> parameters, Tokenizer tok)
        {
            var predicates = new List<PropositionDefinition>();
            while (tok.PeekToken().Type != TokenType.RParen)
            {
                PropositionDefinition pred = ParsePropositionDefinition(parameters, tok);
                predicates.Add(pred);
            }

            return predicates;
        }

        private PropositionDefinition ParsePropositionDefinition(List<uint> parameters, Tokenizer tok)
        {
            bool negated = false;
            var proposition = ParseProposition(tok, out negated);

            CtParameter name = new CtParameter();
            var nameIdx = parameters.IndexOf(proposition.NameId);
            if (nameIdx >= 0) name.Idx = nameIdx;
            else name.Id = proposition.NameId;

            CtParameter property = new CtParameter();
            var propIdx = parameters.IndexOf(proposition.PropertyId);
            if (propIdx >= 0) property.Idx = propIdx;
            else property.Id = proposition.PropertyId;

            CtParameter value = new CtParameter();
            var valueIdx = parameters.IndexOf(proposition.ValueId);
            if (valueIdx >= 0) value.Idx = valueIdx;
            else value.Id = proposition.ValueId;

            return new PropositionDefinition(proposition, name, property, value, negated);
        }

        private void Error(TokenType expected, TokenType actual)
        {
            throw new Exception(String.Format("Unexpected token found. Expected '{0}' Actual '{1}'", expected, actual));
        }
    }

    public static class Ext
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> THIS)
        {
            HashSet<T> ret = new HashSet<T>();
            foreach (T t in THIS)
            {
                ret.Add(t);
            }
            return ret;
        }
    }
}
