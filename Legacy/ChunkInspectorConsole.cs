using MilkyWay.Core;

namespace MilkyWay.Legacy
{
    public static class ChunkInspectorConsole
    {
        public static void Run(ChunkBasedGalaxySystem chunkSystem)
        {
            Console.WriteLine("\n=== Galaxy Chunk Investigator (NEW FAST VERSION) ===");
            Console.WriteLine("Chunks use cylindrical coordinates: r_theta_z");
            Console.WriteLine("This new system generates chunks INSTANTLY!");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  260_0_0    = Solar neighborhood chunk");
            Console.WriteLine("  0_0_0      = Galactic center");

            while (true)
            {
                Console.Write("\nEnter chunk ID (or 'q' to quit): ");
                var input = Console.ReadLine();

                if (input?.ToLower() == "q") break;

                try
                {
                    Console.Write("Include rogue planets? (y/N): ");
                    var includeRogues = Console.ReadLine()?.ToLower() == "y";

                    var startTime = DateTime.Now;
                    chunkSystem.InvestigateChunk(input!, includeRoguePlanets: includeRogues);
                    var elapsed = (DateTime.Now - startTime).TotalSeconds;
                    Console.WriteLine($"\nTotal time: {elapsed:F2}s");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
