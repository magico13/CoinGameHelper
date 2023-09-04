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

static void PrintBoard(Board board)
{
    for (int row = 0; row < 5; row++)
    {
        var rowDiff = board.Rows[row].Points + board.Rows[row].Bombs - board.GetRowScore(row, bombVal: 1);
        for (int col = 0; col < 5; col++)
        {
            var symbol = board.GetSymbol(row, col);
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

static void Simulate1(Board board)
{
    //simulation step 1:
    // check for any rows/columns where the known points + unknowns == bombs+points
    // the rest can be marked as BombOrOne in that case.
    // if known points is met, the rest are bombs
    // if all the bombs are known, the rest are safe to choose (but value still unknown)
    for (int i=0; i<5; i++)
    {
        var rowScore = board.GetRowScore(i);
        if (rowScore == board.Rows[i].Points)
        {
            // any unknowns are bombs
            for (int j=0; j<5; j++)
            {
                var type = board.GetValue(i, j);
                if (type is SpaceType.Unknown or SpaceType.BombOrOne)
                {
                    board.SetValue(SpaceType.Bomb, i, j);
                }
            }
        }
        var rowBombs = board.GetRowKnownBombs(i);
        if (rowBombs == board.Rows[i].Bombs)
        {
            //all bombs known, any unknowns are safe
            for (int j=0; j<5; j++)
            {
                var type = board.GetValue(i, j);
                if (type is SpaceType.Unknown)
                {
                    board.SetValue(SpaceType.Safe, i, j);
                }
                else if (type is SpaceType.BombOrOne)
                { // If BombOrOne, but we know all the bombs, then it must be One
                    board.SetValue(SpaceType.One, i, j);
                }
            }
        }
        var rowUnknowns = board.GetRowUnknownCount(i);
        if (rowScore + rowBombs + rowUnknowns == board.Rows[i].Points + board.Rows[i].Bombs)
        {
            // everything unknown is a BombOrOne, ie not worth choosing
            for (int j=0; j<5; j++)
            {
                var type = board.GetValue(i, j);
                if (type is SpaceType.Unknown)
                {
                    board.SetValue(SpaceType.BombOrOne, i, j);
                }
            }
        }
        // if the (points+bombs) - (known score + BombOrOne spaces) >= 3*(0 spaces - 1)+2
        // then all remaining spaces must be 2 or 3 spaces, ie safe
        var rowDiff = board.Rows[i].Points + board.Rows[i].Bombs - board.GetRowScore(i, bombVal: 1);
        if (rowDiff > (3 * (board.GetRowUnknownCount(i, false) - 1) + 2))
        {
            for (int j = 0; j < 5; j++)
            {
                var type = board.GetValue(i, j);
                if (type is SpaceType.Unknown)
                {
                    board.SetValue(SpaceType.Safe, i, j);
                }
            }
        }
        else if (rowDiff == board.GetRowUnknownCount(i, false))
        {
            // the rest must be 1
            for (int j = 0; j < 5; j++)
            {
                var type = board.GetValue(i, j);
                if (type is SpaceType.Unknown or SpaceType.Safe)
                {
                    board.SetValue(SpaceType.One, i, j);
                }
            }
        }

        var colScore = board.GetColumnScore(i);
        if (colScore == board.Cols[i].Points)
        {
            // any unknowns are bombs
            for (int j=0; j<5; j++)
            {
                var type = board.GetValue(j, i);
                if (type is SpaceType.Unknown or SpaceType.BombOrOne)
                {
                    board.SetValue(SpaceType.Bomb, j, i);
                }
            }
        }
        var colBombs = board.GetColumnsKnownBombs(i);
        if (colBombs == board.Cols[i].Bombs)
        {
            //all bombs known, any unknowns are safe
            for (int j=0; j<5; j++)
            {
                var type = board.GetValue(j, i);
                if (type is SpaceType.Unknown)
                {
                    board.SetValue(SpaceType.Safe, j, i);
                }
                else if (type is SpaceType.BombOrOne)
                { // If BombOrOne, but we know all the bombs, then it must be One
                    board.SetValue(SpaceType.One, j, i);
                }
            }
        }
        var colUnknowns = board.GetColumnUnknownCount(i);
        if (colScore + colBombs + colUnknowns == board.Cols[i].Points + board.Cols[i].Bombs)
        {
            // everything unknown is a BombOrOne, ie not worth choosing
            for (int j=0; j<5; j++)
            {
                var type = board.GetValue(j, i);
                if (type is SpaceType.Unknown)
                {
                    board.SetValue(SpaceType.BombOrOne, j, i);
                }
            }
        }

        // if the (points+bombs) - (known score + BombOrOne spaces) >= 3*(0 spaces - 1)+2
        // then all remaining spaces must be 2 or 3 spaces, ie safe
        var colDiff = board.Cols[i].Points + board.Cols[i].Bombs - board.GetColumnScore(i, bombVal: 1);
        if (colDiff > (3 * (board.GetColumnUnknownCount(i, false) - 1) + 2))
        {
            for (int j = 0; j < 5; j++)
            {
                var type = board.GetValue(j, i);
                if (type is SpaceType.Unknown)
                {
                    board.SetValue(SpaceType.Safe, j, i);
                }
            }
        }
        else if (colDiff == board.GetColumnUnknownCount(i, false))
        {
            // the rest must be 1
            for (int j = 0; j < 5; j++)
            {
                var type = board.GetValue(j, i);
                if (type is SpaceType.Unknown or SpaceType.Safe)
                {
                    board.SetValue(SpaceType.One, j, i);
                }
            }
        }
    }
}

static void Setup(Board board)
{

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


    for (int i = 0; i < 5; i++)
    {
        // if the sum of points and bombs is 5, the line is worthless (mark as x)
        var row = board.Rows[i];
        var col = board.Cols[i];

        if (row.Bombs + row.Points == 5)
        {
            var type = row.Bombs == 5 ? SpaceType.Bomb : SpaceType.BombOrOne;
            for (int j = 0; j < 5; j++)
            {
                var existingType = board.GetValue(i, j);
                if (existingType is SpaceType.Unknown or SpaceType.BombOrOne)
                {
                    board.SetValue(type, i, j);
                }
            }
        }
        if (col.Bombs + col.Points == 5)
        {
            var type = col.Bombs == 5 ? SpaceType.Bomb : SpaceType.BombOrOne;
            for (int j = 0; j < 5; j++)
            {
                var existingType = board.GetValue(j, i);
                if (existingType is SpaceType.Unknown or SpaceType.BombOrOne)
                {
                    board.SetValue(type, j, i);
                }
            }
        }
    }

    // first easy case, if any line is X.0 then tell the player to uncover those spaces. No bombs means it is safe to do so and it will limit the total possible boards
    var safeLineFound = false;
    for (int i = 0; i < 5; i++)
    {
        var row = board.Rows[i];
        var col = board.Cols[i];
        // If there are no bombs in the line it is safe. Might be all 1s
        if (row.Bombs == 0)
        {
            if (row.Points > 5)
            {
                Console.WriteLine($"Uncover row {i + 1} before continuing.");
                safeLineFound = true;
            }
            else
            {
                // each space must be 1 point
                for (int j = 0; j < 5; j++)
                {
                    board.SetValue(SpaceType.One, i, j);
                }
            }
        }
        if (col.Bombs == 0)
        {
            if (col.Points > 5)
            {
                Console.WriteLine($"Uncover column {i + 1} before continuing.");
                safeLineFound = true;
            }
            else
            {
                // each space must be 1 point
                for (int j = 0; j < 5; j++)
                {
                    board.SetValue(SpaceType.One, j, i);
                }
            }
        }


    }

    if (safeLineFound)
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
while (run)
{
    var knownBoard = new Board();
    Setup(knownBoard);

    while (true)
    {
        // single line capabilities based on known points, bombs, and unknowns
        Simulate1(knownBoard);

        // row+column combined restrictions
        // ex: 7.2 - 0 X 2 X X 0
        // Either both 0 spaces are 2 OR one is 3 and the other BombOrOne
        // Using the column data we could narrow down the likelihood of 2-2 or 3-1

        PrintBoard(knownBoard);
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
            EnterSpaceData(knownBoard, line);
        }
    }
}