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
            Console.WriteLine("3. Generate galaxy statistics");
            Console.WriteLine("4. Generate galaxy images");
            Console.WriteLine("5. Advanced analytical statistics");
            Console.WriteLine("6. Investigate galaxy chunk");
            Console.WriteLine("7. Exit");
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
                    GenerateStatistics(generator);
                    break;
                case "4":
                    GenerateGalaxyImages(generator);
                    break;
                case "5":
                    GenerateAdvancedStatistics(generator);
                    break;
                case "6":
                    InvestigateChunkNew(chunkBasedSystem);
                    break;
                case "7":
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
    
    static void GenerateStatistics(ScientificMilkyWayGenerator generator)
    {
        Console.WriteLine("\n=== Galaxy Statistics ===");
        Console.WriteLine("Analyzing 1 million star sample...");
        
        var stars = generator.GenerateStars(1000000);
        
        var typeGroups = stars.GroupBy(s => s.Type).OrderByDescending(g => g.Count());
        var popGroups = stars.GroupBy(s => s.Population).OrderByDescending(g => g.Count());
        var regionGroups = stars.GroupBy(s => s.Region).OrderByDescending(g => g.Count());
        
        Console.WriteLine("\nStar Types:");
        foreach (var group in typeGroups)
        {
            Console.WriteLine($"  {group.Key,-15} {group.Count(),8} ({group.Count() / 10000.0:F1}%)");
        }
        
        Console.WriteLine("\nStellar Populations:");
        foreach (var group in popGroups)
        {
            Console.WriteLine($"  {group.Key,-15} {group.Count(),8} ({group.Count() / 10000.0:F1}%)");
        }
        
        Console.WriteLine("\nGalactic Regions:");
        foreach (var group in regionGroups)
        {
            Console.WriteLine($"  {group.Key,-15} {group.Count(),8} ({group.Count() / 10000.0:F1}%)");
        }
        
        var avgDistance = stars.Average(s => Math.Sqrt(s.Position.X * s.Position.X + s.Position.Y * s.Position.Y + s.Position.Z * s.Position.Z));
        var maxDistance = stars.Max(s => Math.Sqrt(s.Position.X * s.Position.X + s.Position.Y * s.Position.Y + s.Position.Z * s.Position.Z));
        
        Console.WriteLine($"\nDistance Statistics:");
        Console.WriteLine($"  Average distance from center: {avgDistance:F0} ly");
        Console.WriteLine($"  Maximum distance from center: {maxDistance:F0} ly");
    }
    
    static void GenerateGalaxyImages(ScientificMilkyWayGenerator generator)
    {
        Console.WriteLine("\n=== Generate Galaxy Images ===");
        Console.WriteLine("Choose visualization method:");
        Console.WriteLine("1. Original (uniform sampling) - fast but misses core detail");
        Console.WriteLine("2. Density-based sampling - accurate representation");
        Console.Write("Choice (1-2): ");
        
        var method = Console.ReadLine();
        
        if (method == "2")
        {
            Console.Write("Number of stars (100000-2000000, default 500000): ");
            if (!int.TryParse(Console.ReadLine(), out int count) || count < 100000 || count > 2000000)
            {
                count = 500000;
                Console.WriteLine($"Using default: {count:N0}");
            }
            
            var visualizer2 = new ScientificGalaxyVisualizer2(generator);
            visualizer2.GenerateAllViews(2048, 2048, count);
        }
        else
        {
            Console.Write("Number of stars (10000-1000000, default 100000): ");
            if (!int.TryParse(Console.ReadLine(), out int count) || count < 10000 || count > 1000000)
            {
                count = 100000;
                Console.WriteLine($"Using default: {count:N0}");
            }
            
            var visualizer = new ScientificGalaxyVisualizer(generator, count);
            visualizer.GenerateAllViews();
        }
        
        Console.WriteLine("\nImages have been saved to the current directory!");
    }
    
    static void GenerateAdvancedStatistics(ScientificMilkyWayGenerator generator)
    {
        // Use analytical calculations instead of sampling
        AdvancedGalaxyStatistics.GenerateReport();
        
        // Optional: Generate a small sample for verification
        Console.WriteLine("\n=== Sample Verification ===");
        Console.Write("Generate sample stars for verification? (y/n): ");
        if (Console.ReadLine()?.ToLower() == "y")
        {
            Console.Write("Number of stars (1000-10000): ");
            if (!int.TryParse(Console.ReadLine(), out int count) || count < 1000 || count > 10000)
            {
                count = 5000;
            }
            Console.WriteLine($"\nGenerating {count:N0} stars for verification...");
        var stars = generator.GenerateStars(count);
        
        // Group stars by region
        var regionGroups = stars.GroupBy(s => s.Region).ToList();
        
        Console.WriteLine("\n=== Inter-Star Distances by Region ===");
        Console.WriteLine("Region | Stars | Avg Distance | Min Distance | Density (stars/ly³)");
        Console.WriteLine(new string('-', 80));
        
        foreach (var region in regionGroups.OrderByDescending(g => g.Count()))
        {
            var regionStars = region.ToList();
            if (regionStars.Count < 10) continue; // Skip regions with too few stars
            
            // Calculate nearest neighbor distances
            var distances = new List<double>();
            var sampleSize = Math.Min(1000, regionStars.Count); // Sample for performance
            var random = new Random();
            var sample = regionStars.OrderBy(x => random.Next()).Take(sampleSize).ToList();
            
            foreach (var star in sample)
            {
                double minDist = double.MaxValue;
                foreach (var other in sample.Where(s => s.Seed != star.Seed))
                {
                    var dx = star.Position.X - other.Position.X;
                    var dy = star.Position.Y - other.Position.Y;
                    var dz = star.Position.Z - other.Position.Z;
                    var dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    if (dist < minDist) minDist = dist;
                }
                if (minDist < double.MaxValue)
                    distances.Add(minDist);
            }
            
            if (distances.Count > 0)
            {
                var avgDist = distances.Average();
                var minDist = distances.Min();
                
                // Estimate density (stars per cubic light-year)
                var density = 1.0 / (avgDist * avgDist * avgDist * 4.189); // 4/3 * pi
                
                Console.WriteLine($"{region.Key,-20} | {regionStars.Count,6} | {avgDist,12:F2} ly | {minDist,12:F2} ly | {density:E2}");
            }
        }
        
        Console.WriteLine("\n=== Stellar Type Distribution by Region ===");
        
        // Analyze stellar types by region
        var majorRegions = regionGroups.Where(g => g.Count() > 100).OrderByDescending(g => g.Count()).Take(5);
        
        foreach (var region in majorRegions)
        {
            Console.WriteLine($"\n{region.Key} ({region.Count():N0} stars):");
            var typeGroups = region.GroupBy(s => s.Type).OrderByDescending(g => g.Count()).Take(5);
            foreach (var type in typeGroups)
            {
                Console.WriteLine($"  {type.Key,-15} {type.Count(),8} ({type.Count() * 100.0 / region.Count():F1}%)");
            }
        }
        
        Console.WriteLine("\n=== Distance Analysis from Galactic Center ===");
        
        // Analyze radial distribution
        var radialBins = new[] { 1000, 5000, 10000, 15000, 20000, 30000, 40000, 50000, 100000 };
        var lastBin = 0;
        
        Console.WriteLine("Distance Range (ly) | Star Count | Avg. Luminosity | Most Common Type");
        Console.WriteLine(new string('-', 80));
        
        foreach (var bin in radialBins)
        {
            var starsInBin = stars.Where(s =>
            {
                var dist = Math.Sqrt(s.Position.X * s.Position.X + s.Position.Y * s.Position.Y + s.Position.Z * s.Position.Z);
                return dist > lastBin && dist <= bin;
            }).ToList();
            
            if (starsInBin.Any())
            {
                var avgLum = starsInBin.Average(s => s.Luminosity);
                var mostCommon = starsInBin.GroupBy(s => s.Type).OrderByDescending(g => g.Count()).First().Key;
                
                Console.WriteLine($"{lastBin,6:N0} - {bin,6:N0} | {starsInBin.Count,10:N0} | {avgLum,15:F3} | {mostCommon}");
            }
            
            lastBin = bin;
        }
        
        Console.WriteLine("\n=== Spiral Arm Analysis ===");
        
        // Identify stars in spiral arms vs inter-arm regions
        var spiralArmRegions = new[] { "Perseus Arm", "Scutum-Centaurus Arm", "Sagittarius Arm", "Norma Arm", "Local Arm (Orion Spur)" };
        var armStars = stars.Where(s => spiralArmRegions.Any(arm => s.Region.Contains(arm))).ToList();
        var interArmStars = stars.Where(s => !spiralArmRegions.Any(arm => s.Region.Contains(arm))).ToList();
        
        Console.WriteLine($"Stars in spiral arms: {armStars.Count:N0} ({armStars.Count * 100.0 / stars.Count:F1}%)");
        Console.WriteLine($"Stars between arms: {interArmStars.Count:N0} ({interArmStars.Count * 100.0 / stars.Count:F1}%)");
        
            if (armStars.Any() && interArmStars.Any())
            {
                Console.WriteLine($"Avg luminosity in arms: {armStars.Average(s => s.Luminosity):F3}");
                Console.WriteLine($"Avg luminosity between arms: {interArmStars.Average(s => s.Luminosity):F3}");
            }
        }
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
        
        var planetGen = new PlanetarySystemGenerator();
        
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
                
                // Handle companion star query
                if (isCompanion)
                {
                    if (!star.IsMultiple)
                    {
                        Console.WriteLine($"\nError: Star {starSeed} is not part of a multiple star system.");
                        Console.WriteLine($"No companion '{companionLetter}' exists for this star.");
                        continue;
                    }
                    
                    Console.WriteLine($"\nCompanion Star Details:");
                    Console.WriteLine($"  System: {starSeed}-{companionLetter}");
                    Console.WriteLine($"  Primary star seed: {starSeed}");
                    Console.WriteLine($"  Companion designation: {companionLetter}");
                    Console.WriteLine($"  Primary star type: {star.Type}");
                    Console.WriteLine($"  Primary star mass: {star.Mass:F3} solar masses");
                    
                    var (_, companionCount, companionDesignations) = CompanionStarDatabase.GetCompanionInfo(star.Seed, star.Type);
                    if (companionDesignations.Contains(companionLetter))
                    {
                        var (mass, separationAU, _) = CompanionStarDatabase.GetCompanionProperties(star.Seed, star.Mass, companionLetter);
                        var companionType = CompanionStarDatabase.GetCompanionStellarType(mass, star.Seed, companionLetter);
                        Console.WriteLine($"  Companion type: {companionType}");
                        Console.WriteLine($"  Companion mass: {mass:F2} solar masses");
                        Console.WriteLine($"  Separation: {separationAU:F1} AU");
                    }
                    else
                    {
                        Console.WriteLine($"  Error: Companion '{companionLetter}' not found in system");
                    }
                    
                    if (isPlanet)
                    {
                        Console.WriteLine($"\n  Planet {planetIndex} of companion star {companionLetter}");
                        if (isMoon)
                        {
                            Console.WriteLine($"  Moon {moonLetter} of planet {planetIndex}");
                        }
                    }
                }
                // Handle planet query
                else if (isPlanet)
                {
                    var system = planetGen.GeneratePlanetarySystem(star.Seed, star.Type, star.Mass, $"Star-{star.Seed}");
                    var planet = planetGen.GetPlanetDetails(system, planetIndex);
                    
                    if (planet != null)
                    {
                        Console.WriteLine($"\nPlanet Details:");
                        Console.WriteLine($"  Star: {system.StarName} (Seed: {starSeed})");
                        Console.WriteLine($"  Planet Index: {planet.Index}");
                        Console.WriteLine($"  Type: {planet.Type}");
                        Console.WriteLine($"  Mass: {planet.Mass:F2} Earth masses");
                        Console.WriteLine($"  Orbital Distance: {planet.OrbitalDistance:F2} AU");
                        Console.WriteLine($"  Moons: {planet.Moons.Count}");
                        
                        if (isMoon)
                        {
                            var moonId = $"{planetIndex}-{moonLetter}";
                            var moon = planetGen.GetMoonDetails(system, moonId);
                            if (moon != null)
                            {
                                Console.WriteLine($"\nMoon Details:");
                                Console.WriteLine($"  Moon: {moon.Letter}");
                                Console.WriteLine($"  Type: {moon.Type}");
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
                    
                    if (star.IsMultiple)
                    {
                        Console.WriteLine($"  Multiple System: Yes");
                        
                        var (_, companionCount, companionDesignations) = CompanionStarDatabase.GetCompanionInfo(star.Seed, star.Type);
                        Console.WriteLine($"  Companion stars: {companionCount}");
                        foreach (var designation in companionDesignations)
                        {
                            var (mass, separationAU, _) = CompanionStarDatabase.GetCompanionProperties(star.Seed, star.Mass, designation);
                            var companionType = CompanionStarDatabase.GetCompanionStellarType(mass, star.Seed, designation);
                            Console.WriteLine($"    {starSeed}-{designation}: {companionType}, {mass:F2} solar masses, {separationAU:F1} AU separation");
                        }
                    }
                    
                    // Show planetary system if it has planets
                    if (star.PlanetCount > 0)
                    {
                        var system = planetGen.GeneratePlanetarySystem(star.Seed, star.Type, star.Mass, $"Star-{star.Seed}");
                        Console.WriteLine("\n  Planetary System:");
                        foreach (var planet in system.Planets)
                        {
                            Console.WriteLine($"    Planet {planet.Index}: {planet.Type} ({planet.Mass:F1} Earth masses, {planet.OrbitalDistance:F2} AU, {planet.Moons.Count} moons)");
                        }
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
}