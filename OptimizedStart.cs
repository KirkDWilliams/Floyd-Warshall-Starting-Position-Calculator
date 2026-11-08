using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MapSearch;

public class OptimizedStart
{
    private const int INF = 1_000_000;
    private static readonly string _preloadedMap = "Use pre-loaded map";
    private static readonly string _createMap = "Create new map";

    public static void Main()
    {
        GraphData graph = SelectionScreenV2();

        int[,] distanceGraph = StarterMatrix(graph.Nodes);

        foreach (var edge in graph.Edges)
            AddEdge(distanceGraph, edge.From, edge.To);

        RunFloydWarshall(distanceGraph, graph.Nodes);

        var (Nodes, Score) = FindOptimizedStart(distanceGraph, graph.Nodes, graph.Targets);

        Console.WriteLine($"The best starting nodes are: {string.Join(", ", Nodes)}");
        Console.WriteLine($"Score: {Score:F2}");
        Console.ReadLine();
    }

    private static GraphData SelectionScreen()
    {
        Console.WriteLine("-----------------------------------------------------------------");
        Console.WriteLine("---------- Floyd-Warshall Starting Position Calculator ----------");
        Console.WriteLine("-----------------------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine();
        Thread.Sleep(1000);

        // ask if the user would like to use the nodes in the file or the a manual number of nodes.

        string inputDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Input"));
        string[] options = Directory.GetFiles(inputDirectory);

        for (var file = 0; file < options.Length; file++)
            Console.WriteLine($"[{file}]: {options[file]}");

        var validInput = false;
        var graph = new GraphData();

        while (!validInput)
        {
            var fileInput = Console.ReadLine();

            if (!int.TryParse(fileInput, out int selection) || selection < 0 || selection > options.Length - 1)
            {
                Console.WriteLine($"Invalid input. Please enter a number between 0 and {options.Length-1}.");
                continue;
            }

            string json = File.ReadAllText(options[selection]); 
            graph = JsonSerializer.Deserialize<GraphData>(json)!;
            validInput = true;
        }

        validInput = false;

        Console.WriteLine($"Would you like to use the {graph.Targets} targets or some other number of targets?");
        Console.WriteLine($"[press 'Enter' for {graph.Targets} or type a value between 2 and 6, or 0 to exit]");
        Console.WriteLine();
        Thread.Sleep(1000);
        int failCount = 0;

        while (!validInput || failCount > 3)
        {
            var targetSelection = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(targetSelection))
                break;

            if (!int.TryParse(targetSelection, out int target))
            {
                Console.WriteLine("Invalid input. Please enter a number between 0 and 6.");
                Console.WriteLine();
                failCount++;
                continue;
            }

            if (target <= 0)
            {
                Environment.Exit(0);
            }

            if (target > 6)
            {
                Console.WriteLine("Too large of input. Please enter a number between 0 and 6.");
                Console.WriteLine();
                failCount++;
                continue;
            }

            Console.WriteLine($"-- {target} is excellent.");
            graph.Targets = target;
            Thread.Sleep(1000);
        }

        Console.Clear();

        return graph;
    }

    private static GraphData SelectionScreenV2()
    {
        var graph = new GraphData();

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[bold yellow]Welcome[/]");

        AnsiConsole.Write(new Table()
            .AddColumn("[bold yellow]Floyd-Warshall[/]")
            .AddRow("[blue]Starting Position Calculator[/]"));

        // ask to use a preloaded or a new map search.
        var mapType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How would you like to proceed?")
                .AddChoices(_preloadedMap, _createMap));

        if (string.Equals(_preloadedMap, mapType))
        {
            string inputDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Input"));
            string[] options = Directory.GetFiles(inputDirectory);

            var load = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which loaded map would you like to run?")
                    .AddChoices(options));

            int selection = options.IndexOf(load);

            string json = File.ReadAllText(options[selection]);
            graph = JsonSerializer.Deserialize<GraphData>(json)!;

