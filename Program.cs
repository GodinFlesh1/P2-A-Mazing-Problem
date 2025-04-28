using System;
using System.Collections.Generic;
using System.Linq; // Needed for Reverse()

namespace RatInAMaze
{
    class Program
    {
        // --- Data Structure Explanations ---

        // dx and dy arrays: Represent movement directions.
        // These arrays store the change in row (dx) and column (dy) for 8 possible moves
        // from a given cell (x, y) to a neighboring cell (x + dx[i], y + dy[i]).
        // Order: Northwest(-1,-1), North(-1,0), Northeast(-1,1),
        //        West(0,-1),                 East(0,1),
        //        Southwest(1,-1), South(1,0),  Southeast(1,1)
        // Note: Often dx refers to column change and dy to row change, but here
        // dx aligns with the first dimension (row) and dy with the second (column).
        // Let's stick to the code's variable names: dx for row change, dy for column change.
        static readonly int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 }; // Change in Row
        static readonly int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 }; // Change in Column


        static void Main(string[] args)
        {
            // --- Test Case 1: Maze with a Path ---
            Console.WriteLine("--- Test Case 1: Path Exists ---");
            // maze: Represents the grid. 2D integer array.
            // 0: Represents an open path the rat can move through.
            // 1: Represents a wall the rat cannot pass.
            int[,] maze1 = new int[,]
            {
                { 1, 1, 1, 1, 1, 1 },
                { 1, 0, 0, 0, 0, 1 }, // Start (1,1) is a '0'
                { 1, 0, 1, 1, 0, 1 },
                { 1, 0, 0, 1, 0, 1 },
                { 1, 1, 0, 0, 0, 1 }, // Exit (4,4) is a '0'
                { 1, 1, 1, 1, 1, 1 }
            };

            int entryX1 = 1, entryY1 = 1; // Starting row and column (inside the border)
            int exitX1 = 4, exitY1 = 4;   // Target row and column (inside the border)

            Console.WriteLine("Maze Layout:");
            PrintMaze(maze1, entryX1, entryY1, exitX1, exitY1);

            // Validate start/end points before solving
            if (!IsValidStartEnd(maze1, entryX1, entryY1, exitX1, exitY1))
            {
                Console.WriteLine("\nInvalid start or end point (e.g., wall or out of bounds).");
            }
            else
            {
                Console.WriteLine($"\nAttempting to find path from ({entryX1},{entryY1}) to ({exitX1},{exitY1})...");
                SolveMaze(maze1, entryX1, entryY1, exitX1, exitY1);
            }

            Console.WriteLine("\n-----------------------------------\n");

            // --- Test Case 2: Maze with No Path ---
            Console.WriteLine("--- Test Case 2: No Path ---");
            int[,] maze2 = new int[,]
            {
                { 1, 1, 1, 1, 1, 1 },
                { 1, 0, 1, 0, 0, 1 }, // Start (1,1)
                { 1, 0, 1, 1, 0, 1 },
                { 1, 0, 0, 1, 0, 1 },
                { 1, 1, 1, 1, 0, 1 }, // Exit (4,4) - Blocked
                { 1, 1, 1, 1, 1, 1 }
            };
            int entryX2 = 1, entryY2 = 1;
            int exitX2 = 4, exitY2 = 4;

            Console.WriteLine("Maze Layout:");
            PrintMaze(maze2, entryX2, entryY2, exitX2, exitY2);

            if (!IsValidStartEnd(maze2, entryX2, entryY2, exitX2, exitY2))
            {
                Console.WriteLine("\nInvalid start or end point (e.g., wall or out of bounds).");
            }
            else
            {
                Console.WriteLine($"\nAttempting to find path from ({entryX2},{entryY2}) to ({exitX2},{exitY2})...");
                SolveMaze(maze2, entryX2, entryY2, exitX2, exitY2);
            }

            Console.WriteLine("\n-----------------------------------\n");

            // Add more test cases as needed:
            // - Start or End on a wall
            // - Start or End outside bounds
            // - Maze with cycles (visited array handles this)
            // - Larger mazes
        }

        // --- Algorithm Implementation: Depth-First Search (DFS) ---

        // SolveMaze: Attempts to find a path from (startX, startY) to (exitX, exitY).
        // Returns: Does not explicitly return bool anymore, prints the result.
        //          (Could be refactored to return List<(int, int)> or null)
        // Method Type: This is like a procedure (void return type in C#).
        static void SolveMaze(int[,] maze, int startX, int startY, int exitX, int exitY)
        {
            int rows = maze.GetLength(0);
            int cols = maze.GetLength(1);

            // visited: A 2D boolean array matching the maze dimensions.
            // Purpose: To keep track of cells that have already been visited
            //          during the current search path exploration.
            // Prevents: Getting stuck in cycles and redundant exploration.
            // Initial State: All false.
            bool[,] visited = new bool[rows, cols];

            // pathStack: A Stack data structure storing tuples of (row, column).
            // Purpose (DFS): Implements the core of DFS. It holds the current path being explored.
            //   - Push: When moving to a valid, unvisited neighbor, push it onto the stack.
            //           This signifies going deeper into the maze.
            //   - Pop: When reaching a dead end (no valid neighbors) or after exploring
            //          all paths from a cell, pop the current cell from the stack.
            //          This signifies backtracking to a previous decision point.
            // Example: If path is (1,1)->(1,2)->(2,2), stack contains [(1,1), (1,2), (2,2)] (top).
            //          If (2,2) is a dead end, Pop() removes (2,2), leaving [(1,1), (1,2)]. We
            //          then explore other neighbors of (1,2).
            Stack<(int, int)> pathStack = new Stack<(int, int)>();

            // predecessors: Dictionary to store the path taken.
            // Key: A cell (coordinate tuple).
            // Value: The cell from which we arrived at the key cell.
            // Purpose: Allows reconstructing the path *backwards* from the exit
            //          once it's found.
            // Example: If we move from (1,1) to (1,2), we store: predecessors[(1,2)] = (1,1).
            Dictionary<(int, int), (int, int)> predecessors = new Dictionary<(int, int), (int, int)>();

            // --- Algorithm Step: Initialization ---
            // 1. Mark the starting cell as visited.
            visited[startX, startY] = true;
            // 2. Push the starting cell onto the stack.
            pathStack.Push((startX, startY));
            // 3. No predecessor for the start cell.

            bool pathFound = false;

            // --- Algorithm Step: Exploration Loop (while stack is not empty) ---
            // The loop continues as long as there are potential path segments to explore.
            while (pathStack.Count > 0)
            {
                // --- Algorithm Step: Get Current Cell ---
                // 4. Get the current cell from the top of the stack (Pop).
                var (x, y) = pathStack.Pop();

                // --- Algorithm Step: Check for Exit ---
                // 5. Check if the current cell is the exit.
                if (x == exitX && y == exitY)
                {
                    pathFound = true;
                    Console.WriteLine("\nA path to the exit has been found!");
                    Console.WriteLine("Path coordinates (Start to Exit):");
                    // --- Algorithm Step: Path Reconstruction ---
                    PrintPath(predecessors, startX, startY, exitX, exitY);
                    break; // Exit the while loop once the path is found
                }

                // --- Algorithm Step: Explore Neighbors ---
                // 6. Explore neighbors in all 8 directions.
                // The dx/dy arrays provide the offsets for each direction.
                for (int i = 0; i < 8; i++) // 8 directions
                {
                    int newX = x + dx[i];
                    int newY = y + dy[i];

                    // --- Algorithm Step: Check Validity ---
                    // 7. Check if the neighbor is a valid move.
                    // IsValidMove encapsulates boundary checks, wall checks, and visited checks.
                    if (IsValidMove(maze, newX, newY, visited))
                    {
                        // --- Algorithm Step: Mark and Add to Stack ---
                        // 8. If valid:
                        //    a. Mark the neighbor as visited.
                        visited[newX, newY] = true;
                        //    b. Record how we got here (for path reconstruction).
                        predecessors[(newX, newY)] = (x, y);
                        //    c. Push the neighbor onto the stack to explore from it later.
                        pathStack.Push((newX, newY));
                    }
                }
                // If no valid neighbors are found for (x,y), the loop continues,
                // effectively backtracking as the next iteration will Pop the
                // previous cell from the stack.
            } // End while loop

            // --- Algorithm Step: No Path Found ---
            // 9. If the loop finishes and the exit wasn't found, no path exists.
            if (!pathFound)
            {
                Console.WriteLine("\nNo path to the exit found.");
            }
        }


        // IsValidStartEnd: Checks if start and end points are within bounds and not walls.
        // Method Type: Function (returns bool).
        static bool IsValidStartEnd(int[,] maze, int startX, int startY, int exitX, int exitY)
        {
            int rows = maze.GetLength(0);
            int cols = maze.GetLength(1);

            bool startValid = startX >= 0 && startX < rows && startY >= 0 && startY < cols && maze[startX, startY] == 0;
            bool endValid = exitX >= 0 && exitX < rows && exitY >= 0 && exitY < cols && maze[exitX, exitY] == 0;

            return startValid && endValid;
        }


        // IsValidMove: Checks if a potential move (x, y) is valid.
        // Conditions:
        // 1. Within maze boundaries (0 <= x < rows, 0 <= y < cols).
        // 2. Not a wall (maze[x, y] == 0).
        // 3. Not already visited in the current search path (!visited[x, y]).
        // Method Type: Function (returns bool).
        static bool IsValidMove(int[,] maze, int x, int y, bool[,] visited)
        {
            int rows = maze.GetLength(0);
            int cols = maze.GetLength(1);

            // Check bounds first to prevent IndexOutOfRangeException
            if (x < 0 || x >= rows || y < 0 || y >= cols)
            {
                return false;
            }

            // Check if it's a path (0) and not visited
            return maze[x, y] == 0 && !visited[x, y];
        }

        // PrintPath: Reconstructs and prints the path using the predecessors map.
        // Method Type: Procedure (void return type).
        static void PrintPath(Dictionary<(int, int), (int, int)> predecessors, int startX, int startY, int exitX, int exitY)
        {
            List<(int, int)> path = new List<(int, int)>();
            var current = (exitX, exitY);

            // Trace back from exit to start
            while (current != (startX, startY))
            {
                path.Add(current);
                // Check if the key exists before accessing it
                if (!predecessors.ContainsKey(current))
                {
                    Console.WriteLine("Error: Path reconstruction failed. Predecessor not found for ({0},{1})", current.Item1, current.Item2);
                    return; // Exit if path is broken
                }
                current = predecessors[current];
            }
            path.Add((startX, startY)); // Add the start cell

            // The path is currently from Exit to Start, so reverse it.
            path.Reverse();

            // Print the reconstructed path
            foreach (var cell in path)
            {
                Console.WriteLine($"({cell.Item1},{cell.Item2})");
            }
        }


        // PrintMaze: Displays the maze layout with Start (S) and Exit (E) markers.
        // Method Type: Procedure (void return type).
        static void PrintMaze(int[,] maze, int startX, int startY, int exitX, int exitY)
        {
            int rows = maze.GetLength(0);
            int cols = maze.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (i == startX && j == startY)
                    {
                        Console.Write("S "); // Mark Start
                    }
                    else if (i == exitX && j == exitY)
                    {
                        Console.Write("E "); // Mark Exit
                    }
                    else
                    {
                        // Print '1' for walls, '0' for open paths
                        Console.Write(maze[i, j] == 1 ? "1 " : "0 "); // Use # for wall, . for path for clarity
                    }
                }
                Console.WriteLine(); // Newline after each row
            }
        }
    }
}