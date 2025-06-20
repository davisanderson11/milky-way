using MilkyWay.Core;

namespace MilkyWay.Legacy
{
    public static class ChunkVisualizerConsole
    {
        public static void Run(ChunkBasedGalaxySystem chunkSystem)
        {
            Console.WriteLine("\n=== Chunk Visualizer ===");
            Console.WriteLine("Generate images showing stars in a specific chunk");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  260_0_0    = Solar neighborhood chunk");
            Console.WriteLine("  0_0_0      = Galactic center");
            Console.WriteLine("  100_0_0    = 10,000 ly from center");

            Console.Write("\nEnter chunk ID to visualize: ");
            var chunkId = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(chunkId))
            {
                Console.WriteLine("Invalid chunk ID");
                return;
            }

            try
            {
                Console.Write("\nImage size (512-4096, default 1024): ");
                var sizeInput = Console.ReadLine();
                int imageSize = 1024;

                if (!string.IsNullOrWhiteSpace(sizeInput) && int.TryParse(sizeInput, out var size))
                {
                    imageSize = Math.Max(512, Math.Min(4096, size));
                }

                var visualizer = new ChunkVisualizer(imageSize);
                visualizer.VisualizeChunk(chunkId);

                Console.WriteLine("\n✓ Images generated successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
