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
            var predicates = new Dictionary<UInt64, Predicate>();

            using (var sr = new FileStream(path, FileMode.Open))
            {
                var tok = new Tokenizer(sr);

                while(tok.PeekToken() != null)
                {
                    bool g;
                    var p = ParsePredicate(tok, out g);
                    predicates[p.Id] = p;
                }
            }

            return new State(predicates);
        }

        public Dictionary<string, ActionDef> ParseActions(string path)
        {
            var ret = new Dictionary<string, ActionDef>();
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

        private Predicate ParsePredicate(Tokenizer tok)
        {
            bool b = false;
            return ParsePredicate(tok, out b);
        }

        private Predicate ParsePredicate(Tokenizer tok, out bool negated)
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
            if (tok.PeekType() == TokenType.RParen)
            {
                return new Predicate(nameId, null, null);
            }

            string prop = tok.Consume(TokenType.Id).Value;
            uint propId = Ids.GetId(prop);
            if (tok.PeekType() == TokenType.RParen)
            {
                return new Predicate(nameId, propId, null);
            }

            string val = tok.Consume(TokenType.Id).Value;
            uint valId = Ids.GetId(val);
            tok.Consume(TokenType.RParen);

            if (negated) tok.Consume(TokenType.RParen);

            return new Predicate(nameId, propId, valId);
        }

        private ActionDef ParseAction(Tokenizer tok)
        {
            // Action Name
            tok.Consume(TokenType.LParen);
            string name = tok.Consume(TokenType.Id).Value;

            // Signature
            List<uint> parameters = ParseSignature(tok);

            // Pre Expression
            List<PredicateDef> prePreds = new List<PredicateDef>();
            tok.Consume(TokenType.LParen);
            tok.Consume(TokenType.Pre);
            if (tok.PeekToken().Type != TokenType.RParen)
            {
                prePreds = ParseConjuction(parameters, tok);
            }
            tok.Consume(TokenType.RParen);

            // Post Expression
            List<PredicateDef> postPreds = new List<PredicateDef>();
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

            return new ActionDef(
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

        private List<PredicateDef> ParseConjuction(List<uint> parameters, Tokenizer tok)
        {
            var predicates = new List<PredicateDef>();
            while (tok.PeekToken().Type != TokenType.RParen)
            {
                PredicateDef pred = ParsePredicateDef(parameters, tok);
                predicates.Add(pred);
            }

            return predicates;
        }

        private PredicateDef ParsePredicateDef(List<uint> parameters, Tokenizer tok)
        {
            bool negated = false;
            var pred = ParsePredicate(tok, out negated);

            CtParameter name = new CtParameter();
            var nameIdx = parameters.IndexOf(pred.NameId);
            if (nameIdx >= 0) name.Idx = nameIdx;
            else name.Id = pred.NameId;

            if (pred.ValueId != null)
            {
                CtParameter prop = new CtParameter();
                var propIdx = parameters.IndexOf(pred.PropertyId.Value);
                if (propIdx >= 0) prop.Idx = propIdx;
                else prop.Id = pred.PropertyId;

                CtParameter value = new CtParameter();
                var valueIdx = parameters.IndexOf(pred.ValueId.Value);
                if (valueIdx >= 0) value.Idx = valueIdx;
                else value.Id = pred.ValueId.Value;

                return new PredicateDef(name, prop, value, negated);
            }
            else if (pred.PropertyId != null)
            {
                CtParameter prop = new CtParameter();
                var propIdx = parameters.IndexOf(pred.PropertyId.Value);
                if (propIdx >= 0) prop.Idx = propIdx;
                else prop.Id = pred.PropertyId;
                return new PredicateDef(name, prop, null, negated);
            }
            else
            {
                return new PredicateDef(name, null, null, negated);
            }
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
