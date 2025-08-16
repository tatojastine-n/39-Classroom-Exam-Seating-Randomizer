using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SeatingArrangement
{
    private Random _random;
    private int _rows;
    private int _cols;
    private List<Student> _students;
    private Dictionary<string, List<string>> _friendships;
    private Dictionary<string, List<string>> _flaggedPairs;
    private Student[,] _seatingGrid;
    private int _maxAttempts = 1000;

    public SeatingArrangement(int rows, int cols, List<Student> students,
        Dictionary<string, List<string>> friendships,
        Dictionary<string, List<string>> flaggedPairs,
        int? seed = null)
    {
        _rows = rows;
        _cols = cols;
        _students = students;
        _friendships = friendships;
        _flaggedPairs = flaggedPairs;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _seatingGrid = new Student[rows, cols];
    }

    public bool GenerateValidArrangement()
    {
        for (int attempt = 0; attempt < _maxAttempts; attempt++)
        {
            ShuffleStudents();
            if (TryPlaceStudents())
            {
                Console.WriteLine($"Found valid arrangement on attempt {attempt + 1}");
                Console.WriteLine($"Random seed that produces this arrangement: {_random.Next()}");
                return true;
            }
        }
        return false;
    }

    private void ShuffleStudents()
    {
        for (int i = _students.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            var temp = _students[i];
            _students[i] = _students[j];
            _students[j] = temp;
        }
    }

    private bool TryPlaceStudents()
    {
        _seatingGrid = new Student[_rows, _cols];
        var remainingStudents = new Queue<Student>(_students);

        var studentConstraints = CalculateConstraintWeights();
        foreach (var student in studentConstraints.OrderByDescending(s => s.Value))
        {
            if (!TryFindSeatForStudent(student.Key))
                return false;
            remainingStudents = new Queue<Student>(remainingStudents.Where(s => s != student.Key));
        }

        while (remainingStudents.Count > 0)
        {
            var student = remainingStudents.Dequeue();
            if (!TryFindSeatForStudent(student))
                return false;
        }

        return true;
    }

    private Dictionary<Student, int> CalculateConstraintWeights()
    {
        var weights = new Dictionary<Student, int>();
        foreach (var student in _students)
        {
            int weight = 0;
            if (_friendships.ContainsKey(student.Name))
                weight += _friendships[student.Name].Count;
            if (_flaggedPairs.ContainsKey(student.Name))
                weight += _flaggedPairs[student.Name].Count;
            weights[student] = weight;
        }
        return weights;
    }

    private bool TryFindSeatForStudent(Student student)
    {
        if (_friendships.ContainsKey(student.Name))
        {
            foreach (var friend in _friendships[student.Name])
            {
                var friendPosition = FindStudentPosition(friend);
                if (friendPosition != (-1, -1))
                {
                    var (row, col) = friendPosition;
                    foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                    {
                        int newRow = row + dr;
                        int newCol = col + dc;
                        if (IsValidPosition(newRow, newCol) && _seatingGrid[newRow, newCol] == null)
                        {
                            if (!HasFlaggedConflict(student, newRow, newCol))
                            {
                                _seatingGrid[newRow, newCol] = student;
                                return true;
                            }
                        }
                    }
                }
            }
        }

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                if (_seatingGrid[row, col] == null)
                {
                    if (!HasFlaggedConflict(student, row, col))
                    {
                        _seatingGrid[row, col] = student;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private (int row, int col) FindStudentPosition(string name)
    {
        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                if (_seatingGrid[row, col]?.Name == name)
                    return (row, col);
            }
        }
        return (-1, -1);
    }

    private bool IsValidPosition(int row, int col)
    {
        return row >= 0 && row < _rows && col >= 0 && col < _cols;
    }

    private bool HasFlaggedConflict(Student student, int row, int col)
    {
        if (!_flaggedPairs.ContainsKey(student.Name))
            return false;

        foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
        {
            int newRow = row + dr;
            int newCol = col + dc;
            if (IsValidPosition(newRow, newCol) && _seatingGrid[newRow, newCol] != null)
            {
                if (_flaggedPairs[student.Name].Contains(_seatingGrid[newRow, newCol].Name))
                    return true;
            }
        }
        return false;
    }

    public void PrintSeatingArrangement()
    {
        Console.WriteLine("\nSeating Arrangement:");
        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                var student = _seatingGrid[row, col];
                Console.Write(student != null ? $"{student.Name,-10}" : $"{$"Empty {row},{col}",-10}");
            }
            Console.WriteLine();
        }
    }

    public void PrintViolations()
    {
        int flaggedViolations = 0;
        int friendViolations = 0;

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                var current = _seatingGrid[row, col];
                if (current == null) continue;

                foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                {
                    int newRow = row + dr;
                    int newCol = col + dc;
                    if (IsValidPosition(newRow, newCol) && _seatingGrid[newRow, newCol] != null)
                    {
                        var neighbor = _seatingGrid[newRow, newCol].Name;

                        if (_flaggedPairs.ContainsKey(current.Name) &&
                            _flaggedPairs[current.Name].Contains(neighbor))
                        {
                            Console.WriteLine($"VIOLATION: {current.Name} should not be near {neighbor}");
                            flaggedViolations++;
                        }
                    }
                }
            }
        }

        foreach (var student in _students)
        {
            if (_friendships.ContainsKey(student.Name))
            {
                bool hasNearbyFriend = false;
                var pos = FindStudentPosition(student.Name);

                foreach (var friend in _friendships[student.Name])
                {
                    var friendPos = FindStudentPosition(friend);
                    if (friendPos == (-1, -1)) continue;

                    if ((Math.Abs(pos.row - friendPos.row) == 1 && pos.col == friendPos.col) ||
                        (Math.Abs(pos.col - friendPos.col) == 1 && pos.row == friendPos.row))
                    {
                        hasNearbyFriend = true;
                        break;
                    }
                }

                if (!hasNearbyFriend && _friendships[student.Name].Count > 0)
                {
                    friendViolations++;
                }
            }
        }

        Console.WriteLine($"\nTotal flagged violations: {flaggedViolations}");
        Console.WriteLine($"Total friend separation violations: {friendViolations}");
    }
}