            var startingNodes = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("How many players would you like to consider?")
                    .DefaultValue($"{graph.Targets}")
                    .AddChoices("1","2","3","4","5","6"));

            graph.Targets = Int16.Parse(startingNodes);

            return graph;
        }
        else if (string.Equals(_createMap, mapType))
        {

        }
        

        return new GraphData();
    }

    private static int[,] StarterMatrix(int n)
    {
        var distanceGraph = new int[n, n];

        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                distanceGraph[i, j] = (i == j)
                    ? 0
                    : INF;

        return distanceGraph;
    }

    private static void AddEdge(int[,] distanceGraph, int n_1, int n_2)
    {
        distanceGraph[n_1, n_2] = 1;
        distanceGraph[n_2, n_1] = 1;
    }

    private static void RunFloydWarshall(int[,] distanceGraph, int nodes)
    {
        for (int k = 0; k < nodes; k++)
            for (int i = 0; i < nodes; i++)
                for (int j = 0; j < nodes; j++)
                    if (distanceGraph[i, k] < INF && distanceGraph[k, j] < INF)
                        distanceGraph[i, j] = Math.Min(distanceGraph[i, j], distanceGraph[i, k] + distanceGraph[k, j]);
    }

    public static (List<int> Nodes, double Score) FindOptimizedStart(int[,] distanceGraph, int nodes, int targets)
    {
        var allNodes = Enumerable.Range(0, nodes).ToList();
        var combinations = GetKCombinations(allNodes, targets);

        List<int> bestNodes = [];
        double bestScore = double.MaxValue;

        foreach (var combination in combinations)
        {
            // Part A: Calculate Inter-Center Separation Variance
            List<int> centerDistances = [];
            for (int i = 0; i < combination.Count; i++)
                for (int j = i + 1; j < combination.Count; j++)
                    centerDistances.Add(distanceGraph[combination[i], combination[j]]);

            double avgCenterDistance = centerDistances.Average();
            double variance = centerDistances.Average(d => Math.Pow(d - avgCenterDistance, 2));

            // Part B: Calculate Average Distance to Nearest Center
            int maxCoverageDistance = 0;
            for (int i = 0; i < nodes; i++) 
            {
                if (combination.Contains(i))
                    continue;

                int minDistanceToCenter = combination.Min(center => distanceGraph[i, center]);
                maxCoverageDistance = Math.Max(maxCoverageDistance, minDistanceToCenter);
            }

            // Fitness Score: Balance of Uniform Spacing with Optimal Coverage of Empty Spaces
            // Lower score is better
            double overallScore = variance + 2 * maxCoverageDistance;

            if (overallScore < bestScore)
            {
                bestScore = overallScore;
                bestNodes = combination;
            }
        }

        return (bestNodes, bestScore);
    }

    static List<List<int>> GetKCombinations(List<int> list, int length)
    {
        if (length == 1) 
            return list.Select(item => new List<int> { item }).ToList();

        List<List<int>> result = [];

        for (int i = 0; i <= list.Count - length; i++)
        {
            var subList = list.Skip(i + 1).ToList();
            var subCombinations = GetKCombinations(subList, length - 1);
            foreach (var subCombination in subCombinations)
            {
                subCombination.Insert(0, list[i]);
                result.Add(subCombination);
            }
        }

        return result;
    }

    public class GraphData
    {
        [JsonPropertyName("nodes")] public int Nodes { get; set; } = 0;
        [JsonPropertyName("edges")] public List<Edge> Edges { get; set; } = [];
        [JsonPropertyName("targets")] public int Targets { get; set; } = 0;
    }

    public class Edge
    {
        [JsonPropertyName("from")] public int From { get; set; }
        [JsonPropertyName("to")] public int To { get; set; }
        [JsonPropertyName("weight")] public int Weight { get; set; } = 1;
    }
}
