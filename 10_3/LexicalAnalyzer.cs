using System.Text;
using PascalCompiler.IOModule;
using PascalCompiler.IOModule.Models;

namespace PascalCompiler
{
    public class LexicalAnalyzer
    {
        private readonly InputOutputModule _io;
        public List<byte> TokenCodes { get; } = new();
        public List<(int Line, int Column)> TokenPositions { get; } = new();
        public List<CompilerError> Errors => (List<CompilerError>)_io.Errors;

        private static readonly Dictionary<string, byte> Keywords = new()
        {
            {"and", 107}, {"array", 115}, {"begin", 113}, {"case", 31}, {"const", 116},
            {"div", 106}, {"do", 54}, {"downto", 118}, {"else", 32}, {"end", 104},
            {"file", 57}, {"for", 109}, {"function", 123}, {"goto", 33}, {"if", 56},
            {"in", 100}, {"label", 117}, {"mod", 110}, {"nil", 111}, {"not", 108},
            {"of", 101}, {"or", 102}, {"packed", 119}, {"procedure", 124}, {"program", 122},
            {"record", 120}, {"repeat", 121}, {"set", 112}, {"then", 52}, {"to", 103},
            {"type", 34}, {"until", 53}, {"var", 105}, {"while", 114}, {"with", 37},
            {"integer", 100}, {"writeln", 125}
        };

        private static readonly Dictionary<string, byte> SpecialSymbols = new()
        {
            {"+", 17}, {"-", 18}, {"*", 19}, {"/", 50},
            {"=", 16}, {"<>", 38}, {"<", 39}, {"<=", 40},
            {">", 41}, {">=", 42}, {":=", 51}, {":", 5},
            {".", 61}, {"..", 21}, {";", 14}, {",", 20},
            {"(", 9}, {")", 10}, {"[", 11}, {"]", 12}
        };

        public LexicalAnalyzer(InputOutputModule io)
        {
            _io = io;
        }

        public void AnalyzeTokens()
        {
            _io.NextCh();
            while (_io.CurrentChar != '\0')
            {
                if (char.IsWhiteSpace(_io.CurrentChar))
                {
                    _io.NextCh();
                    continue;
                }

                if (_io.CurrentChar == '{')
                {
                    SkipComment('}');
                    continue;
                }
                if (_io.CurrentChar == '(')
                {
                    _io.NextCh();
                    if (_io.CurrentChar == '*')
                    {
                        SkipComment('*', ')');
                        continue;
                    }
                    else
                    {
                        AddToken(9, _io.Line, _io.Column - 1);
                        continue;
                    }
                }

                if (char.IsDigit(_io.CurrentChar))
                {
                    ParseNumber();
                    continue;
                }

                if (char.IsLetter(_io.CurrentChar))
                {
                    ParseIdentifier();
                    continue;
                }

                if (_io.CurrentChar == '\'')
                {
                    ParseString();
                    continue;
                }

                ParseSpecialSymbol();
            }
        }

        private void AddToken(byte code, int line, int column)
        {
            TokenCodes.Add(code);
            TokenPositions.Add((line, column));
        }

        private void ParseSpecialSymbol()
        {
            int startLine = _io.Line;
            int startColumn = _io.Column;
            char ch1 = _io.CurrentChar;
            _io.NextCh();
            char ch2 = _io.CurrentChar;

            string twoChar = $"{ch1}{ch2}";

            if (SpecialSymbols.TryGetValue(twoChar, out byte code))
            {
                _io.NextCh();
                AddToken(code, startLine, startColumn);
            }
            else if (SpecialSymbols.TryGetValue(ch1.ToString(), out code))
            {
                AddToken(code, startLine, startColumn);
            }
            else
            {
                _io.AddErrorAt(startLine, startColumn, 302, $"недопустимый символ '{ch1}'");
            }
        }

        private void SkipComment(char endChar1, char? endChar2 = null)
        {
            int startLine = _io.Line;
            int startColumn = _io.Column - 1;

            _io.NextCh();

            while (_io.CurrentChar != '\0')
            {
                if (_io.CurrentChar == endChar1)
                {
                    if (endChar2 == null)
                    {
                        _io.NextCh();
                        return;
                    }

                    _io.NextCh();
                    if (_io.CurrentChar == endChar2)
                    {
                        _io.NextCh();
                        return;
                    }
                }
                _io.NextCh();
            }

            _io.AddErrorAt(startLine, startColumn, 304);
        }

        private void ParseNumber()
        {
            int startLine = _io.Line;
            int startColumn = _io.Column;
            StringBuilder number = new();

            while (char.IsDigit(_io.CurrentChar))
            {
                number.Append(_io.CurrentChar);
                _io.NextCh();
            }

            if (Int32.TryParse(number.ToString(), out int val))
            {
                if (val > Int16.MaxValue)
                {
                    _io.AddErrorAt(startLine, startColumn, 203);
                }
                AddToken(15, startLine, startColumn);
            }
            else
            {
                _io.AddErrorAt(startLine, startColumn, 203);
            }
        }

        private void ParseIdentifier()
        {
            int startLine = _io.Line;
            int startColumn = _io.Column;
            StringBuilder ident = new();
            while (char.IsLetterOrDigit(_io.CurrentChar))
            {
                ident.Append(_io.CurrentChar);
                _io.NextCh();
            }

            string name = ident.ToString().ToLower();
            if (Keywords.TryGetValue(name, out byte code))
                AddToken(code, startLine, startColumn);
            else
                AddToken(2, startLine, startColumn);
        }

        private void ParseString()
        {
            int startLine = _io.Line;
            int startColumn = _io.Column;
            _io.NextCh();
            bool closed = false;

            while (_io.CurrentChar != '\0')
            {
                if (_io.CurrentChar == '\'')
                {
                    _io.NextCh();
                    closed = true;
                    break;
                }

                if (_io.CurrentChar == '\n')
                {
                    break;
                }

                _io.NextCh();
            }

            if (!closed)
            {
                _io.AddErrorAt(startLine, startColumn, 301);
                AddToken(16, startLine, startColumn); // Токен для незакрытой строки
            }
            else
            {
                AddToken(16, startLine, startColumn);
            }
        }
    }
}
