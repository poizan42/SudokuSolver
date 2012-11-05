using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace PoiTech.SudokuSolver
{
    public struct SudokuPosition
    {
        public int Col;
        public int Row;

        public SudokuPosition(int col, int row)
        {
            this.Col = col;
            this.Row = row;
        }

        public SudokuPosition(Tuple<int, int> vals)
        {
            this.Col = vals.Item1;
            this.Row = vals.Item2;
        }

        public override bool Equals(object obj)
        {
            return obj is SudokuPosition &&
                ((SudokuPosition)obj).Col == Col &&
                ((SudokuPosition)obj).Row == Row;
        }

        public override int GetHashCode()
        {
            return Col*9 + Row;
        }

        public static bool operator ==(SudokuPosition a, SudokuPosition b)
        {
            return a.Col == b.Col && a.Row == b.Row;
        }

        public static bool operator !=(SudokuPosition a, SudokuPosition b)
        {
            return a.Col != b.Col || a.Row != b.Row;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "({0},{1})", Col, Row);
        }
    }
    public class SudokuPositions : List<SudokuPosition> 
    {
        public SudokuPositions() : base() { }
    }

    public class SudokuOverdeterminedException : Exception
    {
    }

    public delegate SudokuPosition Subdivision(int idx);

    public enum SudokuSolutionState { Solved, NoSolution, TooHard };

    public class Sudoku
    {
        //order col,row
        private SudokuValues[,] data = new SudokuValues[9,9];

        public Sudoku()
        {
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    data[i, j] = SudokuValues.All;
        }

        public Sudoku(Sudoku copyFrom)
        {
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    data[i, j] = copyFrom.data[i, j];
        }

        public SudokuValues this[SudokuPosition pos]
        {
            get
            {
                return data[pos.Col, pos.Row];
            }
            set
            {
                data[pos.Col, pos.Row] = value;
            }
        }

        public Subdivision GetColSubdivision(int idx)
        {
            return i => new SudokuPosition(idx, i);
        }

        public Subdivision GetRowSubdivision(int idx)
        {
            return i => new SudokuPosition(i, idx);
        }

        private Tuple<int, int>[] subGridTL = new Tuple<int, int>[] { 
            Tuple.Create(0,0),
            Tuple.Create(3,0),
            Tuple.Create(6,0),
            Tuple.Create(0,3),
            Tuple.Create(3,3),
            Tuple.Create(6,3),
            Tuple.Create(0,6),
            Tuple.Create(3,6),
            Tuple.Create(6,6)
        };

        private Tuple<int, int>[] subGridOffset = new Tuple<int, int>[] { 
            Tuple.Create(0,0),
            Tuple.Create(0,1),
            Tuple.Create(0,2),
            Tuple.Create(1,0),
            Tuple.Create(1,1),
            Tuple.Create(1,2),
            Tuple.Create(2,0),
            Tuple.Create(2,1),
            Tuple.Create(2,2)
        };

        public Subdivision GetSubGridSubdivision(int idx)
        {
            Tuple<int, int> topLeft = subGridTL[idx];
            return i => new SudokuPosition(topLeft.Item1 + subGridOffset[i].Item1, topLeft.Item2 + subGridOffset[i].Item2);
        }

        public KeyValuePair<SudokuValues, SudokuPositions>[] BuildSubdivisionValues(Subdivision subdivision)
        {
            Dictionary<SudokuValues, SudokuPositions> values = new Dictionary<SudokuValues, SudokuPositions>();
            for (int i = 0; i < 9; i++)
            {
                SudokuPosition pos = subdivision(i);
                SudokuPositions positions;
                SudokuValues cellVal = this[pos];
                if (!values.TryGetValue(cellVal, out positions))
                {
                    positions = new SudokuPositions();
                    values.Add(cellVal, positions);
                }
                positions.Add(pos);
            }
            return values.ToArray();
        }

        public bool ValidateSubdivision(KeyValuePair<SudokuValues, SudokuPositions>[] subdivisionValues)
        {
            foreach (var kv in subdivisionValues)
            {
                if (kv.Key.Count < kv.Value.Count) //overdetermined
                    return false;
            }
            return true;
        }

        //Other cells require one or more values because they are the only ones that can contain those values
        public bool ConstrainToSubdivisionOtherReqVal(KeyValuePair<SudokuValues, SudokuPositions>[] subdivisionValues, SudokuPosition cell)
        {
            bool changed = false;
            for (int svi = 0; svi < subdivisionValues.Length; svi++)
            {
                var kv = subdivisionValues[svi];
                if ((kv.Key & this[cell]) != SudokuValues.None)
                {
                    int notThisCount = 0;
                    for (int i = 0; i < kv.Value.Count; i++)
                        if (kv.Value[i] != cell)
                            notThisCount++;
                    if (kv.Key.Count == notThisCount)
                    {
                        this[cell] -= kv.Key;
                        changed = true;
                    }
                }
                if (this[cell] == SudokuValues.None)
                    throw new SudokuOverdeterminedException();
            }
            return changed;
        }

        //Some values are not available to any other cell in the subdivision, so the current cell must take one of them
        public bool ConstrainToSubdivisionThisOnlyVal(KeyValuePair<SudokuValues, SudokuPositions>[] subdivisionValues, SudokuPosition cell)
        {
            SudokuValues cellVal = this[cell];
            SudokuValues availableValues = cellVal;
            for (int svi = 0; svi < subdivisionValues.Length; svi++)
            {
                var kv = subdivisionValues[svi];
                if (kv.Value.Count == 1 && kv.Value[0] == cell)
                  continue;
                availableValues -= kv.Key;
            }
            if (!availableValues.IsEmpty && availableValues != cellVal)
            {
                this[cell] = availableValues;
                return true;
            }
            else
                return false;
        }

        public bool ConstrainAll(Func<int, Subdivision> Subdivider)
        {
            bool changed = false;
            for (int i = 0; i < 9; i++)
            {
                Subdivision sd = Subdivider(i);
                var sdv = BuildSubdivisionValues(sd);
                for (int j = 0; j < 9; j++)
                {
                    changed |= ConstrainToSubdivisionThisOnlyVal(sdv, sd(j));
                    changed |= ConstrainToSubdivisionOtherReqVal(sdv, sd(j));
                }
            }
            return changed;
        }

        public bool IsSolved()
        {
            for (int col = 0; col < 9; col++)
                for (int row = 0; row < 9; row++)
                    if (data[col, row].Count != 1)
                        return false;
            return true;
        }

        public SudokuPosition? GetFirstUnsolvedPosition()
        {
            for (int col = 0; col < 9; col++)
                for (int row = 0; row < 9; row++)
                    if (data[col, row].Count != 1)
                        return new SudokuPosition(col,row);
            return null;
        }

        public bool SolveSingleRound()
        {
            bool changed = ConstrainAll(GetColSubdivision);
            changed |= ConstrainAll(GetRowSubdivision);
            changed |= ConstrainAll(GetSubGridSubdivision);
            return changed;
        }

        public void GreedySolve()
        {
            bool changed = true;
            while (changed)
                changed = SolveSingleRound();
        }

        public SudokuSolutionState BreadthFirstRecursiveSolve(int searchLimit = 1024)
        {
            int branchCount = 0;
            int invalidationCount = 0;
            bool searchLimitReached = false;
            List<Sudoku> sudokus = new List<Sudoku>();
            List<int> remSudokus = new List<int>();
            sudokus.Add(this);
            while (true)
            {
                remSudokus.Clear();
                for (int i = 0; i < sudokus.Count; i++)
                {
                    try
                    {
                        sudokus[i].GreedySolve();
                    }
                    catch (SudokuOverdeterminedException)
                    {
                        remSudokus.Add(i);
                        invalidationCount++;
                    }
                }
                int remOffset = 0;
                foreach (int i in remSudokus)
                {
                    sudokus.RemoveAt(i - remOffset);
                    remOffset++;
                }
                int startLen = sudokus.Count;
                if (startLen == 0)
                    return searchLimitReached ? SudokuSolutionState.TooHard : SudokuSolutionState.NoSolution;
                else if (startLen > searchLimit)
                    searchLimitReached = true;
                for (int i = 0; i < startLen; i++)
                {
                    Sudoku s = sudokus[i];
                    SudokuPosition? posn = s.GetFirstUnsolvedPosition();
                    if (posn == null) //it's a solution!
                    {
                        data = s.data;
                        Console.WriteLine("Solution found. Branch count {0}, invalidation count {1}, unexplored branches {2}.", 
                            branchCount, invalidationCount, startLen - 1);
                        return SudokuSolutionState.Solved;
                    }
                    SudokuPosition pos = posn.Value;
                    SudokuValues values = s[pos];
                    if (searchLimitReached)
                        Console.WriteLine("Search depth reached. Guessing from {0} for {1}.", pos, values.First() + 1);
                    else
                        Console.WriteLine("Solving recursively from {0} for {1}.", pos, values);
                    bool first = true;
                    foreach (byte num in values)
                    {
                        if (!first)
                            s = new Sudoku(s);
                        s[pos] = new SudokuValues(new byte[] { num });
                        if (searchLimitReached)
                            break;
                        if (!first)
                        {
                            sudokus.Add(s);
                            branchCount++;
                        }
                        else
                            first = false;
                    }
                }
            }
        }
    }
}
