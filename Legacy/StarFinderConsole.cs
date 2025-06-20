using MilkyWay.Core;
using MilkyWay.Utils;

namespace MilkyWay.Legacy
{
    public static class StarFinderConsole
    {
        public static void Run(ChunkBasedGalaxySystem chunkSystem)
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
                        system = Converter.ConvertRealStarToSystem(star, unifiedGen);
                    }
                    else
                    {
                        // Generate procedural system
                        system = unifiedGen.GenerateSystem(star.Seed, Converter.ConvertToScientificType(star.Type), star.Mass,
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
    }
}
