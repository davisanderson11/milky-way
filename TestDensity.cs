using System;

namespace MilkyWay
{
    class TestDensity
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Milky Way Density Calculation Test ===\n");
            
            // Test positions in light years
            double[] testDistances = { 8000, 26000, 50000 };
            
            // For each test position, we'll test at z=0 (in the disk plane)
            double z = 0;
            
            foreach (double r in testDistances)
            {
                Console.WriteLine($"Testing at distance r = {r:N0} ly from center (z = {z} ly):");
                Console.WriteLine("------------------------------------------------");
                
                // Calculate normalized density (0-1 range)
                double normalizedDensity = GalaxyDensity.CalculateTotalDensity(r, z);
                Console.WriteLine($"Normalized density: {normalizedDensity:E6}");
                
                // Get the expected star density with scaling applied
                double stellarDensity = GalaxyDensity.GetExpectedStarDensity(r, z);
                Console.WriteLine($"Final stellar density: {stellarDensity:E6} stars/ly³");
                
                // Calculate the scale factor being applied
                double scaleFactor = stellarDensity / normalizedDensity;
                Console.WriteLine($"Scale factor: {scaleFactor:E6}");
                
                // Let's also break down the individual components
                Console.WriteLine("\nComponent breakdown:");
                
                // Bulge contribution
                double bulgeDensity = GalaxyDensity.BulgeDensity(r, z);
                Console.WriteLine($"  Bulge component: {bulgeDensity:E6}");
                
                // Disk contribution
                double diskDensity = GalaxyDensity.DiskDensity(r, z);
                Console.WriteLine($"  Disk component: {diskDensity:E6}");
                
                // Halo contribution
                double haloDensity = GalaxyDensity.HaloDensity(r, z);
                Console.WriteLine($"  Halo component: {haloDensity:E6}");
                
                // Spiral arm contribution (if applicable)
                double spiralModulation = 1.0;
                if (r >= 10000 && r <= 50000)
                {
                    // Calculate angle (assuming 0 for simplicity)
                    double angle = 0;
                    spiralModulation = GalaxyDensity.SpiralArmModulation(r, angle);
                }
                Console.WriteLine($"  Spiral modulation factor: {spiralModulation:F3}");
                
                Console.WriteLine($"\nSum of components: {bulgeDensity + diskDensity + haloDensity:E6}");
                Console.WriteLine($"With spiral modulation: {(bulgeDensity + diskDensity + haloDensity) * spiralModulation:E6}");
                
                // Test at different z heights for the same r
                Console.WriteLine($"\nDensity variation with height at r = {r:N0} ly:");
                double[] zHeights = { 0, 100, 500, 1000, 2000 };
                foreach (double zTest in zHeights)
                {
                    double densityAtZ = GalaxyDensity.GetExpectedStarDensity(r, zTest);
                    Console.WriteLine($"  z = {zTest,5} ly: {densityAtZ:E6} stars/ly³");
                }
                
                Console.WriteLine("\n");
            }
            
            // Additional test: Check density profile along the disk
            Console.WriteLine("Density profile along the galactic disk (z = 0):");
            Console.WriteLine("------------------------------------------------");
            double[] radialPositions = { 0, 1000, 5000, 8000, 15000, 26000, 35000, 50000, 75000, 100000 };
            foreach (double rTest in radialPositions)
            {
                double density = GalaxyDensity.GetExpectedStarDensity(rTest, 0);
                Console.WriteLine($"r = {rTest,6} ly: {density:E6} stars/ly³");
            }
        }
    }
}