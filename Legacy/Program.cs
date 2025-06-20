using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MilkyWay.Utils;
using MilkyWay.Core;
using MilkyWay.Legacy;

/// <summary>
/// Console-only version of the Scientific Milky Way Generator
/// No Windows Forms dependencies - works with standard .NET SDK
/// </summary>
public class ScientificMilkyWayConsole
{
    static void Main(string[] args)
    {
        var chunkBasedSystem = new ChunkBasedGalaxySystem();
        
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Scientific Milky Way Galaxy Generator ===");
            Console.WriteLine("Mode: CHUNK-BASED SYSTEM");
            Console.WriteLine("Based on latest astronomical research (2024)");
            Console.WriteLine();
            Console.WriteLine("1. Find star by seed");
            Console.WriteLine("2. Investigate galaxy chunk");
            Console.WriteLine("3. Visualize chunk (generate images)");
            Console.WriteLine("4. Estimate total galaxy star count");
            Console.WriteLine("5. Generate density heatmaps");
            Console.WriteLine("6. Exit");
            Console.WriteLine();
            Console.Write("Select option: ");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    FindStarBySeedChunkBased(chunkBasedSystem);
                    break;
                case "2":
                    InvestigateChunkNew(chunkBasedSystem);
                    break;
                case "3":
                    VisualizeChunk(chunkBasedSystem);
                    break;
                case "4":
                    chunkBasedSystem.EstimateTotalStarCount();
                    break;
                case "5":
                    GenerateDensityHeatmaps();
                    break;
                case "6":
                    return;
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    } 
}