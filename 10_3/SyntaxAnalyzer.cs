using PascalCompiler.IOModule;

namespace PascalCompiler
{
    public class SyntaxAnalyzer
    {
        private readonly List<byte> tokenCodes;
        private readonly List<(int Line, int Column)> tokenPositions;
        private readonly InputOutputModule _io;
        private int position = 0;

        public SyntaxAnalyzer(List<byte> tokenCodes, List<(int Line, int Column)> tokenPositions, InputOutputModule io)
        {
            this.tokenCodes = tokenCodes;
            this.tokenPositions = tokenPositions;
            _io = io;
        }

        private byte Current => position < tokenCodes.Count ? tokenCodes[position] : (byte)0;
        private (int Line, int Column) CurrentPosition => position < tokenPositions.Count ? tokenPositions[position] : (0, 0);
        private (int Line, int Column) LastPosition => position > 0 ? tokenPositions[position - 1] : (0, 0);

        private void Next() => position++;

        public void Analyze()
        {
            if (Current == 122) // program
            {
                Next();
                if (Current == 2) // имя программы
                {
                    Next();
                    Expect(14); // ;
                }
                else
                {
                    ReportSyntaxError(401);
                    SkipToRecoveryPoint();
                }
            }

            while (Current == 105 || Current == 116) // var или const
            {
                if (Current == 105)
                    ParseVarSection();
                else
                    ParseConstSection();
            }

            if (Current == 113) // begin
                ParseCompoundStatement();
            else
            {
                var (line, column) = LastPosition;
                _io.AddErrorAt(line, column, 402);
            }

            if (Current != 61) // '.'
            {
                var (line, column) = LastPosition;
                _io.AddErrorAt(line, column, 403);
            }
        }

        private void ParseVarSection()
        {
            Next();
            while (Current == 2) // идентификатор
            {
                var startPos = CurrentPosition;
                var identifiers = new List<(int Line, int Column)>();
                identifiers.Add(startPos);

                do
                {
                    Next();
                    if (Current == 20) // запятая
                    {
                        Next();
                        if (Current == 2)
                        {
                            identifiers.Add(CurrentPosition);
                            Next();
                        }
                        else
                        {
                            ReportSyntaxError($"ожидался идентификатор после ','");
                            SkipToRecoveryPoint();
                            return;
                        }
                    }
                } while (Current == 20);

                if (Current != 5) // ':'
                {
                    var (line, column) = identifiers.Last();
                    _io.AddErrorAt(line, column, 404, "ожидался ':'");
                    SkipToRecoveryPoint();
                    return;
                }
                Expect(5); // :

                if (Current == 115) // array
                    ParseArrayType();
                else if (Current == 120) // record
                    ParseRecordType();
                else if (IsStandardType(Current))
                    Next();
                else
                {
                    ReportSyntaxError(405);
                    SkipToRecoveryPoint();
                    return;
                }

                if (Current != 14) // ';'
                {
                    var (line, column) = LastPosition;
                    _io.AddErrorAt(line, column, 404, "ожидался ';'");
                    SkipToRecoveryPoint();
                    return;
                }
                Expect(14); // ;
            }
        }

        private void ParseConstSection()
        {
            Next();
            while (Current == 2) // идентификатор
            {
                var startPos = CurrentPosition;
                Next();
                if (Current != 16) // '='
                {
                    var (line, column) = startPos;
                    _io.AddErrorAt(line, column, 404, "ожидался '='");
                    SkipToRecoveryPoint();
                    return;
                }
                Expect(16); // =
                if (Current != 15) // число
                {
                    ReportSyntaxError(409);
                    SkipToRecoveryPoint();
                    return;
                }
                Next();
                if (Current != 14) // ';'
                {
                    var (line, column) = LastPosition;
                    _io.AddErrorAt(line, column, 404, "ожидался ';'");
                    SkipToRecoveryPoint();
                    return;
                }
                Expect(14); // ;
            }
        }

        private void ParseArrayType()
        {
            Next();
            Expect(11); // [
            Expect(15); // число
            Expect(21); // ..
            Expect(15); // число
            Expect(12); // ]
            Expect(101); // of
            if (IsStandardType(Current))
                Next();
            else
            {
                ReportSyntaxError(406);
                SkipToRecoveryPoint();
            }
        }

