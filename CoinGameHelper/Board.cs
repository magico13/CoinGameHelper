namespace CoinGameHelper
{
    internal enum SpaceType
    {
        Unknown = 0,
        One = 1,
        Two = 2,
        Three = 3,
        Bomb = 4,
        BombOrOne = 5,
        Safe = 6
    }

    internal class Board
    {
        public List<LineInfo> Rows { get; set; } = new List<LineInfo>(5);

        public List<LineInfo> Cols { get; set; } = new List<LineInfo>(5);
        public int[,] GameBoard { get; set; } = new int[5,5];

        public Board(){}
        public Board(Board original)
        {
            GameBoard = original.GameBoard;
        }

        public SpaceType GetValue(int row, int col)
        {
            return (SpaceType)GameBoard[row, col];
        }

        public void SetValue(SpaceType val, int row, int col)
        {
            GameBoard[row, col] = (int)val;
        }

        public void SetValue(int val, int row, int col)
        {
            GameBoard[row, col] = val;
        }

        public string GetSymbol(int row, int col)
        {
            return GetSymbol(GameBoard[row, col]);
        }

        public static string GetSymbol(int val)
        {
            SpaceType type = (SpaceType)val;
            return type switch
            {
                SpaceType.Bomb => "B",
                SpaceType.BombOrOne => "X",
                SpaceType.Safe => "S",
                _ => val.ToString(),
            };
        }

        public int GetRowScore(int row, int bombVal = 0)
        {
            int score = 0;
            for (int i=0; i<5; i++)
            {
                score += SpaceValue(row, i, bombVal);
            }
            return score;
        }

        public int GetColumnScore(int col, int bombVal = 0)
        {
            int score = 0;
            for (int i=0; i<5; i++)
            {
                score += SpaceValue(i, col, bombVal);
            }
            return score;
        }

        public int GetRowKnownBombs(int row)
        {
            int bombs = 0;
            for (int i=0; i<5; i++)
            {
                var type = GetValue(row, i);
                if (type is SpaceType.Bomb)
                {
                    bombs++;
                }
            }
            return bombs;
        }

        public int GetColumnsKnownBombs(int col)
        {
            int bombs = 0;
            for (int i=0; i<5; i++)
            {
                var type = GetValue(i, col);
                if (type is SpaceType.Bomb)
                {
                    bombs++;
                }
            }
            return bombs;
        }

        public int GetRowUnknownCount(int row, bool includeBombOrOne = true)
        {
            int unknowns = 0;
            for (int i=0; i<5; i++)
            {
                var type = GetValue(row, i);
                if (type is SpaceType.Unknown or SpaceType.Safe
                    || (includeBombOrOne && type is SpaceType.BombOrOne))
                {
                    unknowns++;
                }
            }
            return unknowns;
        }

        public int GetColumnUnknownCount(int col, bool includeBombOrOne = true)
        {
            int unknowns = 0;
            for (int i=0; i<5; i++)
            {
                var type = GetValue(i, col);
                if (type is SpaceType.Unknown or SpaceType.Safe
                    || (includeBombOrOne && type is SpaceType.BombOrOne))
                {
                    unknowns++;
                }
            }
            return unknowns;
        }

        private int SpaceValue(int row, int col, int bombValue = 0, int unknownValue = 0)
        {
            var type = GetValue(row, col);
            return type switch
            {
                SpaceType.One or SpaceType.Two or SpaceType.Three => (int)type,
                SpaceType.Safe or SpaceType.Unknown => unknownValue,
                SpaceType.Bomb or SpaceType.BombOrOne => bombValue,
                _ => 0,
            };
        }
    }
}
