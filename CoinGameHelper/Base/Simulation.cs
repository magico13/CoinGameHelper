namespace CoinGameHelper;

public class Simulation
{
    public Board Board { get; private set; } = new();
    public bool WereRowsAndColumnsFinalized { get; private set; }
    public HashSet<Board> AllPossibleBoards { get; } = new();
    private List<double> SafeProbabilities { get; } = new();

    public Simulation()
    {
        StartNew();
    }

    public void StartNew()
    {
        WereRowsAndColumnsFinalized = false;
        Board = new Board();
        for (int i = 0; i < 5; i++)
        {
            Board.Rows.Add(new());
            Board.Columns.Add(new());
        }
        AllPossibleBoards.Clear();
        SafeProbabilities.Clear();
    }

    public (List<int> safeRows, List<int> safeColumns) FinalizeRowsAndColumns()
    {
        for (int i = 0; i < 5; i++)
        {
            // if the sum of points and bombs is 5, the line is worthless (mark as x)
            var row = Board.Rows[i];
            var col = Board.Columns[i];

            if (row.Bombs + row.Points == 5)
            {
                var type = row.Bombs == 5 ? SpaceType.Bomb : SpaceType.BombOrOne;
                for (int j = 0; j < 5; j++)
                {
                    var existingType = Board.GetValue(i, j);
                    if (existingType is SpaceType.Unknown or SpaceType.BombOrOne)
                    {
                        Board.SetValue(type, i, j);
                    }
                }
            }
            if (col.Bombs + col.Points == 5)
            {
                var type = col.Bombs == 5 ? SpaceType.Bomb : SpaceType.BombOrOne;
                for (int j = 0; j < 5; j++)
                {
                    var existingType = Board.GetValue(j, i);
                    if (existingType is SpaceType.Unknown or SpaceType.BombOrOne)
                    {
                        Board.SetValue(type, j, i);
                    }
                }
            }
        }

        // first easy case, if any line is X.0 then tell the player to uncover those spaces. No bombs means it is safe to do so and it will limit the total possible Boards
        List<int> safeRows = new();
        List<int> safeColumns = new();
        for (int i = 0; i < 5; i++)
        {
            var row = Board.Rows[i];
            var col = Board.Columns[i];
            // If there are no bombs in the line it is safe. Might be all 1s
            if (row.Bombs == 0)
            {
                if (row.Points > 5)
                {
                    //Console.WriteLine($"Uncover row {i + 1} before continuing.");
                    safeRows.Add(i + 1);
                }
                else
                {
                    // each space must be 1 point
                    for (int j = 0; j < 5; j++)
                    {
                        Board.SetValue(SpaceType.One, i, j);
                    }
                }
            }
            if (col.Bombs == 0)
            {
                if (col.Points > 5)
                {
                    //Console.WriteLine($"Uncover column {i + 1} before continuing.");
                    safeColumns.Add(i + 1);
                }
                else
                {
                    // each space must be 1 point
                    for (int j = 0; j < 5; j++)
                    {
                        Board.SetValue(SpaceType.One, j, i);
                    }
                }
            }
        }

        WereRowsAndColumnsFinalized = true;
        Simulate();
        return (safeRows, safeColumns);
    }

