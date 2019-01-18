using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STRIPS
{
    class Parser
	{
        private Tokenizer _tok;

		public Parser(Stream stream)
		{
            _tok = new Tokenizer(stream);            
		}

        public Parser(string file)
            : this(new FileStream(file, FileMode.Open))
        { }

        public SObject ParseObjects()
        {
            var ret = new SObject("world");
            while(_tok.PeekToken() != null)
            {
                ParseObject(ret);
            }
            return ret;
        }

        public Dictionary<string, Action> ParseActions()
        {
            var ret = new List<Action>();
            while(_tok.PeekToken() != null)
            {
                ret.Add(ParseAction()); 
            }
            return ret.ToDictionary(a => a.Name);
        }

        private void ParseObject(SObject parent)
        {
            // Object Name
            Consume(TokenType.LParen);
            var name = Consume(TokenType.Id).Value;
            var next = _tok.PeekToken().Type;

            // If Single inline property
            if (next == TokenType.Id)
            {
                var propertyName = Consume(TokenType.Id).Value;
                var propObj = new SObject(propertyName);

                // If parent already contains name
                if (parent.ContainsKey(name))
                {
                    parent[name][propertyName] = propObj;
                }
                else
                {
                    var ret = new SObject(name);
                    ret[propertyName] = propObj;
                    parent[name] = ret;
                }
            }
            // If List of Properties
            else if (next != TokenType.RParen)
            {
                // If parent already contains name
                if (parent.ContainsKey(name))
                {
                    while (_tok.PeekToken().Type != TokenType.RParen)
                    {
                        ParseObject(parent);
                    }
                }
                else
                {
                    var ret = new SObject(name);
                    while (_tok.PeekToken().Type != TokenType.RParen)
                    {
                        ParseObject(ret);
                    }
                    parent[name] = ret;
                }
            }
            else
            {
                parent[name] = new SObject(name);
            }

            Consume(TokenType.RParen);
        }

        private Action ParseAction()
        {
            // Action Name
            Consume(TokenType.LParen);
            string name = Consume(TokenType.Id).Value;

            // Signature
            List<string> parameters = ParseSignature();

            // Pre Expression
            Conjunction pre = null;
            Consume(TokenType.LParen);
            Consume(TokenType.Pre);
            if (_tok.PeekToken().Type != TokenType.RParen)
            {
                pre = ParseConjuction(parameters);
            }
            Consume(TokenType.RParen);

            // Post Expression
            Consume(TokenType.LParen);
            Consume(TokenType.Post);
            Conjunction post = null;
            if (_tok.PeekToken().Type != TokenType.RParen)
            {
                post = ParseConjuction(parameters);
            }
            Consume(TokenType.RParen);
            Consume(TokenType.RParen);

            return new Action(name, parameters, pre, post);
        }

        private List<string> ParseSignature()
        {
            var ret = new List<string>();
            Consume(TokenType.LParen);
            while(_tok.PeekToken().Type != TokenType.RParen)
            {
                string param = Consume(TokenType.Id).Value;
                ret.Add(param);
            }
            Consume(TokenType.RParen);
            return ret;
        }

        private Conjunction ParseConjuction(List<string> parameters)
        {

            var predicates = new List<Expression>();    
            while(_tok.PeekToken().Type != TokenType.RParen)
            {
                Expression pred = ParsePredicate(parameters);
                predicates.Add(pred);
            }

            Conjunction ret = new Conjunction(predicates.ToArray());
            return ret;
        }

        private Expression ParsePredicate(List<string> parameters)
        {
            List<KV> paramIdxs = new List<KV>();
            Consume(TokenType.LParen);

            if (_tok.PeekToken().Type == TokenType.Not)
            {
                Consume(TokenType.Not);
                var expr = ParsePredicate(parameters) as Predicate;
                Consume(TokenType.RParen);
                return new NotExpression(expr);
            }
            else
            {
                while (_tok.PeekToken().Type != TokenType.RParen)
                {
                    var name = Consume(TokenType.Id).Value;
                    int idx = parameters.IndexOf(name);
                    if (idx == -1 && paramIdxs.Count == 0)
                    {
                        throw new Exception("Undefined variable reference: " + name);
                    }

                    var param = new KV() { Name = name, ParamIdx = idx };
                    paramIdxs.Add(param);
                }

                Consume(TokenType.RParen);
                return new Predicate(paramIdxs);
            }
        }

        private Token Consume(TokenType t)
        {
            var tok = _tok.ReadToken();
            if (tok.Type != t) Error(t, tok.Type);
            return tok;
        }

        private void Error(TokenType expected, TokenType actual)
        {
            throw new Exception(String.Format("Unexpected token found. Expected '{0}' Actual '{1}'", expected, actual));
        }
	}

    class Tokenizer
    {
        private StreamReader _stream;
        private Token _peekToken;
        private int _peek;
        private char[] _singleCharStrings = new[]
        {
            '(', ')', '{', '}' 
        };

        public Tokenizer(Stream stream)
        {
            _stream = new StreamReader(stream);
            Read();
            ReadToken();
        }

        public Token ReadToken()
        {
            var ret = _peekToken;

            var str = ReadString();
            if (String.IsNullOrWhiteSpace(str))
            {
                _peekToken = null;
            }
            else
            {
                _peekToken = new Token(str);
            }

            return ret;
        }

        public Token PeekToken()
        {
            return _peekToken;
        }

        private string ReadString()
        {
            EatWhiteSpace();
            StringBuilder sb = new StringBuilder();

            char next = (char)Peek();
            if (_singleCharStrings.Contains(next))
			{
				return ((char)Read()).ToString();
			}

			while(!_stream.EndOfStream)
			{
				next = (char)Peek();
				if (_singleCharStrings.Contains(next) || Char.IsWhiteSpace(next))
				{
					return sb.ToString();
				}

				sb.Append((char)Read());
			}

			var ret = sb.ToString();
			return sb.ToString().ToLower();
		}

		private int Read()
		{
			int ret = _peek;
			_peek = _stream.Read();
			return ret;
		}

		private int Peek()
		{
			return _peek;
		}

		private void EatWhiteSpace()
		{
			while (!_stream.EndOfStream && Char.IsWhiteSpace((char)Peek()))
			{
				Read();
			}
		}

	}

	enum TokenType
	{
		LParen, RParen, Id, LBracket, RBracket, Pre, Post, Not
	}

	class Token
	{
		public TokenType Type { get; private set; }
		public string Value { get; private set; }

		public Token(string value)
		{
			Value = value.ToLowerInvariant();
            if (Value == "(") Type = TokenType.LParen;
            else if (Value == ")") Type = TokenType.RParen;
            else if (Value == "not") Type = TokenType.Not;
            else if (Value == "pre") Type = TokenType.Pre;
            else if (Value == "post") Type = TokenType.Post;
            else Type = TokenType.Id;
		}
	}
}
