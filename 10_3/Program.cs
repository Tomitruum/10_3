using System.Text;
using PascalCompiler;
using PascalCompiler.IOModule;

class Program
{
    static void Main()
    {
        string filePath = "input1.pas";

        if (!File.Exists(filePath))
        {
            Console.WriteLine("Файл input.pas не найден.");
            return;
        }

        string code = File.ReadAllText(filePath, Encoding.UTF8);
        var io = new InputOutputModule(code);
        var analyzer = new LexicalAnalyzer(io);
        analyzer.AnalyzeTokens();

        string outputPath = "output.txt";
        File.WriteAllText(outputPath, string.Join(" ", analyzer.TokenCodes));
        Console.WriteLine($"Коды символов записаны в текстовый файл: {outputPath}");
        Console.WriteLine();

        var syntax = new SyntaxAnalyzer(analyzer.TokenCodes, analyzer.TokenPositions, io);
        syntax.Analyze();

        // Сортировка ошибок по строке и столбцу
        var sortedErrors = io.Errors.OrderBy(e => e.Line).ThenBy(e => e.Column).ToList();
        for (int i = 0; i < sortedErrors.Count; i++)
        {
            sortedErrors[i].ErrorNumber = i + 1;
        }

        int errorCount = sortedErrors.Count;

        for (int i = 0; i < io.Lines.Count; i++)
        {
            string line = io.Lines[i];
            Console.WriteLine("        " + line);

            foreach (var error in sortedErrors)
            {
                if (error.Line == i + 1)
                {
                    string num = error.ErrorNumber.ToString("D2");
                    string pointer = new string(' ', Math.Max(0, error.Column - 1)) + $"^ ошибка код {error.ErrorCode}";
                    Console.WriteLine($"**{num}** {pointer}");
                    Console.WriteLine("****** " + error.Message);
                }
            }
        }

        Console.WriteLine($"\nКомпиляция окончена: ошибок - {errorCount} !");
    }
}