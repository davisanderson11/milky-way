using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Console-only version of the Scientific Milky Way Generator
/// No Windows Forms dependencies - works with standard .NET SDK
/// </summary>
public class ScientificMilkyWayConsole
{
    static void Main(string[] args)
    {
        var generator = new ScientificMilkyWayGenerator();
        var chunkBasedSystem = new ChunkBasedGalaxySystem();
        
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Scientific Milky Way Galaxy Generator ===");
            Console.WriteLine("Mode: CHUNK-BASED SYSTEM");
            Console.WriteLine("Based on latest astronomical research (2024)");
            Console.WriteLine();
            Console.WriteLine("1. Export stars for Unity (JSON)");
            Console.WriteLine("2. Find star by seed");
            // Option 3 removed (was galaxy statistics)
            Console.WriteLine("4. Generate galaxy images (point cloud)");
            Console.WriteLine("5. Generate density heatmaps (pure formulas)");
            Console.WriteLine("6. Investigate galaxy chunk");
            Console.WriteLine("7. Visualize chunk (generate images)");
            Console.WriteLine("8. Estimate total galaxy star count");
            // Option 9 removed (was sky view)
            Console.WriteLine("10. Exit");
            Console.WriteLine();
            Console.Write("Select option: ");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    ExportForUnity(generator);
                    break;
                case "2":
                    FindStarBySeedChunkBased(chunkBasedSystem, generator);
                    break;
                case "3":
                    Console.WriteLine("This option has been removed.");
                    break;
                case "4":
                    GenerateGalaxyImages(generator);
                    break;
                case "5":
                    GenerateDensityHeatmaps(generator);
                    break;
                case "6":
                    InvestigateChunkNew(chunkBasedSystem);
                    break;
                case "7":
                    VisualizeChunk(chunkBasedSystem);
                    break;
                case "8":
                    chunkBasedSystem.EstimateTotalStarCount();
                    break;
                case "9":
                    Console.WriteLine("This option has been removed.");
                    break;
                case "10":
                    return;
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
    
    static void ExportForUnity(ScientificMilkyWayGenerator generator)
    {
        Console.WriteLine("\n=== Export for Unity ===");
        Console.Write("Number of stars (1000-1000000): ");
        
        if (!int.TryParse(Console.ReadLine(), out int count) || count < 1000 || count > 1000000)
        {
            count = 100000;
            Console.WriteLine($"Using default: {count}");
        }
        
        Console.WriteLine($"\nGenerating {count:N0} stars...");
        var stars = generator.GenerateStars(count);
        
        var filename = "MilkyWay_Unity_Export.json";
        using (var writer = new StreamWriter(filename))
        {
            writer.WriteLine("{");
            writer.WriteLine($"  \"generatedAt\": \"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\",");
            writer.WriteLine($"  \"starCount\": {stars.Count},");
            writer.WriteLine("  \"stars\": [");
            
            for (int i = 0; i < stars.Count; i++)
            {
                var star = stars[i];
                writer.Write("    {");
                writer.Write($"\"seed\":{star.Seed},");
                writer.Write($"\"position\":[{star.Position.X:F2},{star.Position.Y:F2},{star.Position.Z:F2}],");
                writer.Write($"\"type\":\"{star.Type}\",");
                writer.Write($"\"mass\":{star.Mass:F3},");
                writer.Write($"\"temperature\":{star.Temperature:F0},");
                writer.Write($"\"luminosity\":{star.Luminosity:F4},");
                writer.Write($"\"color\":[{star.Color.X:F3},{star.Color.Y:F3},{star.Color.Z:F3}],");
                writer.Write($"\"population\":\"{star.Population}\",");
                writer.Write($"\"planets\":{star.PlanetCount}");
                writer.Write("}");
                
                if (i < stars.Count - 1) writer.WriteLine(",");
                else writer.WriteLine();
            }
            
            writer.WriteLine("  ]");
            writer.WriteLine("}");
        }
        
        Console.WriteLine($"✓ Exported {stars.Count:N0} stars to {filename}");
    }
    
    
    static void GenerateGalaxyImages(ScientificMilkyWayGenerator generator)
    {
        Console.WriteLine("\n=== Generate Galaxy Images ===");
        Console.Write("Number of stars (100000-10000000, default 500000): ");
        if (!int.TryParse(Console.ReadLine(), out int count) || count < 100000 || count > 10000000)
        {
            count = 500000;
            Console.WriteLine($"Using default: {count:N0}");
        }
        
        var visualizer = new ScientificGalaxyVisualizer2(generator);
        visualizer.GenerateAllViews(2048, 2048, count);
        
        Console.WriteLine("\nImages have been saved to the current directory!");
    }
    
    static void GenerateDensityHeatmaps(ScientificMilkyWayGenerator generator)
    {
        Console.WriteLine("\n=== Generate Density Heatmaps ===");
        Console.WriteLine("This will create pure mathematical visualizations of galaxy density");
        Console.WriteLine("using only the formulas from GalaxyGenerator - no star sampling!");
        Console.WriteLine();
        Console.Write("Image resolution (512-4096, default 2048): ");
        
        int resolution = 2048;
        var input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int res))
        {
            resolution = Math.Max(512, Math.Min(4096, res));
        }
        
