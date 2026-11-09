using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MapSearch;

public class OptimizedStart
{
    private const int INF = 1_000_000;
    private const string _preloadedMap = "Use pre-loaded map";
    private const string _createMap = "Create new map";

    public static void Main()
    {
        List<GraphData> graphs = SelectionScreenV2();

        int[,] distanceGraph = StarterMatrix(graphs[0].Nodes);

        foreach (var edge in graphs[0].Edges)
            AddEdge(distanceGraph, edge.From, edge.To);

        RunFloydWarshall(distanceGraph, graphs[0].Nodes);

        List<(List<int> Nodes, double Score)> outcomes = [];

        var table = new Table()
            .AddColumn("# of Targets")
            .AddColumn("Score");

        AnsiConsole.Live(table)
            .Start(ctx =>
            {
                Thread.Sleep(1000);
                for (var idx = 0; idx < graphs.Count; idx++)
                {
                    outcomes.Add(
                        FindOptimizedStart(
                            distanceGraph, graphs[idx].Nodes, graphs[idx].Targets));

                    table.AddRow($"{graphs[idx].Targets}", $"{outcomes[idx].Score}");
                    ctx.Refresh();
                    Thread.Sleep(500);
                }
            });

        //TODO: add a return to beginning
        //      or a save results off to a file
        Console.ReadLine();
    }

    private static List<GraphData> SelectionScreenV2()
    {
        AnsiConsole.Status()
            .Start("[bold yellow]Welcome[/]", ctx =>
            {
                ctx.Status("[green]Loading [italic] local data [/][/]...");
                Thread.Sleep(1500);
            });

        AnsiConsole.Write(new Table()
            .AddColumn("[bold yellow]Floyd-Warshall[/]").DoubleBorder()
            .AddRow("[blue]Starting Position Calculator[/]"));

        AnsiConsole.MarkupLine("");
        Thread.Sleep(1500);

        var mapType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How would you like to proceed?")
                .AddChoices(
                    _preloadedMap, 
                    _createMap));

        List<GraphData> graphTargetOptions = [];
        var graph = new GraphData();

        switch (mapType)
        {
            case _preloadedMap:
                string inputDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Input"));
                string[] options = Directory.GetFiles(inputDirectory);

                var load = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Which loaded map would you like to run?")
                        .AddChoices(options));

                int selection = options.IndexOf(load);

                string json = File.ReadAllText(options[selection]);
                graph = JsonSerializer.Deserialize<GraphData>(json)!;

                var targets = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("How many players would you like to consider?")
                        .DefaultValue($"{graph.Targets}")
                        .AddChoices("1", "2", "3", "4", "5", "6"));

                foreach (var target in targets)
                {
                    GraphData targetGraph = new()
                    {
                        Edges = graph.Edges,
                        Nodes = graph.Nodes,
                        Targets = Int16.Parse(target)
                    };

                    graphTargetOptions.Add(targetGraph);
                }

                return graphTargetOptions;

            case _createMap:

                break;
        }      

        return new List<GraphData>();
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