        private void ParseRecordType()
        {
            Next();
            while (Current == 2) // имя поля
            {
                var startPos = CurrentPosition;
                do
                {
                    Next();
                } while (Current == 20 && NextTokenIs(2));

                Expect(5); // :
                if (IsStandardType(Current))
                    Next();
                else
                {
                    ReportSyntaxError(405);
                    SkipToRecoveryPoint();
                    return;
                }
                Expect(14); // ;
            }
            if (Current != 104) // end
            {
                ReportSyntaxError(407);
                SkipToRecoveryPoint();
            }
            else
                Next();
        }

        private void ParseCompoundStatement()
        {
            Next();
            while (Current != 104 && Current != 0) // до end или конца токенов
            {
                ParseStatement();
                if (Current == 14) // ;
                    Next();
                else if (Current != 104 && Current != 0)
                {
                    var (line, column) = LastPosition;
                    _io.AddErrorAt(line, column, 404, "ожидался ';' или 'end'");
                    SkipToRecoveryPoint();
                }
            }
            if (Current == 104) // end
                Next();
            else
            {
                var (line, column) = LastPosition;
                _io.AddErrorAt(line, column, 408);
            }
        }

        private void ParseStatement()
        {
            if (Current == 2 || Current == 125) // идентификатор или writeln
            {
                var startPos = CurrentPosition;
                Next();
                if (Current == 11) // '[' — индекс
                {
                    Next();
                    Expect(15); // число
                    Expect(12); // ]
                }
                if (Current == 51) // :=
                {
                    Next();
                    ParseExpression();
                }
                else if (Current == 5 && NextTokenIs(2) && NextTokenIs(51, 2)) // запись: rec.a := ...
                {
                    Next(); // :
                    Next(); // поле
                    if (Current == 51) // :=
                    {
                        Next();
                        ParseExpression();
                    }
                }
                else if (Current == 9 && position > 0 && tokenCodes[position - 1] == 125) // writeln(
                {
                    Next(); // (
                    while (Current != 10 && Current != 0 && Current != 14 && Current != 104) // до ), ; или end
                    {
                        if (Current == 16 || Current == 2 || Current == 15) // строка, идентификатор или число
                            Next();
                        else
                            break;
                    }
                    if (Current == 10) // )
                        Next();
                    // Не добавляем ошибку 408 здесь, так как незакрытая строка уже обработана
                }
            }
        }

        private void ParseExpression()
        {
            if (Current == 2 || Current == 15)
            {
                Next();
                while (Current == 17 || Current == 18 || Current == 19 || Current == 50) // +, -, *, /
                {
                    Next();
                    if (Current == 2 || Current == 15)
                        Next();
                    else
                    {
                        ReportSyntaxError(409);
                        SkipToRecoveryPoint();
                        return;
                    }
                }
            }
            else
            {
                ReportSyntaxError(409);
                SkipToRecoveryPoint();
            }
        }

        private bool NextTokenIs(byte token, int offset = 1)
        {
            return position + offset < tokenCodes.Count && tokenCodes[position + offset] == token;
        }

        private bool IsStandardType(byte token) =>
            token == 2 || token == 100 || token == 114 || token == 109 || token == 111;

        private void Expect(byte expected)
        {
            if (Current == expected)
                Next();
            else
            {
                var (line, column) = LastPosition;
                string expectedToken = GetTokenName(expected);
                _io.AddErrorAt(line, column, 404, $"ожидался '{expectedToken}'");
                SkipToRecoveryPoint();
            }
        }

        private string GetTokenName(byte token)
        {
            return token switch
            {
                14 => ";",
                5 => ":",
                16 => "=",
                11 => "[",
                12 => "]",
                21 => "..",
                101 => "of",
                104 => "end",
                10 => ")",
                _ => "неизвестный токен"
            };
        }

        private void ReportSyntaxError(int errorCode)
        {
            var (line, column) = LastPosition;
            _io.AddErrorAt(line, column, errorCode);
            SkipToRecoveryPoint();
        }

        private void ReportSyntaxError(string message)
        {
            var (line, column) = LastPosition;
            _io.AddErrorAt(line, column, 404, message);
            SkipToRecoveryPoint();
        }

        private void SkipToRecoveryPoint()
        {
            while (position < tokenCodes.Count && Current != 14 && Current != 104 && Current != 61)
                Next();
            if (Current == 14)
                Next();
        }
    }
}