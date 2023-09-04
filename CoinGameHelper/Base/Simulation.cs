namespace CoinGameHelper;

public class Simulation
{
    public Board Board { get; private set; } = new();
    public bool WereRowsAndColumnsFinalized { get; private set; }

    public Simulation()
    {
        StartNew();
    }

    public void StartNew()
    {
        WereRowsAndColumnsFinalized = false;
        Board = new Board();
        for (int i = 0; i< 5; i++)
        {
            Board.Rows.Add(new());
            Board.Columns.Add(new());
        }
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
                    safeRows.Add(i+1);
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
                    safeColumns.Add(i+1);
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

    public void Simulate()
    {
        //simulation step 1:
        // check for any rows/columns where the known points + unknowns == bombs+points
        // the rest can be marked as BombOrOne in that case.
        // if known points is met, the rest are bombs
        // if all the bombs are known, the rest are safe to choose (but value still unknown)
        for (int i = 0; i < 5; i++)
        {
            var rowScore = Board.GetRowScore(i);
            if (rowScore == Board.Rows[i].Points)
            {
                // any unknowns are bombs
                for (int j = 0; j < 5; j++)
                {
                    var type = Board.GetValue(i, j);
                    if (type is SpaceType.Unknown or SpaceType.BombOrOne)
                    {
                        Board.SetValue(SpaceType.Bomb, i, j);
                    }
                }
            }
            var rowBombs = Board.GetRowKnownBombs(i);
            if (rowBombs == Board.Rows[i].Bombs)
            {
                //all bombs known, any unknowns are safe
                for (int j = 0; j < 5; j++)
                {
                    var type = Board.GetValue(i, j);
                    if (type is SpaceType.Unknown)
                    {
                        Board.SetValue(SpaceType.Safe, i, j);
                    }
                    else if (type is SpaceType.BombOrOne)
                    { // If BombOrOne, but we know all the bombs, then it must be One
                        Board.SetValue(SpaceType.One, i, j);
                    }
                }
            }
            var rowUnknowns = Board.GetRowUnknownCount(i);
            if (rowScore + rowBombs + rowUnknowns == Board.Rows[i].Points + Board.Rows[i].Bombs)
            {
                // everything unknown is a BombOrOne, ie not worth choosing
                for (int j = 0; j < 5; j++)
                {
                    var type = Board.GetValue(i, j);
                    if (type is SpaceType.Unknown)
                    {
                        Board.SetValue(SpaceType.BombOrOne, i, j);
                    }
                }
            }
            // if the (points+bombs) - (known score + BombOrOne spaces) >= 3*(0 spaces - 1)+2
            // then all remaining spaces must be 2 or 3 spaces, ie safe
            var rowDiff = Board.Rows[i].Points + Board.Rows[i].Bombs - Board.GetRowScore(i, bombVal: 1);
            if (rowDiff > (3 * (Board.GetRowUnknownCount(i, false) - 1) + 2))
            {
                for (int j = 0; j < 5; j++)
                {
                    var type = Board.GetValue(i, j);
                    if (type is SpaceType.Unknown)
                    {
                        Board.SetValue(SpaceType.Safe, i, j);
                    }
                }
            }
            else if (rowDiff == Board.GetRowUnknownCount(i, false))
            {
                // the rest must be 1
                for (int j = 0; j < 5; j++)
                {
                    var type = Board.GetValue(i, j);
                    if (type is SpaceType.Unknown or SpaceType.Safe)
                    {
                        Board.SetValue(SpaceType.One, i, j);
                    }
                }
            }

            var colScore = Board.GetColumnScore(i);
            if (colScore == Board.Columns[i].Points)
            {
                // any unknowns are bombs
                for (int j = 0; j < 5; j++)
                {
                    var type = Board.GetValue(j, i);
                    if (type is SpaceType.Unknown or SpaceType.BombOrOne)
                    {
                        Board.SetValue(SpaceType.Bomb, j, i);
                    }
                }
            }
            var colBombs = Board.GetColumnsKnownBombs(i);
            if (colBombs == Board.Columns[i].Bombs)
            {
                //all bombs known, any unknowns are safe
                for (int j = 0; j < 5; j++)
                {
                    var type = Board.GetValue(j, i);
                    if (type is SpaceType.Unknown)
                    {
                        Board.SetValue(SpaceType.Safe, j, i);
                    }
                    else if (type is SpaceType.BombOrOne)
                    { // If BombOrOne, but we know all the bombs, then it must be One
                        Board.SetValue(SpaceType.One, j, i);
                    }
                }
            }
            var colUnknowns = Board.GetColumnUnknownCount(i);
            if (colScore + colBombs + colUnknowns == Board.Columns[i].Points + Board.Columns[i].Bombs)
            {
                // everything unknown is a BombOrOne, ie not worth choosing
                for (int j = 0; j < 5; j++)
                {
                    var type = Board.GetValue(j, i);
                    if (type is SpaceType.Unknown)
                    {
                        Board.SetValue(SpaceType.BombOrOne, j, i);
                    }
                }
            }

            // if the (points+bombs) - (known score + BombOrOne spaces) >= 3*(0 spaces - 1)+2
            // then all remaining spaces must be 2 or 3 spaces, ie safe
            var colDiff = Board.Columns[i].Points + Board.Columns[i].Bombs - Board.GetColumnScore(i, bombVal: 1);
            if (colDiff > (3 * (Board.GetColumnUnknownCount(i, false) - 1) + 2))
            {
                for (int j = 0; j < 5; j++)
                {
                    var type = Board.GetValue(j, i);
                    if (type is SpaceType.Unknown)
                    {
                        Board.SetValue(SpaceType.Safe, j, i);
                    }
                }
            }
            else if (colDiff == Board.GetColumnUnknownCount(i, false))
            {
                // the rest must be 1
                for (int j = 0; j < 5; j++)
                {
                    var type = Board.GetValue(j, i);
                    if (type is SpaceType.Unknown or SpaceType.Safe)
                    {
                        Board.SetValue(SpaceType.One, j, i);
                    }
                }
            }
        }
    }

}
