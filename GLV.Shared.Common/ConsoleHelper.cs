namespace GLV.Shared.Common;

public static class ConsoleHelper
{
    public static ConsoleColor SelectionColor { get; set; } = ConsoleColor.DarkYellow;
    public static ConsoleColor InfoColor { get; set; } = ConsoleColor.Cyan;
    public static ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

    public static int MultiSelect(params string[] options)
        => MultiSelect((IList<string>)options);

    public static int MultiSelect(IList<string> options)
    {
        var dfc = Console.ForegroundColor;
        var dbc = Console.BackgroundColor;

        try
        {
            Console.BackgroundColor = BackgroundColor;

            if (options.Count < 1)
                throw new ArgumentException("options cannot be empty", nameof(options));

            int selected = 0;
            int startY = Console.CursorTop + 1;
            int lastY = selected;

            Console.ForegroundColor = SelectionColor;
            Console.WriteLine();
            foreach (var option in options)
            {
                Console.Write("[ ] > ");
                Console.WriteLine(option);
            }
            Console.ForegroundColor = InfoColor;
            Console.WriteLine("\n* Use the arrow keys to select, then press enter");

            Update();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key is ConsoleKey.DownArrow)
                {
                    selected = (selected + 1) % options.Count;
                    Update();
                }
                else if (key.Key is ConsoleKey.UpArrow)
                {
                    if (--selected < 0)
                        selected = options.Count - 1;

                    Update();
                }
                else if (key.Key is ConsoleKey.Enter)
                {
                    Console.SetCursorPosition(0, startY + options.Count + 1);
                    Console.WriteLine($"\n Selected option {selected}: {options[selected]}");
                    return selected;
                }
            }

            void Update()
            {
                var prevColor = Console.ForegroundColor;
                Console.ForegroundColor = SelectionColor;

                Console.SetCursorPosition(1, lastY + startY);
                Console.Write(' ');

                Console.SetCursorPosition(1, selected + startY);
                Console.Write('*');

                Console.ForegroundColor = prevColor;
                lastY = selected;
            }
        }
        finally
        {
            Console.ForegroundColor = dfc;
            Console.BackgroundColor = dbc;
        }
    }
}
