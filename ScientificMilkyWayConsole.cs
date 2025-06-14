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
        var chunkSystem = new GalaxyChunkSystem();
        
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Scientific Milky Way Galaxy Generator ===");
            Console.WriteLine("Mode: CHUNK-BASED SYSTEM");
            Console.WriteLine("Based on latest astronomical research (2024)");
            Console.WriteLine();
            Console.WriteLine("1. Export stars for Unity (JSON)");
            Console.WriteLine("2. Find star by seed");
            Console.WriteLine("3. Generate comprehensive galaxy statistics");
            Console.WriteLine("4. Generate galaxy images");
            Console.WriteLine("5. Investigate galaxy chunk");
            Console.WriteLine("6. Exit");
            Console.WriteLine();
            Console.Write("Select option: ");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    ExportForUnity(generator);
                    break;
                case "2":
                    FindStarBySeedChunkBased(chunkSystem, generator);
                    break;
                case "3":
                    GenerateComprehensiveStatistics(generator, chunkSystem);
                    break;
                case "4":
                    GenerateGalaxyImages(generator);
                    break;
                case "5":
                    InvestigateChunk(chunkSystem);
                    break;
                case "6":
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
    
    static void GenerateComprehensiveStatistics(ScientificMilkyWayGenerator generator, GalaxyChunkSystem chunkSystem)
    {
        Console.WriteLine("\n=== COMPREHENSIVE GALAXY STATISTICS ===");
        Console.WriteLine("Choose analysis type:");
        Console.WriteLine("1. Sample-based statistics (1M stars - fast)");
        Console.WriteLine("2. Analytical statistics (mathematical - instant)");
        Console.WriteLine("3. Both sample and analytical");
        Console.Write("Selection (1-3): ");
        
        var choice = Console.ReadLine();
        bool doSample = choice == "1" || choice == "3";
        bool doAnalytical = choice == "2" || choice == "3";
        
        if (doSample)
        {
            Console.WriteLine("\n--- SAMPLE-BASED ANALYSIS ---");
            Console.Write("Number of stars to sample (100000-10000000, default 1000000): ");
            if (!int.TryParse(Console.ReadLine(), out int sampleSize) || sampleSize < 100000 || sampleSize > 10000000)
            {
                sampleSize = 1000000;
            }
            
            Console.WriteLine($"\nAnalyzing {sampleSize:N0} star sample...");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var stars = generator.GenerateStars(sampleSize);
            sw.Stop();
            Console.WriteLine($"Sample generated in {sw.ElapsedMilliseconds:N0} ms");
            
            // Stellar classification breakdown with interesting facts
            Console.WriteLine("\n[STELLAR CLASSIFICATION]");
            var typeGroups = stars.GroupBy(s => s.Type).OrderByDescending(g => g.Count());
            foreach (var group in typeGroups.Take(10))
            {
                var percentage = group.Count() * 100.0 / sampleSize;
                var estimatedTotal = (long)(percentage * 1e9); // Scale to 100 billion
                var properties = generator.GetStellarProperties(group.Key);
                
                Console.WriteLine($"  {group.Key,-6} {percentage,5:F2}% (~{estimatedTotal/1e9:F1}B stars) - " +
                    $"{properties.temperature:F0}K, {properties.mass:F2}M☉, {GetStarDescription(group.Key)}");
            }
            
            // Multiple star systems
            var multipleStars = stars.Where(s => s.IsMultiple).Count();
            Console.WriteLine($"\n[MULTIPLE STAR SYSTEMS]");
            Console.WriteLine($"  Binary/Multiple Systems: {multipleStars * 100.0 / sampleSize:F1}% (~{multipleStars * 100L:N0} million systems)");
            
            // Planetary statistics
            var starsWithPlanets = stars.Where(s => s.PlanetCount > 0).ToList();
            var totalPlanets = starsWithPlanets.Sum(s => s.PlanetCount);
            Console.WriteLine($"\n[PLANETARY SYSTEMS]");
            Console.WriteLine($"  Stars with planets: {starsWithPlanets.Count * 100.0 / sampleSize:F1}% (~{starsWithPlanets.Count * 100L:N0} million systems)");
            Console.WriteLine($"  Average planets per system: {(double)totalPlanets / starsWithPlanets.Count:F2}");
            Console.WriteLine($"  Estimated total planets: ~{totalPlanets * 100L:N0} million");
            
            // Spatial distribution analysis
            Console.WriteLine($"\n[SPATIAL DISTRIBUTION]");
            var regionGroups = stars.GroupBy(s => s.Region).OrderByDescending(g => g.Count());
            foreach (var group in regionGroups)
            {
                var regionStars = group.ToList();
                var avgR = regionStars.Average(s => Math.Sqrt(s.Position.X * s.Position.X + s.Position.Y * s.Position.Y));
                var avgZ = regionStars.Average(s => Math.Abs(s.Position.Z));
                Console.WriteLine($"  {group.Key,-20} {group.Count() * 100.0 / sampleSize,5:F1}% - Avg R: {avgR:F0} ly, Avg |Z|: {avgZ:F0} ly");
            }
            
            // Population analysis
            Console.WriteLine($"\n[STELLAR POPULATIONS & METALLICITY]");
            var popGroups = stars.GroupBy(s => s.Population).OrderByDescending(g => g.Count());
            foreach (var group in popGroups)
            {
                var popStars = group.ToList();
                var avgAge = GetPopulationAge(group.Key);
                Console.WriteLine($"  {group.Key,-12} {group.Count() * 100.0 / sampleSize,5:F1}% - Age: {avgAge}, {GetPopulationDescription(group.Key)}");
            }
            
            // Extreme stars
            Console.WriteLine($"\n[EXTREME STARS IN SAMPLE]");
            var massiveStars = stars.Where(s => s.Mass > 20).OrderByDescending(s => s.Mass).Take(5);
            Console.WriteLine($"  Most Massive Stars:");
            foreach (var star in massiveStars)
            {
                Console.WriteLine($"    Seed {star.Seed}: {star.Type} - {star.Mass:F1} M☉ at ({star.Position.X:F0}, {star.Position.Y:F0}, {star.Position.Z:F0})");
            }
            
            var brightestStars = stars.Where(s => s.Luminosity > 10000).OrderByDescending(s => s.Luminosity).Take(5);
            Console.WriteLine($"  Most Luminous Stars:");
            foreach (var star in brightestStars)
            {
                Console.WriteLine($"    Seed {star.Seed}: {star.Type} - {star.Luminosity:F0} L☉ ({star.Luminosity/3.828e26:E2} watts)");
            }
            
            // Density variations
            Console.WriteLine($"\n[DENSITY ANALYSIS]");
            var centralStars = stars.Where(s => s.Position.Length() < 1000).Count();
            var diskStars = stars.Where(s => s.Position.Length2D() > 20000 && s.Position.Length2D() < 30000 && Math.Abs(s.Position.Z) < 500).Count();
            var haloStars = stars.Where(s => s.Position.Length() > 50000).Count();
            
            var centralVolume = 4.0/3.0 * Math.PI * Math.Pow(1000, 3);
            var diskVolume = Math.PI * (Math.Pow(30000, 2) - Math.Pow(20000, 2)) * 1000;
            var haloVolume = 4.0/3.0 * Math.PI * (Math.Pow(100000, 3) - Math.Pow(50000, 3));
            
            Console.WriteLine($"  Central region (<1 kly): {centralStars / centralVolume:E2} stars/ly³");
            Console.WriteLine($"  Disk (20-30 kly, |z|<500): {diskStars / diskVolume:E2} stars/ly³");
            Console.WriteLine($"  Halo (>50 kly): {haloStars / haloVolume:E2} stars/ly³");
            Console.WriteLine($"  Density contrast (center/disk): {(centralStars / centralVolume) / (diskStars / diskVolume):F0}x");
        }
        
        if (doAnalytical)
        {
            Console.WriteLine("\n--- ANALYTICAL CALCULATIONS ---");
            GalacticAnalytics.GenerateReport(generator);
        }
        
        // Fun facts
        Console.WriteLine("\n[INTERESTING FACTS]");
        Console.WriteLine($"  • If you could travel at light speed, crossing the galaxy would take 100,000 years");
        Console.WriteLine($"  • The galaxy rotates once every ~225 million years (one 'galactic year')");
        Console.WriteLine($"  • Our Sun has completed ~20 galactic orbits since its formation");
        Console.WriteLine($"  • There are more stars in our galaxy than grains of sand on all Earth's beaches");
        Console.WriteLine($"  • The supermassive black hole at the center has a mass of 4.3 million suns");
        Console.WriteLine($"  • Most stars (75%) are smaller and cooler than our Sun");
        Console.WriteLine($"  • The galaxy's dark matter halo extends far beyond the visible stars");
    }
    
    static string GetStarDescription(ScientificMilkyWayGenerator.StellarType type)
    {
        return type switch
        {
            ScientificMilkyWayGenerator.StellarType.O5V => "Extremely hot blue stars, very rare",
            ScientificMilkyWayGenerator.StellarType.B0V => "Hot blue-white stars, massive and short-lived",
            ScientificMilkyWayGenerator.StellarType.A0V => "White stars like Sirius",
            ScientificMilkyWayGenerator.StellarType.F0V => "Yellow-white stars, slightly hotter than Sun",
            ScientificMilkyWayGenerator.StellarType.G5V => "Sun-like yellow stars",
            ScientificMilkyWayGenerator.StellarType.K5V => "Orange dwarf stars, very common",
            ScientificMilkyWayGenerator.StellarType.M5V => "Red dwarf stars, most numerous",
            ScientificMilkyWayGenerator.StellarType.K0III => "Red giant stars",
            ScientificMilkyWayGenerator.StellarType.M2I => "Red supergiant stars like Betelgeuse",
            ScientificMilkyWayGenerator.StellarType.DA => "White dwarf stellar remnants",
            ScientificMilkyWayGenerator.StellarType.NS => "Neutron star/pulsar remnants",
            ScientificMilkyWayGenerator.StellarType.BH => "Stellar-mass black holes",
            ScientificMilkyWayGenerator.StellarType.SMBH => "Supermassive black hole (Sgr A*)",
            _ => "Unknown type"
        };
    }
    
    static string GetPopulationAge(ScientificMilkyWayGenerator.StellarPopulation pop)
    {
        return pop switch
        {
            ScientificMilkyWayGenerator.StellarPopulation.PopIII => "13+ Gyr",
            ScientificMilkyWayGenerator.StellarPopulation.PopII => "10-13 Gyr",
            ScientificMilkyWayGenerator.StellarPopulation.Halo => "11-13 Gyr",
            ScientificMilkyWayGenerator.StellarPopulation.Bulge => "10-12 Gyr",
            ScientificMilkyWayGenerator.StellarPopulation.ThickDisk => "8-10 Gyr",
            ScientificMilkyWayGenerator.StellarPopulation.ThinDisk => "0-8 Gyr",
            _ => "Unknown"
        };
    }
    
    static string GetPopulationDescription(ScientificMilkyWayGenerator.StellarPopulation pop)
    {
        return pop switch
        {
            ScientificMilkyWayGenerator.StellarPopulation.PopIII => "First generation, metal-free stars",
            ScientificMilkyWayGenerator.StellarPopulation.PopII => "Old, metal-poor stars",
            ScientificMilkyWayGenerator.StellarPopulation.Halo => "Ancient halo stars, eccentric orbits",
            ScientificMilkyWayGenerator.StellarPopulation.Bulge => "Old bulge/bar stars",
            ScientificMilkyWayGenerator.StellarPopulation.ThickDisk => "Intermediate age, heated orbits",
            ScientificMilkyWayGenerator.StellarPopulation.ThinDisk => "Young disk stars, circular orbits",
            _ => "Unknown population"
        };
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
        
        Console.Write("Z-axis exaggeration for side view (1-10, default 5, use 1 for realistic): ");
        if (!float.TryParse(Console.ReadLine(), out float zExaggeration) || zExaggeration < 1 || zExaggeration > 10)
        {
            zExaggeration = 5.0f;
            Console.WriteLine($"Using default: {zExaggeration:F1}x");
        }
        
        var visualizer = new ScientificGalaxyVisualizer2(generator);
        visualizer.GenerateAllViews(2048, 2048, count, zExaggeration);
        
        Console.WriteLine("\nImages have been saved to the current directory!");
        if (zExaggeration == 1.0f)
        {
            Console.WriteLine("Note: Side view uses realistic proportions (1:1 scale)");
        }
        else
        {
            Console.WriteLine($"Note: Side view Z-axis exaggerated by {zExaggeration:F1}x for visibility");
        }
    }
    
    
    static void FindStarBySeedChunkBased(GalaxyChunkSystem chunkSystem, ScientificMilkyWayGenerator generator)
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
                        var (r, theta, z, index) = GalaxyChunkSystem.DecodeSeed(starSeed);
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
                        
                        starSeed = GalaxyChunkSystem.EncodeSeed(r, theta, z, index);
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
                    var (r, theta, z, index) = GalaxyChunkSystem.DecodeSeed(starSeed);
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
                    
                    var (_, companionCount, companionDesignations) = MultipleStarSystems.GetCompanionInfo(star.Seed, star.Type);
                    if (companionDesignations.Contains(companionLetter))
                    {
                        var (mass, separationAU, _) = MultipleStarSystems.GetCompanionProperties(star.Seed, star.Mass, companionLetter);
                        var companionType = MultipleStarSystems.GetCompanionStellarType(mass, star.Seed, companionLetter);
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
                        // Generate the multiple star system to get companion's planetary system
                        var multipleSystem = MultipleStarSystems.GenerateMultipleStarSystem(star);
                        var companion = multipleSystem.Companions.FirstOrDefault(c => c.Designation == companionLetter);
                        
                        if (companion?.PlanetarySystem != null)
                        {
                            var planet = companion.PlanetarySystem.Planets.FirstOrDefault(p => p.Index == planetIndex);
                            if (planet != null)
                            {
                                Console.WriteLine($"\n  Planet {planetIndex} of companion star {companionLetter}:");
                                Console.WriteLine($"    Type: {planet.Type}");
                                Console.WriteLine($"    Mass: {planet.Mass:F2} Earth masses");
                                Console.WriteLine($"    Orbital Distance: {planet.OrbitalDistance:F2} AU");
                                Console.WriteLine($"    Moons: {planet.Moons.Count}");
                                
                                if (isMoon)
                                {
                                    var moon = planet.Moons.FirstOrDefault(m => m.Letter == moonLetter);
                                    if (moon != null)
                                    {
                                        Console.WriteLine($"\n  Moon {moonLetter} of planet {planetIndex}:");
                                        Console.WriteLine($"    Type: {moon.Type}");
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
                        else
                        {
                            Console.WriteLine($"  No planetary system found for companion {companionLetter}");
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
                        
                        // Generate the complete multiple star system with planetary systems
                        var multipleSystem = MultipleStarSystems.GenerateMultipleStarSystem(star);
                        Console.WriteLine($"  Companion stars: {multipleSystem.Companions.Count}");
                        
                        foreach (var companion in multipleSystem.Companions)
                        {
                            Console.WriteLine($"    {starSeed}-{companion.Designation}: {companion.Type}, {companion.Mass:F2} solar masses, {companion.SeparationAU:F1} AU separation");
                            if (companion.PlanetarySystem != null && companion.PlanetarySystem.Planets.Count > 0)
                            {
                                Console.WriteLine($"      Planets: {companion.PlanetarySystem.Planets.Count}");
                            }
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
    
    static void InvestigateChunk(GalaxyChunkSystem chunkSystem)
    {
        Console.WriteLine("\n=== Galaxy Chunk Investigator ===");
        Console.WriteLine("Fixed 100 ly chunks with no star count limits");
        Console.WriteLine("\nChunk format: r_theta_z");
        Console.WriteLine("Examples:");
        Console.WriteLine("  0_0_0     = Galactic center");
        Console.WriteLine("  260_0_0   = Solar neighborhood");
        Console.WriteLine("  100_180_5 = 10,000 ly opposite side, 500 ly above plane");
        
        while (true)
        {
            Console.Write("\nEnter chunk ID (r_theta_z) or 'q' to quit: ");
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
