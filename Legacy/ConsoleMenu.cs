using MilkyWay.Core;
using MilkyWay.Legacy;

namespace MilkyWay.Legacy
{
  public class ConsoleMenu
  {
    static void Main(string[] args)
    {
      var chunkSystem = new ChunkBasedGalaxySystem();

      while (true)
      {
        Console.Clear();
        Console.WriteLine("1. Find star by seed");
        Console.WriteLine("2. Investigate galaxy chunk");
        Console.WriteLine("3. Visualize chunk");
        Console.WriteLine("4. Estimate total galaxy star count");
        Console.WriteLine("5. Generate density heatmaps");
        Console.WriteLine("6. Exit");
        var choice = Console.ReadLine();

        switch (choice)
        {
          case "1": StarFinderConsole.Run(chunkSystem);        break;
          case "2": ChunkInspectorConsole.Run(chunkSystem);    break;
          case "3": ChunkVisualizerConsole.Run(chunkSystem);   break;
          case "4": chunkSystem.EstimateTotalStarCount();      break;
          case "5": HeatmapConsole.Run();                      break;
          case "6": return;
        }

        Console.WriteLine("\nPress any key to continue…");
        Console.ReadKey();
      }
    }
  }
}
