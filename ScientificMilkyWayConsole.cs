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
        Console.WriteLine("\n=== Star Finder (Improved System) ===");
        Console.WriteLine("Seeds now encode: ChunkR_ChunkTheta_ChunkZ_StarIndex");
        Console.WriteLine("\nYou can enter:");
        Console.WriteLine("  1. Star seed (e.g., 12345678)");
        Console.WriteLine("  2. Chunk coordinates: r_theta_z_index (e.g., 260_45_0_100)");
        Console.WriteLine("  3. Star system object: SEED-SUFFIX (e.g., 12345678-A, 12345678-1, 12345678-1-a)");
        Console.WriteLine("\nSuffix format:");
        Console.WriteLine("  Stars: A, B, C, D (uppercase letters)");
        Console.WriteLine("  Planets: 1, 2, 3... (numbers)");
        Console.WriteLine("  Moons: a, b, c... (lowercase letters)");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  12345678      - Primary star system overview");
        Console.WriteLine("  12345678-B    - Star B details");
        Console.WriteLine("  12345678-1    - Planet 1 of primary star");
        Console.WriteLine("  12345678-B-1  - Planet 1 of star B");
        Console.WriteLine("  12345678-1-a  - Moon a of planet 1");
        
        var unifiedGen = new UnifiedSystemGenerator();
        
        while (true)
        {
            Console.Write("\nEnter seed or chunk coordinates (or 'q' to quit): ");
            var input = Console.ReadLine();
            
            if (input?.ToLower() == "q") break;
            
            try
            {
                Star star;
                long starSeed = 0;
                string? suffix = null;
                
                // Check if it's SEED-SUFFIX format (e.g., 12345678-A-1-a)
                if (input != null && input.Contains('-') && !input.Contains('_'))
                {
                    var dashIndex = input.IndexOf('-');
                    var seedPart = input.Substring(0, dashIndex);
                    suffix = input.Substring(dashIndex + 1);
                    
                    if (long.TryParse(seedPart, out starSeed))
                    {
                        var (r, theta, z, index) = ChunkBasedGalaxySystem.DecodeSeed(starSeed);
                        Console.WriteLine($"Decoded to chunk {r}_{theta}_{z}, star index {index}");
                    }
                    else
                    {
                        Console.WriteLine("Invalid seed format. Use SEED-SUFFIX");
                        continue;
                    }
                }
                // Check if it's underscore format (e.g., 260_45_0_100)
                else if (input != null && input.Contains('_'))
                {
                    var parts = input.Split('_');
                    
                    // Basic star format: r_theta_z_index
                    if (parts.Length == 4)
                    {
                        int r = int.Parse(parts[0]);
                        int theta = int.Parse(parts[1]);
                        int z = int.Parse(parts[2]);
                        long index = long.Parse(parts[3]);
                        
                        // Handle negative indices for rogue planets
                        if (index < 0)
                        {
                            index = Math.Abs(index) - 1; // Convert to 0-based positive
                            index |= 0x800000000; // Set high bit for rogue planet
                        }
                        
                        starSeed = ChunkBasedGalaxySystem.EncodeSeed(r, theta, z, index);
                        Console.WriteLine($"Encoded seed: {starSeed}");
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
                
                // Check if this is a rogue planet
                if (ChunkBasedGalaxySystem.IsRoguePlanetSeed(starSeed))
                {
                    var rogue = chunkSystem.GetRoguePlanetBySeed(starSeed);
                    Console.WriteLine($"\nRogue Planet at seed {starSeed}:");
                    Console.WriteLine($"  Chunk: {rogue.ChunkR}_{rogue.ChunkTheta}_{rogue.ChunkZ}");
                    Console.WriteLine($"  Index: {rogue.Index}");
                    Console.WriteLine($"  Position: ({rogue.Position.X:F1}, {rogue.Position.Y:F1}, {rogue.Position.Z:F1}) ly");
                    Console.WriteLine($"  Type: {rogue.Type}");
                    
                    // Display mass in appropriate units
                    float massEarth = rogue.Mass / 0.00315f;
                    if (massEarth < 10)
                        Console.WriteLine($"  Mass: {massEarth:F2} Earth masses");
                    else
                        Console.WriteLine($"  Mass: {rogue.Mass:F3} Jupiter masses");
                    
                    Console.WriteLine($"  Radius: {rogue.Radius:F1} Earth radii");
                    Console.WriteLine($"  Temperature: {rogue.Temperature:F0} K");
                    Console.WriteLine($"  Origin: {rogue.Origin}");
                    Console.WriteLine($"  Moons: {rogue.MoonCount}");
                    
                    // Distance from galactic center
                    var distance = Math.Sqrt(rogue.Position.X * rogue.Position.X + 
                                           rogue.Position.Y * rogue.Position.Y + 
                                           rogue.Position.Z * rogue.Position.Z);
                    Console.WriteLine($"  Distance from center: {distance:F1} ly");
                    continue;
                }
                
                // Get the star
                star = chunkSystem.GetStarBySeed(starSeed);
                
                // Generate the unified system once
                var system = unifiedGen.GenerateSystem(star.Seed, ConvertToScientificType(star.Type), star.Mass, 
                    star.Temperature, star.Luminosity);
                
                // If no suffix provided, show the star and its system
                if (string.IsNullOrEmpty(suffix))
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
                    
                    // Display the system tree
                    Console.WriteLine(system.GetFullTreeDisplay());
                }
                else
                {
                    // Find the object by suffix
                    var obj = unifiedGen.FindObjectBySuffix(system, suffix);
                    
                    if (obj == null)
                    {
                        Console.WriteLine($"\nObject with suffix '{suffix}' not found in system {starSeed}");
                        continue;
                    }
                    
                    // Display object details based on type
                    switch (obj)
                    {
                        case UnifiedSystemGenerator.Star starObj:
                            Console.WriteLine($"\nStar {starObj.Name} Details:");
                            Console.WriteLine($"  Full ID: {starObj.Id}");
                            Console.WriteLine($"  Type: {starObj.StellarType}");
                            Console.WriteLine($"  Mass: {starObj.Mass:F3} solar masses");
                            Console.WriteLine($"  Temperature: {starObj.Temperature:F0} K");
                            Console.WriteLine($"  Luminosity: {starObj.Luminosity:F4} solar luminosities");
                            if (starObj.Relationship != UnifiedSystemGenerator.StarRelationship.Primary)
                            {
                                Console.WriteLine($"  Relationship: {starObj.Relationship}");
                                Console.WriteLine($"  Separation: {starObj.OrbitalDistance:F2} AU");
                            }
                            if (starObj.BinaryCompanion != null)
                            {
                                Console.WriteLine($"  Binary companion: Star {starObj.BinaryCompanion.Name}");
                            }
                            Console.WriteLine($"  Planets: {starObj.Children.Count(c => c is UnifiedSystemGenerator.Planet)}");
                            
                            // List planets
                            var planets = starObj.Children.OfType<UnifiedSystemGenerator.Planet>().ToList();
                            if (planets.Any())
                            {
                                Console.WriteLine("\n  Planets:");
                                foreach (var p in planets)
                                {
                                    var binaryStr = p.BinaryCompanion != null ? " (binary)" : "";
                                    Console.WriteLine($"    Planet {p.Name}: {p.PlanetType}, {p.Mass:F2} M⊕, {p.OrbitalDistance:F2} AU{binaryStr}");
                                }
                            }
                            break;
                            
                        case UnifiedSystemGenerator.Planet planetObj:
                            Console.WriteLine($"\nPlanet {planetObj.Name} Details:");
                            Console.WriteLine($"  Full ID: {planetObj.Id}");
                            Console.WriteLine($"  Parent star: {planetObj.Parent?.Name ?? "Unknown"}");
                            Console.WriteLine($"  Type: {planetObj.PlanetType}");
                            Console.WriteLine($"  Mass: {planetObj.Mass:F2} Earth masses");
                            Console.WriteLine($"  Radius: {planetObj.Radius:F2} Earth radii");
                            Console.WriteLine($"  Orbital Distance: {planetObj.OrbitalDistance:F2} AU");
                            Console.WriteLine($"  Orbital Period: {planetObj.OrbitalPeriod:F2} years");
                            Console.WriteLine($"  Temperature: {planetObj.Temperature:F0} K");
                            if (planetObj.BinaryCompanion != null)
                            {
                                Console.WriteLine($"  Binary companion: Planet {planetObj.BinaryCompanion.Name}");
                            }
                            Console.WriteLine($"  Moons: {planetObj.Children.Count}");
                            
                            // List moons
                            var moons = planetObj.Children.OfType<UnifiedSystemGenerator.Moon>().ToList();
                            if (moons.Any())
                            {
                                Console.WriteLine("\n  Moons:");
                                foreach (var m in moons)
                                {
                                    var binaryStr = m.BinaryCompanion != null ? " (binary)" : "";
                                    Console.WriteLine($"    Moon {m.Name}: {m.Composition}, {m.Mass:F4} M☾{binaryStr}");
                                }
                            }
                            break;
                            
                        case UnifiedSystemGenerator.Moon moonObj:
                            Console.WriteLine($"\nMoon {moonObj.Name} Details:");
                            Console.WriteLine($"  Full ID: {moonObj.Id}");
                            Console.WriteLine($"  Parent planet: {moonObj.Parent?.Name ?? "Unknown"}");
                            Console.WriteLine($"  Composition: {moonObj.Composition}");
                            Console.WriteLine($"  Mass: {moonObj.Mass:F4} Moon masses");
                            Console.WriteLine($"  Radius: {moonObj.Radius:F2} Moon radii");
                            Console.WriteLine($"  Orbital Distance: {moonObj.OrbitalDistance:F4} AU");
                            if (moonObj.BinaryCompanion != null)
                            {
                                Console.WriteLine($"  Binary companion: Moon {moonObj.BinaryCompanion.Name}");
                            }
                            break;
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
    
    /// <summary>
    /// Convert StellarTypeGenerator type to ScientificMilkyWayGenerator type for UnifiedSystemGenerator
    /// </summary>
    private static ScientificMilkyWayGenerator.StellarType ConvertToScientificType(StellarTypeGenerator.StellarType type)
    {
        return type switch
        {
            StellarTypeGenerator.StellarType.O5V => ScientificMilkyWayGenerator.StellarType.O5V,
            StellarTypeGenerator.StellarType.B0V => ScientificMilkyWayGenerator.StellarType.B0V,
            StellarTypeGenerator.StellarType.B5V => ScientificMilkyWayGenerator.StellarType.B5V,
            StellarTypeGenerator.StellarType.A0V => ScientificMilkyWayGenerator.StellarType.A0V,
            StellarTypeGenerator.StellarType.A5V => ScientificMilkyWayGenerator.StellarType.A5V,
            StellarTypeGenerator.StellarType.F0V => ScientificMilkyWayGenerator.StellarType.F0V,
            StellarTypeGenerator.StellarType.F5V => ScientificMilkyWayGenerator.StellarType.F5V,
            StellarTypeGenerator.StellarType.G0V => ScientificMilkyWayGenerator.StellarType.G0V,
            StellarTypeGenerator.StellarType.G5V => ScientificMilkyWayGenerator.StellarType.G5V,
            StellarTypeGenerator.StellarType.K0V => ScientificMilkyWayGenerator.StellarType.K0V,
            StellarTypeGenerator.StellarType.K5V => ScientificMilkyWayGenerator.StellarType.K5V,
            StellarTypeGenerator.StellarType.M0V => ScientificMilkyWayGenerator.StellarType.M0V,
            StellarTypeGenerator.StellarType.M5V => ScientificMilkyWayGenerator.StellarType.M5V,
            StellarTypeGenerator.StellarType.M8V => ScientificMilkyWayGenerator.StellarType.M8V,
            
            // Brown dwarfs
            StellarTypeGenerator.StellarType.L0 => ScientificMilkyWayGenerator.StellarType.L0,
            StellarTypeGenerator.StellarType.L5 => ScientificMilkyWayGenerator.StellarType.L5,
            StellarTypeGenerator.StellarType.T0 => ScientificMilkyWayGenerator.StellarType.T0,
            StellarTypeGenerator.StellarType.T5 => ScientificMilkyWayGenerator.StellarType.T5,
            StellarTypeGenerator.StellarType.Y0 => ScientificMilkyWayGenerator.StellarType.Y0,
            
            // Giants
            StellarTypeGenerator.StellarType.G5III => ScientificMilkyWayGenerator.StellarType.G5III,
            StellarTypeGenerator.StellarType.K0III => ScientificMilkyWayGenerator.StellarType.K0III,
            StellarTypeGenerator.StellarType.K5III => ScientificMilkyWayGenerator.StellarType.K5III,
            StellarTypeGenerator.StellarType.M0III => ScientificMilkyWayGenerator.StellarType.M0III,
            StellarTypeGenerator.StellarType.B0III => ScientificMilkyWayGenerator.StellarType.B0III,
            
            // Supergiants
            StellarTypeGenerator.StellarType.M2I => ScientificMilkyWayGenerator.StellarType.M2I,
            StellarTypeGenerator.StellarType.B0I => ScientificMilkyWayGenerator.StellarType.B0I,
            
            // Compact objects
            StellarTypeGenerator.StellarType.DA => ScientificMilkyWayGenerator.StellarType.DA,
            StellarTypeGenerator.StellarType.NS => ScientificMilkyWayGenerator.StellarType.NS,
            StellarTypeGenerator.StellarType.BH => ScientificMilkyWayGenerator.StellarType.BH,
            StellarTypeGenerator.StellarType.QS => ScientificMilkyWayGenerator.StellarType.NS, // Treat as neutron star
            StellarTypeGenerator.StellarType.SMBH => ScientificMilkyWayGenerator.StellarType.SMBH,
            
            _ => ScientificMilkyWayGenerator.StellarType.G5V
        };
    }
}