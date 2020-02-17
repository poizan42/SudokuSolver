using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace PoiTech.SudokuSolver
{   
    class Program
    {
        public static Sudoku LoadSudoku(string filename)
        {
            Sudoku sudoku = new Sudoku();
            using (StreamReader sr = new StreamReader(filename, Encoding.ASCII))
            {
                for (int row = 0; row < 9; row++)
                {
                    string[] vals = sr.ReadLine().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (vals.Length == 0 || vals[0][0] == '#')
                    {
                        row--;
                        continue;
                    }
                    for (int col = 0; col < 9; col++)
                    {
                        SudokuValues val = vals[col] == "*" ? SudokuValues.All : 
                            new SudokuValues(new byte[] { (byte)(Byte.Parse(vals[col], CultureInfo.InvariantCulture) - 1) });
                        sudoku[new SudokuPosition(col, row)] = val;
                    }
                }
            }
            return sudoku;
        }

        private static List<(Sudoku, string)> LoadSudokusCsv(string filename)
        {
            var sudokus = new List<(Sudoku, string)>();
            bool firstLine = true;
            using (StreamReader sr = new StreamReader(filename, Encoding.ASCII))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (firstLine && line.Length > 0 && !char.IsDigit(line[0]))
                        continue;
                    firstLine = false;
                    string[] parts = line.Split(',');
                    string quiz = parts[0];
                    string sol = parts.Length > 1 ? parts[1] : null;
                    Sudoku sudoku = new Sudoku();
                    for (int row = 0; row < 9; row++)
                    {
                        for (int col = 0; col < 9; col++)
                        {
                            int iVal = quiz[row * 9 + col] - '0';
                            SudokuValues val = iVal == 0 ? SudokuValues.All : SudokuValues.FromSingleValueUnbiased((byte)iVal);
                            sudoku[new SudokuPosition(col, row)] = val;
                        }
                    }
                    sudokus.Add((sudoku, sol));
                }
            }
            return sudokus;
        }

        private static void PrintFieldPart(SudokuValues values, int part)
        {
            for (int v = part; v < part + 3; v++)
                Console.Write(values.Contains((byte)v) ? (v + 1).ToString(CultureInfo.InvariantCulture) : " ");
        }

        private static bool CompareSolution(Sudoku sudoku, string solution)
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    int iVal = solution[row * 9 + col] - '0';
                    if (!sudoku[new SudokuPosition(col, row)].ContainsUnbiased((byte)iVal))
                        return false;
                }
            }
            return true;
        }

        private static void PrintSudoku(Sudoku sudoku)
        {
            for (int row = 0; row < 9; row++)
            {
                if (row % 3 == 0)
                    Console.WriteLine("*************************************");
                else
                    Console.WriteLine("*---+---+---*---+---+---*---+---+---*");
                for (int part = 0; part < 9; part += 3)
                {
                    for (int col = 0; col < 9; col++)
                    {
                        Console.Write(col % 3 == 0 ? '*' : '|');
                        PrintFieldPart(sudoku[new SudokuPosition(col, row)], part);
                    }
                    Console.WriteLine('*');
                }
            }
            Console.WriteLine("*************************************");
        }

        public static void PrintSimple(Sudoku sudoku)
        {
            for (int row = 0; row < 9; row++)
            {
                if (row % 3 == 0)
                    Console.WriteLine("+---+---+---+");
                for (int col = 0; col < 9; col++)
                {
                    if (col % 3 == 0)
                        Console.Write('|');
                    SudokuValues cellVal = sudoku[new SudokuPosition(col, row)];
                    if (cellVal == SudokuValues.All)
                        Console.Write(' ');
                    else if (cellVal.Count == 1)
                        Console.Write(cellVal.First() + 1);
                    else
                        Console.Write('?');
                }
                Console.WriteLine("|");
            }
            Console.WriteLine("+---+---+---+");
        }

        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            List<(Sudoku, string)> sudokus;
            if (args[0] == "-csv")
            {
                sw.Start();
                sudokus = LoadSudokusCsv(args[1]);
                Console.WriteLine("{0} loaded in {1}", args[1], sw.Elapsed);
            }
            else
                sudokus = new List<(Sudoku, string)> { (LoadSudoku(args[0]), null) };
            bool print = sudokus.Count <= 1;
            sw.Restart();
            int i = 1;
            foreach ((Sudoku sudoku, string solution) in sudokus)
            {
                if (print)
                {
                    Console.WriteLine("Loaded sudoku:");
                    PrintSimple(sudoku);
                    Console.WriteLine("Solving...");
                }
                SudokuSolutionState solState = sudoku.BreadthFirstRecursiveSolve(print: print);
                if (solution != null)
                {
                    if (!CompareSolution(sudoku, solution))
                    {
                        Console.WriteLine("{0}: Wrong solution!", i);
                    }
                }
                switch (solState)
                {
                    case SudokuSolutionState.Solved:
                        if (print)
                        {
                            Console.WriteLine("Solved!");
                            PrintSimple(sudoku);
                        }
                        break;
                    case SudokuSolutionState.NoSolution:
                        Console.WriteLine("{0}: The sudoku has no solutions!", i);
                        break;
                    case SudokuSolutionState.TooHard:
                        Console.WriteLine("{0}: The sudoku was too hard!", i);
                        break;
                }
                i++;
            }
            Console.WriteLine("Total time: {0}", sw.Elapsed);
            //Console.ReadLine();
        }
    }
}
