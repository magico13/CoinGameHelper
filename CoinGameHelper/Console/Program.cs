using CoinGameHelper;

static bool EnterSpaceData(Board board, string line)
{
    if (string.IsNullOrEmpty(line) || line.Length != 3)
    {
        return false;
    }
    var row = int.Parse(line[0..1]) - 1;
    var col = int.Parse(line[1..2]) - 1;
    var val = int.Parse(line[2..3]);

    board.SetValue(val, row, col);
    return true;
}

static string GetSymbolForValue(SpaceType val)
    => val switch
    {
        SpaceType.Bomb => "B",
        SpaceType.BombOrOne => "X",
        SpaceType.Safe => "S",
        _ => ((int)val).ToString(),
    };

static void PrintBoard(Board board)
{
    for (int row = 0; row < 5; row++)
    {
        var rowDiff = board.Rows[row].Points + board.Rows[row].Bombs - board.GetRowScore(row, bombVal: 1);
        for (int col = 0; col < 5; col++)
        {
            var symbol = GetSymbolForValue(board.GetValue(row, col));
            Console.Write($"{symbol} ");
        }
        Console.Write($" > {rowDiff}");
        Console.WriteLine();
    }
    Console.WriteLine();
    Console.WriteLine("v v v v v");
    for (int col = 0; col < 5; col++)
    {
        var colDiff = board.Cols[col].Points + board.Cols[col].Bombs - board.GetColumnScore(col, bombVal: 1);
        Console.Write($"{colDiff} ");
    }
    Console.WriteLine();
}

static void Setup(Simulation simulation)
{
    var board = simulation.Board;
    Console.WriteLine("Enter rows first, one at a time, in 'pointsbombs' format, ex: 071");
    for (int i = 0; i < 5; i++)
    {
        var line = Console.ReadLine()?.Trim();
        if (line is null) return;
        if (line.Length < 2 || line.Length > 3)
        {
            Console.WriteLine("Invalid input");
            return;
        }
        board.Rows.Add(new()
        {
            Points = int.Parse(line[..^1]),
            Bombs = int.Parse(line[^1..])
        });
    }

    Console.WriteLine("Now enter columns in 'pointsbombs' format, ex: 071");
    for (int i = 0; i < 5; i++)
    {
        var line = Console.ReadLine()?.Trim();
        if (line is null) return;
        if (line.Length < 2 || line.Length > 3)
        {
            Console.WriteLine("Invalid input");
            return;
        }
        board.Cols.Add(new()
        {
            Points = int.Parse(line[..^1]),
            Bombs = int.Parse(line[^1..])
        });
    }

    // in the board, values can be bomb (0), 1, 2, or 3. Only the 2 and 3 matter, 1 and bomb are both "bad" (only bomb is end of game)
    // so we need to find any space that has a 100% chance of being a 2 or 3. Failing that, find the *best* chance of being a 2 or 3.
    // The key trick is that if the number of points+bombs in a line are 5, then we don't want to select any of those spaces because they MUST be 1 or bomb
    // Generalizing a bit, if the sum of the known spaces and 1 per unknown space is equal to the points+bombs in a line, then every unknown space is worthless
    // If points+bombs > known_points+unknown_spaces then at least one of the unknown spaces must be a 2 or 3
    // Ultimately we can simulate all possible boards that satisfy the criteria to get the likelihood of a space being worthwhile. Eventually it will come down to chance (often a 50-50)

    var (safeRows, safeCols) = simulation.RowsAndColumnsFinalized();

    foreach (var row in safeRows)
    {
        Console.WriteLine($"Uncover row {row} before continuing.");
    }

    foreach (var col in safeCols)
    {
        Console.WriteLine($"Uncover column {col} before continuing.");
    }

    if (safeRows.Any() || safeCols.Any())
    {
        Console.WriteLine("Enter spaces, row then column then value (ex 213: row 2, col 1, val=3)");
        while (true)
        {
            var line = Console.ReadLine()?.Trim() ?? string.Empty;
            if (!EnterSpaceData(board, line))
            {
                break;
            }
        }
    }
}

bool run = true;
var simulation = new Simulation();
while (run)
{
    simulation.StartNew();
    Setup(simulation);

    while (true)
    {
        // single line capabilities based on known points, bombs, and unknowns
        simulation.Simulate();

        // row+column combined restrictions
        // ex: 7.2 - 0 X 2 X X 0
        // Either both 0 spaces are 2 OR one is 3 and the other BombOrOne
        // Using the column data we could narrow down the likelihood of 2-2 or 3-1

        PrintBoard(simulation.Board);
        Console.WriteLine("Enter space, row then column then value (ex 213: row 2, col 1, val=3)");
        var line = Console.ReadLine()?.Trim() ?? string.Empty;
        if (string.Equals(line, "r", StringComparison.OrdinalIgnoreCase))
        {
            //restart
            Console.WriteLine("Restarting, new board.");
            break;
        }
        else if (string.Equals(line, "q", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Exiting");
            run = false;
            break;
        }
        else
        {
            EnterSpaceData(simulation.Board, line);
        }
    }
}