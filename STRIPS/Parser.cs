using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STRIPS
{
    /*
        (Infer
            (Pre (a.Adjacent b))
            (Post (b.Adjacent a))
        )

        (Move   
            (Pre 
                (and 
                    (a.Type Player) 
                    (b.Type Location)
                    (c.Type Location)
                    (a.Location b)
                    (a.adjacent b)
                )
            )
            (Post 
                (a.Location c)
            )
        )
     */
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
            var ret = new List<SObject>();
            while(_tok.PeekToken() != null)
            {
                ret.Add(ParseObject()); 
            }
            return new SObject("world", ret);
        }

        public SAction[] ParseActions()
        {
            var ret = new List<SAction>();
            while(_tok.PeekToken() != null)
            {
                ret.Add(ParseAction()); 
            }
            return ret.ToArray();
        }

        private SObject ParseObject()
        {
            // Object Name
            Consume(TokenType.LParen);
            var name = Consume(TokenType.Id).Value;

            // List of Object Properties
            var properties = new List<SObject>();
            while (_tok.PeekToken().Type != TokenType.RParen)
            {
                // If Nested Prop
                if (_tok.PeekToken().Type == TokenType.LParen)
                {
                    var property = ParseObject();
                    properties.Add(property);
                }
                // If Inline Prop
                else
                {
                    var pVal = Consume(TokenType.Id).Value;
                    var prop = new SObject(pVal);
                    properties.Add(prop);
                    break;
                }
            }

            Consume(TokenType.RParen);

            return new SObject(name, properties);
        }

        private SAction ParseAction()
        {
            // Action Name
            Consume(TokenType.LParen);
            string name = Consume(TokenType.Id).Value;

            // Pre Expression
            Consume(TokenType.LParen);
            Consume(TokenType.Pre);
            Expression pre = ParseExpression();
            Consume(TokenType.RParen);

            // Post Expression
            Consume(TokenType.LParen);
            Consume(TokenType.Post);
            Expression post = ParseExpression();
            Consume(TokenType.RParen);

            Consume(TokenType.RParen);

            return new SAction(name, pre, post);
        }

        private Expression ParseExpression()
        {
            Expression ret = null;
            Consume(TokenType.LParen);
            var next = _tok.PeekToken().Type;
            
            if (next == TokenType.And)
            {
                Consume(TokenType.And);

                List<Expression> andExpressions = new List<Expression>();
                while(_tok.PeekToken().Type != TokenType.RParen)
                {
                    Expression expr = ParseExpression();
                    andExpressions.Add(expr);
                }

                ret = new AndExpression(andExpressions.ToArray());
            }
            else if (next == TokenType.Or)
            {
                Consume(TokenType.Or);
                List<Expression> orExpressions = new List<Expression>();
                while(_tok.PeekToken().Type != TokenType.RParen)
                {
                    Expression expr = ParseExpression();
                    orExpressions.Add(expr);
                }

                ret = new OrExpression(orExpressions.ToArray());
            }
            else if (next == TokenType.Not)
            {
                Consume(TokenType.Not);
                var expr = ParseExpression();
                ret = new NotExpression(expr);
            }
            else
            {
                ret = ParsePredicate();
            }

            Consume(TokenType.RParen);

            return ret;
        }

        private Expression ParsePredicate()
        {
            Expression ret = null;

            //Consume(TokenType.LParen);
            var objName = Consume(TokenType.Id).Value;
            Consume(TokenType.Period);
            var propertyName = Consume(TokenType.Id).Value;

            // If Boolean Predicate
            var next = _tok.PeekToken().Type;
            if (next == TokenType.RParen)
            {
                ret = new BooleanPredicateExpression(objName, propertyName);
            }
            else
            {
                var propertyValue = Consume(TokenType.Id).Value;
                ret = new KeyValuePredicateExpression(objName, propertyName, propertyValue);
            }

            return ret;
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
            '(', ')', '{', '}', '.', '=', ','
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
			return sb.ToString();
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
		LParen, RParen, Id, Comma, LBracket, RBracket, Pre, Post, Period, And, Or, Not
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
            else if (Value == ".") Type = TokenType.Period;
            else if (Value == "and") Type = TokenType.And;
            else if (Value == "or") Type = TokenType.Or;
            else if (Value == "not") Type = TokenType.Not;
            else if (Value == "pre") Type = TokenType.Pre;
            else if (Value == "post") Type = TokenType.Post;
            else Type = TokenType.Id;
		}
	}
}
