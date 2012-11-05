using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;

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

        private static void PrintFieldPart(SudokuValues values, int part)
        {
            for (int v = part; v < part + 3; v++)
                Console.Write(values.Contains((byte)v) ? (v + 1).ToString(CultureInfo.InvariantCulture) : " ");
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
            Sudoku sudoku = LoadSudoku(args[0]);
            Console.WriteLine("Loaded sudoku:");
            PrintSimple(sudoku);
            Console.WriteLine("Solving...");
            switch (sudoku.BreadthFirstRecursiveSolve())
            {
                case SudokuSolutionState.Solved:
                    Console.WriteLine("Solved!");
                    PrintSimple(sudoku);
                    break;
                case SudokuSolutionState.NoSolution:
                    Console.WriteLine("The sudoku has no solutions!");
                    break;
                case SudokuSolutionState.TooHard:
                    Console.WriteLine("The sudoku was too hard!");
                    break;
            }
            Console.ReadLine();
        }
    }
}
