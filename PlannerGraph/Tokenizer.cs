using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlannerGraph
{
    public class Tokenizer
    {

        private StreamReader _stream;
        private Token _peekToken;
        private int _lineNum;
        private int _peek;
        private string _fileName;
        private char[] _singleCharStrings = new[]
        {
            '(', ')', '{', '}'
        };

        public Tokenizer(Stream stream, string fileName)
        {
            _stream = new StreamReader(stream);
            _fileName = fileName;
            _lineNum = 1;
            Read();
            ReadToken();
        }

        public Token Consume(TokenType t)
        {
            var tok = ReadToken();
            Debug.WriteLine(tok.Value);
            if (tok.Type != t) Error(t, tok.Type);
            return tok;
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

        public TokenType PeekType()
        {
            return _peekToken.Type;
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

            while (!_stream.EndOfStream)
            {
                next = (char)Peek();
                if (_singleCharStrings.Contains(next) || Char.IsWhiteSpace(next))
                {
                    return sb.ToString();
                }

                sb.Append((char)Read());
            }

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
                if (Peek() == '\n') _lineNum++;
                Read();
            }
        }

        private void Error(TokenType expected, TokenType actual)
        {
            throw new Exception($"{_fileName} Line {_lineNum}: Unexpected token found. Expected '{expected}' Actual '{actual}'");
        }
    }

    public enum TokenType
    {
        LParen, RParen, Id, LBracket, RBracket, Pre, Post, Not, Auto, Dep, Import
    }

    public class Token
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
            else if (Value == "auto") Type = TokenType.Auto;
            else if (Value == "dep") Type = TokenType.Dep;
            else if (Value == "import") Type = TokenType.Import;
            else Type = TokenType.Id;
        }
    }
}
