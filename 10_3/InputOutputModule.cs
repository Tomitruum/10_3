using PascalCompiler.IOModule.Models;

namespace PascalCompiler.IOModule
{
    public class InputOutputModule
    {
        private readonly string _input;
        public List<string> Lines { get; }
        public char CurrentChar { get; private set; }
        public int Position { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }

        private readonly List<CompilerError> _errors = new();
        public IReadOnlyList<CompilerError> Errors => _errors;

        public InputOutputModule(string code)
        {
            _input = code.Replace("\r\n", "\n");
            Lines = new List<string>(_input.Split('\n'));
            CurrentChar = _input.Length > 0 ? _input[0] : '\0';
            Position = 0;
            Line = 0;
            Column = 0;
        }

        public void NextCh()
        {
            if (Position >= _input.Length)
            {
                CurrentChar = '\0';
                return;
            }

            char nextChar = _input[Position++];
            if (nextChar == '\n')
            {
                Line++;
                Column = 0;
            }
            else
            {
                Column++;
            }

            CurrentChar = nextChar;
        }

        public void AddError(int errorCode, string specificMessage = null)
        {
            string message = specificMessage ?? (ErrorTable.Errors.ContainsKey(errorCode)
                ? ErrorTable.Errors[errorCode]
                : "Неизвестная ошибка");
            _errors.Add(new CompilerError
            {
                Line = Line + 1,
                Column = Column,
                ErrorCode = errorCode,
                ErrorNumber = _errors.Count + 1,
                Message = message
            });
        }

        public void AddErrorAt(int line, int column, int errorCode, string specificMessage = null)
        {
            string message = specificMessage ?? (ErrorTable.Errors.ContainsKey(errorCode)
                ? ErrorTable.Errors[errorCode]
                : "Неизвестная ошибка");
            _errors.Add(new CompilerError
            {
                Line = line + 1,
                Column = column + 1,
                ErrorCode = errorCode,
                ErrorNumber = _errors.Count + 1,
                Message = message
            });
        }
    }
}