        Console.Write("\nVertical scale for side view (1-10, default 5): ");
        float verticalScale = 5.0f;
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && float.TryParse(input, out float vScale))
        {
            verticalScale = Math.Max(1.0f, Math.Min(10.0f, vScale));
        }
        
        Console.WriteLine($"\nGenerating {resolution}x{resolution} density heatmaps with {verticalScale}x vertical scale...");
        
        var visualizer = new ScientificGalaxyVisualizer2(generator);
        visualizer.GenerateDensityHeatmaps(resolution, resolution, verticalScale);
        
        Console.WriteLine("\nHeatmap images have been saved to the current directory!");
    }
    
    static void FindStarBySeedChunkBased(ChunkBasedGalaxySystem chunkSystem, ScientificMilkyWayGenerator generator)
    {
        Console.WriteLine("\n=== Star Finder (Chunk-Based System) ===");
        Console.WriteLine("Seeds now encode: ChunkR_ChunkTheta_ChunkZ_StarIndex");
        Console.WriteLine("Example: Star at chunk 260_45_0, index 100");
        Console.WriteLine("You can enter either:");
        Console.WriteLine("  1. A chunk-based seed (e.g., 12345678)");
        Console.WriteLine("  2. Chunk coordinates: r_theta_z_index (e.g., 260_45_0_100)");
        Console.WriteLine("\nFor stellar system objects, you can use either format:");
        Console.WriteLine("  Underscore format: 260_45_0_100_A or 260_45_0_100_1_a");
        Console.WriteLine("  Dash format: 12345678-A or 12345678-1-a");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  Companion stars: _A, _B or -A, -B");
        Console.WriteLine("  Planets: _1, _2 or -1, -2");
        Console.WriteLine("  Moons: _1_a, _2_b or -1-a, -2-b");
        
        var unifiedGen = new UnifiedSystemGenerator();
        
        while (true)
        {
            Console.Write("\nEnter seed or chunk coordinates (or 'q' to quit): ");
            var input = Console.ReadLine();
            
            if (input?.ToLower() == "q") break;
            
            try
            {
                ScientificMilkyWayGenerator.Star star;
                long starSeed = 0;
                bool isCompanion = false;
                bool isPlanet = false;
                bool isMoon = false;
                string companionLetter = "";
                int planetIndex = 0;
                string moonLetter = "";
                
                // Parse input to extract star, companion, planet, and moon information
                
                // Check if it's dash format (e.g., 12345678-A-1-a)
                if (input != null && input.Contains('-') && !input.Contains('_'))
                {
                    var parts = input.Split('-');
                    if (parts.Length >= 1 && long.TryParse(parts[0], out starSeed))
                    {
                        var (r, theta, z, index) = ChunkBasedGalaxySystem.DecodeSeed(starSeed);
                        Console.WriteLine($"Decoded to chunk {r}_{theta}_{z}, star index {index}");
                        
                        // Parse additional parts
                        if (parts.Length > 1)
                        {
                            // Check if it's a companion star (single letter)
                            if (parts[1].Length == 1 && char.IsLetter(parts[1][0]) && char.IsUpper(parts[1][0]))
                            {
                                isCompanion = true;
                                companionLetter = parts[1];
                                
                                // Check for planet of companion
                                if (parts.Length > 2 && int.TryParse(parts[2], out planetIndex))
                                {
                                    isPlanet = true;
                                    
                                    // Check for moon of planet of companion
                                    if (parts.Length > 3)
                                    {
                                        isMoon = true;
                                        moonLetter = parts[3];
                                    }
                                }
                            }
                            // Check if it's a planet (number)
                            else if (int.TryParse(parts[1], out planetIndex))
                            {
                                isPlanet = true;
                                
                                // Check for moon
                                if (parts.Length > 2)
                                {
                                    isMoon = true;
                                    moonLetter = parts[2];
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid dash format. Use SEED-COMPANION-PLANET-MOON");
                        continue;
                    }
                }
                // Check if it's underscore format (e.g., 260_45_0_100_A)
                else if (input != null && input.Contains('_'))
                {
                    var parts = input.Split('_');
                    
                    // Basic star format: r_theta_z_index
                    if (parts.Length >= 4)
                    {
                        int r = int.Parse(parts[0]);
                        int theta = int.Parse(parts[1]);
                        int z = int.Parse(parts[2]);
                        int index = int.Parse(parts[3]);
                        
                        starSeed = ChunkBasedGalaxySystem.EncodeSeed(r, theta, z, index);
                        Console.WriteLine($"Encoded seed: {starSeed}");
                        
                        // Check for additional parts (companion, planet, moon)
                        if (parts.Length > 4)
                        {
                            // Check if it's a companion star (single letter)
                            if (parts[4].Length == 1 && char.IsLetter(parts[4][0]) && char.IsUpper(parts[4][0]))
                            {
                                isCompanion = true;
                                companionLetter = parts[4];
                                
                                // Check for planet of companion
                                if (parts.Length > 5 && int.TryParse(parts[5], out planetIndex))
                                {
                                    isPlanet = true;
                                    
                                    // Check for moon of planet of companion
                                    if (parts.Length > 6)
                                    {
                                        isMoon = true;
                                        moonLetter = parts[6];
                                    }
                                }
                            }
                            // Check if it's a planet (number)
                            else if (int.TryParse(parts[4], out planetIndex))
                            {
                                isPlanet = true;
                                
                                // Check for moon
                                if (parts.Length > 5)
                                {
                                    isMoon = true;
                                    moonLetter = parts[5];
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid format. Use r_theta_z_index");
                        continue;
                    }
                }
                else if (long.TryParse(input, out starSeed))
                {
                    var (r, theta, z, index) = ChunkBasedGalaxySystem.DecodeSeed(starSeed);
                    Console.WriteLine($"Decoded to chunk {r}_{theta}_{z}, star index {index}");
                }
                else
                {
                    Console.WriteLine("Invalid input");
                    continue;
                }
                
                // Get the star
                star = chunkSystem.GetStarBySeed(starSeed);
                
                // Generate the unified system once
                var systemRoot = unifiedGen.GenerateSystem(star.Seed, star.Type, star.Mass, 
                    star.Temperature, star.Luminosity, star.Seed.ToString());
                
                // Handle companion star query
                if (isCompanion)
                {
                    // Find the companion star
                    UnifiedSystemGenerator.Star? companionStar = null;
                    
                    // Check direct children
                    companionStar = systemRoot.Children
                        .OfType<UnifiedSystemGenerator.Star>()
                        .FirstOrDefault(s => s.Name.EndsWith($" {companionLetter}"));
                    
                    // Check binary companion
                    if (companionStar == null && systemRoot.BinaryCompanion is UnifiedSystemGenerator.Star bs && 
                        bs.Name.EndsWith($" {companionLetter}"))
                    {
                        companionStar = bs;
                    }
                    
                    if (companionStar == null)
                    {
                        Console.WriteLine($"\nError: Companion star {companionLetter} not found in system.");
                        continue;
                    }
                    
                    Console.WriteLine($"\nCompanion Star Details:");
                    Console.WriteLine($"  Name: {companionStar.Name}");
                    Console.WriteLine($"  Type: {companionStar.StellarType}");
                    Console.WriteLine($"  Mass: {companionStar.Mass:F3} solar masses");
                    Console.WriteLine($"  Temperature: {companionStar.Temperature:F0} K");
                    Console.WriteLine($"  Separation: {companionStar.OrbitalDistance:F2} AU");
                    
                    if (isPlanet)
                    {
                        var planet = companionStar.Children
                            .OfType<UnifiedSystemGenerator.Planet>()
                            .FirstOrDefault(p => p.Name.EndsWith($" {planetIndex}"));
                        
                        if (planet != null)
                        {
                            Console.WriteLine($"\n  Planet {planetIndex} of companion star {companionLetter}:");
                            Console.WriteLine($"    Type: {planet.PlanetType}");
                            Console.WriteLine($"    Mass: {planet.Mass:F2} Earth masses");
                            Console.WriteLine($"    Orbital Distance: {planet.OrbitalDistance:F2} AU");
                            Console.WriteLine($"    Moons: {planet.Children.Count}");
                            
                            if (isMoon)
                            {
                                var moon = planet.Children
                                    .OfType<UnifiedSystemGenerator.Moon>()
                                    .FirstOrDefault(m => m.Name.EndsWith($" {moonLetter}"));
                                
                                if (moon != null)
                                {
                                    Console.WriteLine($"\n  Moon {moonLetter} of planet {planetIndex}:");
                                    Console.WriteLine($"    Type: {moon.Composition}");
                                    Console.WriteLine($"    Mass: {moon.Mass:F4} Earth masses");
                                }
                                else
                                {
                                    Console.WriteLine($"  Moon {moonLetter} not found");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"  Planet {planetIndex} not found for companion {companionLetter}");
                        }
                    }
                }
                // Handle planet query
                else if (isPlanet)
                {
                    // Find planet in the system
                    UnifiedSystemGenerator.Planet? planet = null;
                    
                    // Search in all stars' children
                    var allStars = new List<UnifiedSystemGenerator.Star> { systemRoot };
                    allStars.AddRange(systemRoot.Children.OfType<UnifiedSystemGenerator.Star>());
                    if (systemRoot.BinaryCompanion is UnifiedSystemGenerator.Star bs2)
                        allStars.Add(bs2);
                    
                    foreach (var s in allStars)
                    {
                        planet = s.Children
                            .OfType<UnifiedSystemGenerator.Planet>()
                            .FirstOrDefault(p => p.Name.EndsWith($" {planetIndex}"));
                        if (planet != null) break;
                    }
                    
                    if (planet != null)
                    {
                        Console.WriteLine($"\nPlanet Details:");
                        Console.WriteLine($"  Name: {planet.Name}");
                        Console.WriteLine($"  Type: {planet.PlanetType}");
                        Console.WriteLine($"  Mass: {planet.Mass:F2} Earth masses");
                        Console.WriteLine($"  Orbital Distance: {planet.OrbitalDistance:F2} AU");
                        Console.WriteLine($"  Moons: {planet.Children.Count}");
                        
                        if (isMoon)
                        {
                            var moon = planet.Children
                                .OfType<UnifiedSystemGenerator.Moon>()
                                .FirstOrDefault(m => m.Name.EndsWith($" {moonLetter}"));
                                
                            if (moon != null)
                            {
                                Console.WriteLine($"\nMoon Details:");
                                Console.WriteLine($"  Name: {moon.Name}");
                                Console.WriteLine($"  Type: {moon.Composition}");
                                Console.WriteLine($"  Mass: {moon.Mass:F4} Earth masses");
                            }
                            else
                            {
                                Console.WriteLine($"  Moon {moonLetter} not found");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Planet {planetIndex} not found in system.");
                    }
                }
                // Handle basic star query
                else
                {
                    Console.WriteLine($"\nStar Details:");
                    Console.WriteLine($"  Seed: {star.Seed}");
                    Console.WriteLine($"  Type: {star.Type}");
                    Console.WriteLine($"  Position: ({star.Position.X:F1}, {star.Position.Y:F1}, {star.Position.Z:F1}) ly");
                    Console.WriteLine($"  Distance from center: {Math.Sqrt(star.Position.X * star.Position.X + star.Position.Y * star.Position.Y + star.Position.Z * star.Position.Z):F1} ly");
                    Console.WriteLine($"  Mass: {star.Mass:F3} solar masses");
                    Console.WriteLine($"  Temperature: {star.Temperature:F0} K");
                    Console.WriteLine($"  Luminosity: {star.Luminosity:F4} solar luminosities");
                    Console.WriteLine($"  Color (RGB): ({star.Color.X:F3}, {star.Color.Y:F3}, {star.Color.Z:F3})");
                    Console.WriteLine($"  Population: {star.Population}");
                    Console.WriteLine($"  Region: {star.Region}");
                    Console.WriteLine($"  Planets: {star.PlanetCount}");
                    
                    // Display the unified system tree
                    Console.WriteLine(UnifiedSystemGenerator.GetSystemTreeDisplay(systemRoot));
                    
                    if (star.IsMultiple)
                    {
                        Console.WriteLine($"  Multiple System: Yes (see system tree above)");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
    
    static void InvestigateChunkNew(ChunkBasedGalaxySystem chunkSystem)
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
                var startTime = DateTime.Now;
                chunkSystem.InvestigateChunk(input!);
                var elapsed = (DateTime.Now - startTime).TotalSeconds;
                Console.WriteLine($"\nTotal time: {elapsed:F2}s");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
    
    static void VisualizeChunk(ChunkBasedGalaxySystem chunkSystem)
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