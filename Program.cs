using MapSearch;
using Spectre.Console;

AnsiConsole.Status()
    .Spinner(Spinner.Known.SimpleDotsScrolling)
       .Start("Loading...", ctx =>
       {
           Thread.Sleep(2000);
       });

Console.Clear();            
Thread.Sleep(1000);
OptimizedStart.Main();