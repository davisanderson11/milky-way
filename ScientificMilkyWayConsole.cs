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
    
    static void FindStarBySeedChunkBased(ChunkBasedGalaxySystem chunkSystem)
    {
        Console.WriteLine("\n=== Star Finder (Improved System) ===");
        Console.WriteLine("Seeds now encode: ChunkR_ChunkTheta_ChunkZ_StarIndex");
        Console.WriteLine("\nYou can enter:");
        Console.WriteLine("  1. Star seed (e.g., 12345678)");
        Console.WriteLine("  2. Chunk coordinates: r_theta_z_index (e.g., 260_45_0_100)");
        Console.WriteLine("  3. Star system object: SEED-SUFFIX (e.g., 12345678-A, 12345678-1, 12345678-1-a)");
        Console.WriteLine("  4. Real star name (e.g., 'Sol', 'Proxima Centauri', 'Sirius')");
        Console.WriteLine("  5. 'list real' to see all real stars");
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
            
            if (input?.ToLower() == "list real")
            {
                chunkSystem.ListRealStars();
                continue;
            }
            
            try
            {
                Star star;
                long starSeed = 0;
                string? suffix = null;
                
                // First check if it's a real star name (doesn't contain _ or only digits)
                if (input != null && !input.Contains('_') && !long.TryParse(input.Split('-')[0], out _) && !input.Contains('-'))
                {
                    var realStarSeed = chunkSystem.GetRealStarSeedByName(input);
                    if (realStarSeed.HasValue)
                    {
                        starSeed = realStarSeed.Value;
                        Console.WriteLine($"Found real star '{input}' with seed: {starSeed}");
                    }
                    else
                    {
                        Console.WriteLine($"Real star '{input}' not found. Try 'list real' to see available stars.");
                        continue;
                    }
                }
                // Check if it's SEED-SUFFIX format (e.g., 12345678-A-1-a)
                else if (input != null && input.Contains('-') && !input.Contains('_'))
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
                
                // Generate the unified system
                UnifiedSystemGenerator.StarSystem system;
                
                // Check if this is a real star with real data
                if (star.IsRealStar && star.RealStarData != null)
                {
                    // Convert real star data to unified system
                    system = ConvertRealStarToSystem(star, unifiedGen);
                }
                else
                {
                    // Generate procedural system
                    system = unifiedGen.GenerateSystem(star.Seed, ConvertToScientificType(star.Type), star.Mass, 
                        star.Temperature, star.Luminosity);
                }
                
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
    /// Convert StellarTypeGenerator type to StellarTypeGenerator type for UnifiedSystemGenerator
    /// </summary>
    private static StellarTypeGenerator.StellarType ConvertToScientificType(StellarTypeGenerator.StellarType type)
    {
        return type switch
        {
            StellarTypeGenerator.StellarType.O5V => StellarTypeGenerator.StellarType.O5V,
            StellarTypeGenerator.StellarType.B0V => StellarTypeGenerator.StellarType.B0V,
            StellarTypeGenerator.StellarType.B5V => StellarTypeGenerator.StellarType.B5V,
            StellarTypeGenerator.StellarType.A0V => StellarTypeGenerator.StellarType.A0V,
            StellarTypeGenerator.StellarType.A5V => StellarTypeGenerator.StellarType.A5V,
            StellarTypeGenerator.StellarType.F0V => StellarTypeGenerator.StellarType.F0V,
            StellarTypeGenerator.StellarType.F5V => StellarTypeGenerator.StellarType.F5V,
            StellarTypeGenerator.StellarType.G0V => StellarTypeGenerator.StellarType.G0V,
            StellarTypeGenerator.StellarType.G5V => StellarTypeGenerator.StellarType.G5V,
            StellarTypeGenerator.StellarType.K0V => StellarTypeGenerator.StellarType.K0V,
            StellarTypeGenerator.StellarType.K5V => StellarTypeGenerator.StellarType.K5V,
            StellarTypeGenerator.StellarType.M0V => StellarTypeGenerator.StellarType.M0V,
            StellarTypeGenerator.StellarType.M5V => StellarTypeGenerator.StellarType.M5V,
            StellarTypeGenerator.StellarType.M8V => StellarTypeGenerator.StellarType.M8V,
            
            // Brown dwarfs
            StellarTypeGenerator.StellarType.L0 => StellarTypeGenerator.StellarType.L0,
            StellarTypeGenerator.StellarType.L5 => StellarTypeGenerator.StellarType.L5,
            StellarTypeGenerator.StellarType.T0 => StellarTypeGenerator.StellarType.T0,
            StellarTypeGenerator.StellarType.T5 => StellarTypeGenerator.StellarType.T5,
            StellarTypeGenerator.StellarType.Y0 => StellarTypeGenerator.StellarType.Y0,
            
            // Giants
            StellarTypeGenerator.StellarType.G5III => StellarTypeGenerator.StellarType.G5III,
            StellarTypeGenerator.StellarType.K0III => StellarTypeGenerator.StellarType.K0III,
            StellarTypeGenerator.StellarType.K5III => StellarTypeGenerator.StellarType.K5III,
            StellarTypeGenerator.StellarType.M0III => StellarTypeGenerator.StellarType.M0III,
            StellarTypeGenerator.StellarType.B0III => StellarTypeGenerator.StellarType.B0III,
            
            // Supergiants
            StellarTypeGenerator.StellarType.M2I => StellarTypeGenerator.StellarType.M2I,
            StellarTypeGenerator.StellarType.B0I => StellarTypeGenerator.StellarType.B0I,
            
            // Compact objects
            StellarTypeGenerator.StellarType.DA => StellarTypeGenerator.StellarType.DA,
            StellarTypeGenerator.StellarType.NS => StellarTypeGenerator.StellarType.NS,
            StellarTypeGenerator.StellarType.BH => StellarTypeGenerator.StellarType.BH,
            StellarTypeGenerator.StellarType.QS => StellarTypeGenerator.StellarType.NS, // Treat as neutron star
            StellarTypeGenerator.StellarType.SMBH => StellarTypeGenerator.StellarType.SMBH,
            
            _ => StellarTypeGenerator.StellarType.G5V
        };
    }
    
    static void GenerateDensityHeatmaps()
    {
        Console.WriteLine("\n=== Generate Density Heatmaps ===");
        Console.WriteLine("Create beautiful visualization of star and rogue planet density");
        Console.WriteLine("Using pure mathematical formulas from GalaxyGenerator");
        
        Console.Write("\nImage size (512-4096, default 2048): ");
        var sizeInput = Console.ReadLine();
        int imageSize = 2048;
        
        if (!string.IsNullOrWhiteSpace(sizeInput) && int.TryParse(sizeInput, out var size))
        {
            imageSize = Math.Max(512, Math.Min(4096, size));
        }
        
        Console.Write("\nVertical scale for side view (1.0-10.0, default 5.0): ");
        var scaleInput = Console.ReadLine();
        float verticalScale = 5.0f;
        
        if (!string.IsNullOrWhiteSpace(scaleInput) && float.TryParse(scaleInput, out var scale))
        {
            verticalScale = Math.Max(1.0f, Math.Min(10.0f, scale));
        }
        
        try
        {
            var visualizer = new DensityVisualizer();
            visualizer.GenerateDensityHeatmaps(imageSize, imageSize, verticalScale);
            visualizer.GenerateStellarDensityHeatmaps(imageSize, imageSize, verticalScale);
            
            Console.WriteLine("\n✓ Density heatmaps generated successfully!");
            Console.WriteLine("\nGenerated files:");
            Console.WriteLine("  - MilkyWay_DensityHeatmap_Top.png (top-down view)");
            Console.WriteLine("  - MilkyWay_DensityHeatmap_Side.png (side view)");
            Console.WriteLine("  - MilkyWay_DensityHeatmap_Arms.png (spiral arm enhancement)");
            Console.WriteLine("  - MilkyWay_DensityHeatmap_Rogues.png (rogue planet density)");
            Console.WriteLine("  - MilkyWay_DensityHeatmap_Composite.png (all views combined)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating heatmaps: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Convert real star data to unified system format
    /// </summary>
    static UnifiedSystemGenerator.StarSystem ConvertRealStarToSystem(Star star, UnifiedSystemGenerator unifiedGen)
    {
        var realData = star.RealStarData!;
        
        // Create primary star
        var primaryStar = new UnifiedSystemGenerator.Star
        {
            Id = "A",
            Name = realData.SystemName.EndsWith(" A") ? realData.SystemName : $"{realData.SystemName} A",
            Type = UnifiedSystemGenerator.ObjectType.Star,
            Mass = star.Mass,
            StellarType = ConvertToScientificType(star.Type),
            Temperature = star.Temperature,
            Luminosity = star.Luminosity,
            Relationship = UnifiedSystemGenerator.StarRelationship.Primary
        };
        
        // Create the system
        var system = new UnifiedSystemGenerator.StarSystem
        {
            Seed = star.Seed,
            PrimaryStar = primaryStar
        };
        system.AllStars.Add(primaryStar);
        
        // Add companion stars
        int compIndex = 1;
        foreach (var companion in realData.CompanionStars)
        {
            var compId = ((char)('A' + compIndex)).ToString();
            var compStar = new UnifiedSystemGenerator.Star
            {
                Id = compId,
                Name = companion.SystemName,
                Type = UnifiedSystemGenerator.ObjectType.Star,
                Mass = (float)companion.Mass,
                StellarType = ConvertToScientificType(RealStellarData.ConvertSpectralType(companion.Type)),
                Temperature = (float)companion.Temperature,
                Luminosity = (float)companion.Luminosity,
                Relationship = UnifiedSystemGenerator.StarRelationship.Binary,
                Parent = primaryStar,
                OrbitalDistance = 0.1 * (compIndex + 1) // Approximate orbital distance
            };
            
            // For binary companions, set as BinaryCompanion instead of adding to children
            if (compIndex == 1 && realData.CompanionStars.Count == 1)
            {
                primaryStar.BinaryCompanion = compStar;
            }
            else
            {
                // Multiple companions are satellites
                compStar.Relationship = UnifiedSystemGenerator.StarRelationship.Satellite;
            }
            
            system.AllStars.Add(compStar);
            compIndex++;
        }
        
        // Check if we have real planet data
        if (realData.Planets.Count > 0)
        {
            // Add real planets to PRIMARY star
            int planetIndex = 1;
            foreach (var planet in realData.Planets)
            {
                var planetObj = new UnifiedSystemGenerator.Planet
                {
                    Id = planetIndex.ToString(),
                    Name = planet.Name,
                    Type = UnifiedSystemGenerator.ObjectType.Planet,
                    Mass = (float)planet.Mass * (planet.MassUnit == "Jupiter" ? 317.8f : 1.0f), // Convert to Earth masses
                    Radius = (float)planet.Radius * (planet.RadiusUnit == "Jupiter" ? 11.21f : 1.0f), // Convert to Earth radii
                    Parent = primaryStar,
                    OrbitalDistance = planet.SemiMajorAxis,
                    OrbitalPeriod = planet.OrbitalPeriod, // Already in days
                    PlanetType = ConvertPlanetType(planet.PlanetType)
                };
                
                // Add moons if any
                int moonIndex = 0;
                foreach (var moon in planet.Moons)
                {
                    var moonId = ((char)('a' + moonIndex)).ToString();
                    var moonObj = new UnifiedSystemGenerator.Moon
                    {
                        Id = moonId,
                        Name = moon.Name,
                        Type = UnifiedSystemGenerator.ObjectType.Moon,
                        Mass = (float)moon.Mass * 0.0123f, // Convert to Earth masses (Moon = 0.0123 Earth masses)
                        Radius = (float)moon.Radius * 0.273f, // Convert to Earth radii (Moon = 0.273 Earth radii)
                        Parent = planetObj,
                        OrbitalDistance = moon.SemiMajorAxis / 384400.0, // Convert km to lunar distances
                        OrbitalPeriod = moon.OrbitalPeriod // Already in days
                    };
                    planetObj.Children.Add(moonObj);
                    moonIndex++;
                }
                
                primaryStar.Children.Add(planetObj);
                planetIndex++;
            }
        }
        
        // Add planets for COMPANION stars
        foreach (var companion in realData.CompanionStars)
        {
            if (companion.Planets.Count > 0)
            {
                // Find the corresponding star in our system
                var companionStar = system.AllStars.FirstOrDefault(s => s.Name == companion.SystemName);
                if (companionStar != null)
                {
                    int planetIndex = 1;
                    foreach (var planet in companion.Planets)
                    {
                        var planetObj = new UnifiedSystemGenerator.Planet
                        {
                            Id = planetIndex.ToString(),
                            Name = planet.Name,
                            Type = UnifiedSystemGenerator.ObjectType.Planet,
                            Mass = (float)planet.Mass * (planet.MassUnit == "Jupiter" ? 317.8f : 1.0f),
                            Radius = (float)planet.Radius * (planet.RadiusUnit == "Jupiter" ? 11.21f : 1.0f),
                            Parent = companionStar,
                            OrbitalDistance = planet.SemiMajorAxis,
                            OrbitalPeriod = planet.OrbitalPeriod,
                            PlanetType = ConvertPlanetType(planet.PlanetType)
                        };
                        
                        // Add moons if any
                        int moonIndex = 0;
                        foreach (var moon in planet.Moons)
                        {
                            var moonId = ((char)('a' + moonIndex)).ToString();
                            var moonObj = new UnifiedSystemGenerator.Moon
                            {
                                Id = moonId,
                                Name = moon.Name,
                                Type = UnifiedSystemGenerator.ObjectType.Moon,
                                Mass = (float)moon.Mass * 0.0123f,
                                Radius = (float)moon.Radius * 0.273f,
                                Parent = planetObj,
                                OrbitalDistance = moon.SemiMajorAxis / 384400.0,
                                OrbitalPeriod = moon.OrbitalPeriod
                            };
                            planetObj.Children.Add(moonObj);
                            moonIndex++;
                        }
                        
                        companionStar.Children.Add(planetObj);
                        planetIndex++;
                    }
                }
            }
        }
        
        if (realData.Planets.Count == 0 && realData.CompanionStars.All(c => c.Planets.Count == 0))
        {
            // No real planet data - use procedural generation
            var tempSystem = unifiedGen.GenerateSystem(star.Seed, ConvertToScientificType(star.Type), star.Mass, 
                star.Temperature, star.Luminosity);
            
            // Copy the generated planets to our real star
            foreach (var child in tempSystem.PrimaryStar.Children)
            {
                if (child is UnifiedSystemGenerator.Planet)
                {
                    child.Parent = primaryStar;
                    primaryStar.Children.Add(child);
                }
            }
        }
        
        // Sort children by orbital distance
        primaryStar.Children = primaryStar.Children.OrderBy(c => c.OrbitalDistance).ToList();
        
        return system;
    }
    
    /// <summary>
    /// Convert planet type string to enum
    /// </summary>
    static UnifiedSystemGenerator.PlanetType ConvertPlanetType(string type)
    {
        return type.ToLower() switch
        {
            "terrestrial" => UnifiedSystemGenerator.PlanetType.Terra,
            "gas giant" => UnifiedSystemGenerator.PlanetType.Jupiter,
            "ice giant" => UnifiedSystemGenerator.PlanetType.Neptune,
            "ocean" => UnifiedSystemGenerator.PlanetType.Aquaria,
            "rocky" => UnifiedSystemGenerator.PlanetType.Selena,
            _ => UnifiedSystemGenerator.PlanetType.Terra
        };
    }
}