public class Student
{
    public string Name { get; }

    public Student(string name)
    {
        Name = name;
    }
}
namespace Classroom_Exam_Seating_Randomizer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var students = new List<Student>
        {
            new Student("Alice"),
            new Student("Bob"),
            new Student("Charlie"),
            new Student("Dave"),
            new Student("Eve"),
            new Student("Frank"),
            new Student("Grace"),
            new Student("Henry")
        };

            var friendships = new Dictionary<string, List<string>>
            {
                ["Alice"] = new List<string> { "Bob", "Charlie" },
                ["Bob"] = new List<string> { "Alice", "Dave" },
                ["Charlie"] = new List<string> { "Alice", "Eve" },
                ["Dave"] = new List<string> { "Bob" }
            };

            var flaggedPairs = new Dictionary<string, List<string>>
            {
                ["Alice"] = new List<string> { "Eve" },
                ["Bob"] = new List<string> { "Frank" },
                ["Eve"] = new List<string> { "Alice", "Grace" },
                ["Grace"] = new List<string> { "Henry" }
            };

            int rows = 3;
            int cols = 3;

            Console.WriteLine("Generating seating arrangement...");

            int? seed = null;

            var arrangement = new SeatingArrangement(rows, cols, students, friendships, flaggedPairs, seed);

            if (arrangement.GenerateValidArrangement())
            {
                arrangement.PrintSeatingArrangement();
                arrangement.PrintViolations();
            }
            else
            {
                Console.WriteLine("Unable to find a valid arrangement after multiple attempts. Try relaxing some constraints.");
            }
        }
    }
}