    public (int row, int column, double value) FindSafestUnknownSpace()
    {
        // Loop through the current board and find the safest unknown space
        // Safest is defined as the space with the highest probability of being safe
        // If there are multiple spaces with the same probability, return the first one found
        // If there are no unknown spaces, return (-1, -1)
        double highestProbability = 0;
        int row = -1;
        int column = -1;

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                var type = Board.GetValue(i, j);
                if (type is SpaceType.Unknown or SpaceType.Safe)
                {
                    var probability = SafeProbabilities[i * 5 + j];
                    if (probability > highestProbability)
                    {
                        highestProbability = probability;
                        row = i;
                        column = j;
                    }
                }
            }
        }

        return (row, column, highestProbability);
    }

    public void Simulate()
    {
        // Step 1 is specific to within a row or column with no cross-row/column information
        Simulate_Step1(Board);
        // Step 2 is to look at the grid as a whole
        Simulate_Step2();
    }

    private static void Simulate_Step1(Board board)
    {
        //simulation step 1:
        // check for any rows/columns where the known points + unknowns == bombs+points
        // the rest can be marked as BombOrOne in that case.
        // if known points is met, the rest are bombs
        // if all the bombs are known, the rest are safe to choose (but value still unknown)
        for (int i = 0; i < 5; i++)
        {
            var rowScore = board.GetRowScore(i);
            if (rowScore == board.Rows[i].Points)
            {
                // any unknowns are bombs
                for (int j = 0; j < 5; j++)
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
                for (int j = 0; j < 5; j++)
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
                for (int j = 0; j < 5; j++)
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
            if (rowDiff >= (3 * (board.GetRowUnknownCount(i, false) - 1) + 2))
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
            if (colScore == board.Columns[i].Points)
            {
                // any unknowns are bombs
                for (int j = 0; j < 5; j++)
                {
                    var type = board.GetValue(j, i);
                    if (type is SpaceType.Unknown or SpaceType.BombOrOne)
                    {
                        board.SetValue(SpaceType.Bomb, j, i);
                    }
                }
            }
            var colBombs = board.GetColumnsKnownBombs(i);
            if (colBombs == board.Columns[i].Bombs)
            {
                //all bombs known, any unknowns are safe
                for (int j = 0; j < 5; j++)
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
            if (colScore + colBombs + colUnknowns == board.Columns[i].Points + board.Columns[i].Bombs)
            {
                // everything unknown is a BombOrOne, ie not worth choosing
                for (int j = 0; j < 5; j++)
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
            var colDiff = board.Columns[i].Points + board.Columns[i].Bombs - board.GetColumnScore(i, bombVal: 1);
            if (colDiff >= (3 * (board.GetColumnUnknownCount(i, false) - 1) + 2))
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

    private void Simulate_Step2()
    {
        // try to figure out which spaces are the safest to choose by calculating the probability that they're safe/a bomb

        // I'm not sure how to do this mathematically. But we can simulate all of the possible boards and see which spaces are the safest to choose
        AllPossibleBoards.Clear();
        SafeProbabilities.Clear();
        SimulateAllBoards(Board, AllPossibleBoards, 0);

        for (int i = 0; i < 25; i++)
        {
            // get the number of boards where the index is a bomb
            // it's safe P = 1 - (bombs/total)
            var row = i / 5;
            var col = i % 5;
            var bombBoards = 0;
            foreach (var board in AllPossibleBoards)
            {
                if (board.GetValue(row, col) is SpaceType.Bomb)
                {
                    bombBoards++;
                }
            }

            var bombProbability = (double)bombBoards / AllPossibleBoards.Count;
            SafeProbabilities.Add(1 - bombProbability);
        }
    }

    private void SimulateAllBoards(Board currentBoard, HashSet<Board> allBoards, int index)
    {
        if (index == 25)
        {
            // if any of the board is unknown then it's not a full valid board
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    var type = currentBoard.GetValue(i, j);
                    if (type is SpaceType.Unknown or SpaceType.BombOrOne)
                    {
                        return;
                    }
                }
                // Check that the rows and columns have the right number of bombs
                var rowBombs = currentBoard.GetRowKnownBombs(i);
                var columnBombs = currentBoard.GetColumnsKnownBombs(i);
                if (rowBombs != currentBoard.Rows[i].Bombs
                    || columnBombs != currentBoard.Columns[i].Bombs)
                {
                    return;
                }
            }

            allBoards.Add(currentBoard.Copy());
            return;
        }

        int row = index / 5;
        int col = index % 5;

        var originalValue = Board.GetValue(row, col);
        if (originalValue is SpaceType.Unknown or SpaceType.BombOrOne)
        {
            // If it can be a bomb, assume it's a bomb
            var rowBombs = currentBoard.GetRowKnownBombs(row);
            var columnBombs = currentBoard.GetColumnsKnownBombs(col);
            if (rowBombs < currentBoard.Rows[row].Bombs
                && columnBombs < currentBoard.Columns[col].Bombs)
            {
                // Can be a bomb
                var bombCopy = currentBoard.Copy();
                bombCopy.SetValue(SpaceType.Bomb, row, col);
                Simulate_Step1(bombCopy);
                SimulateAllBoards(bombCopy, allBoards, index + 1);

                // Reset the square to unrevealed for the next simulation.
                //copy.SetValue(originalValue, row, col);
            }
            //else
            //{
            //    // can't be a bomb but is an unknown, mark as safe?
            //    //var copy = currentBoard.Copy();
            //    ////copy.SetValue(SpaceType.Safe, row, col);
            //    //Simulate_Step1(copy);
            //    SimulateAllBoards(currentBoard, allBoards, index + 1);
            //}
            var copy = currentBoard.Copy();
            SimulateAllBoards(copy, allBoards, index + 1);
        }
        else
        {
            SimulateAllBoards(currentBoard, allBoards, index + 1);
        }
    }
}
