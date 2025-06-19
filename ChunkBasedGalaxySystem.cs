using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Complete galaxy system using chunk-based seeding
/// Seeds encode: ChunkR_ChunkTheta_ChunkZ_StarIndex
/// </summary>
public class ChunkBasedGalaxySystem
{
    // Chunk dimensions
    private const int CHUNK_SIZE = 100; // 100 light years per chunk
    private const int MAX_RADIUS_CHUNKS = 2000; // 0-1499 (150,000 ly) - extended for halo
    private const int MAX_ANGULAR_CHUNKS = 360; // 0-359 degrees
    private const int MAX_Z_CHUNKS = 501; // -100 to 100 (-10000 to 10000 ly) - extended for halo
    private const double SOL_EXCLUSION_RADIUS = 80.0;    // ly

    
    // Real stellar data
    private RealStellarData? realStellarData;
    private Dictionary<long, Star> realStarCache = new Dictionary<long, Star>();
    private Dictionary<string, long> realStarSeedMap = new Dictionary<string, long>(); // Map star names to their special seeds
    
    /// <summary>
    /// Get the real stellar data (for testing/debugging)
    /// </summary>
    public RealStellarData? GetRealStellarData() => realStellarData;
    
    // splitmix64 generator for one-off 64-bit samples
    private static ulong SplitMix64(ref ulong state)
    {
        state += 0x9E3779B97F4A7C15UL;
        ulong z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    
    /// <summary>
    /// Initialize the galaxy system with optional real stellar data
    /// </summary>
    public ChunkBasedGalaxySystem(bool loadRealData = true)
    {
        if (loadRealData)
        {
            realStellarData = new RealStellarData();
            // Try to load from files first, fall back to default data
            string dataPath = "stellar_data";

            // Try multiple possible paths
            string[] possiblePaths = {
                dataPath,
                Path.Combine("..", dataPath),
                Path.Combine("..", "..", dataPath),
                Path.Combine("ScientificMilkyWayVisual", dataPath),
                Path.GetFullPath(dataPath)
            };

            bool loaded = false;
            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    Console.WriteLine($"Found stellar data directory at: {path}");
                    realStellarData.LoadData(
                        Path.Combine(path, "stars.csv"),
                        Path.Combine(path, "planets.csv"),
                        Path.Combine(path, "moons.csv")
                    );
                    loaded = true;
                    break;
                }
            }

            if (!loaded)
            {
                Console.WriteLine("Stellar data directory not found. No real stars will be loaded.");
                Console.WriteLine("Searched paths:");
                foreach (var path in possiblePaths)
                {
                    Console.WriteLine($"  - {Path.GetFullPath(path)}");
                }
            }
            else
            {
                // Generate seeds for all real stars
                GenerateRealStarSeeds();
            }
        }
    }
    
    /// <summary>
    /// Chunk coordinate representation
    /// </summary>
    public class ChunkCoordinate
    {
        public int R { get; set; }      // 0-1499
        public int Theta { get; set; }  // 0-359
        public int Z { get; set; }      // -100 to 100
        
        public ChunkCoordinate(int r, int theta, int z)
        {
            if (r < 0 || r >= 1500)
                throw new ArgumentException($"ChunkCoordinate R must be 0-1999, got {r}");
            if (theta < 0 || theta >= 360)
                throw new ArgumentException($"ChunkCoordinate Theta must be 0-359, got {theta}");
            if (z < -100 || z > 100)
                throw new ArgumentException($"ChunkCoordinate Z must be -250 to 250, got {z}");
                
            R = r;
            Theta = theta;
            Z = z;
        }
        
        public ChunkCoordinate(string chunkId)
        {
            var parts = chunkId.Split('_');
            if (parts.Length != 3)
                throw new ArgumentException("Invalid chunk ID format. Use 'r_theta_z' format.");
                
            int r = int.Parse(parts[0]);
            int theta = int.Parse(parts[1]);
            int z = int.Parse(parts[2]);
            
            if (r < 0 || r >= 1500)
                throw new ArgumentException($"ChunkCoordinate R must be 0-1499, got {r}");
            if (theta < 0 || theta >= 360)
                throw new ArgumentException($"ChunkCoordinate Theta must be 0-359, got {theta}");
            if (z < -100 || z > 100)
                throw new ArgumentException($"ChunkCoordinate Z must be -100 to 100, got {z}");
                
            R = r;
            Theta = theta;
            Z = z;
        }
        
        public override string ToString() => $"{R}_{Theta}_{Z}";
        
        public (double rMin, double rMax, double thetaMin, double thetaMax, double zMin, double zMax) GetBounds()
        {
            double rMin = R * CHUNK_SIZE;
            double rMax = (R + 1) * CHUNK_SIZE;
            double thetaMin = Theta * Math.PI / 180.0;
            double thetaMax = (Theta + 1) * Math.PI / 180.0;
            double zMin = Z * CHUNK_SIZE;
            double zMax = (Z + 1) * CHUNK_SIZE;
            
            return (rMin, rMax, thetaMin, thetaMax, zMin, zMax);
        }
    }
    
    /// <summary>
    /// Encode chunk coordinates and star index into a seed
    /// </summary>
    public static long EncodeSeed(int chunkR, int chunkTheta, int chunkZ, long starIndex)
    {
        // Validate inputs
        if (chunkR < 0 || chunkR >= MAX_RADIUS_CHUNKS)
            throw new ArgumentException($"chunkR must be 0-{MAX_RADIUS_CHUNKS-1}");
        if (chunkTheta < 0 || chunkTheta >= MAX_ANGULAR_CHUNKS)
            throw new ArgumentException($"chunkTheta must be 0-{MAX_ANGULAR_CHUNKS-1}");
        if (chunkZ < -100 || chunkZ > 100)
            throw new ArgumentException("chunkZ must be -100 to 100");
        if (starIndex < 0)
            throw new ArgumentException($"starIndex must be >= 0, got {starIndex}");
            
        // Normalize chunkZ to 0-200 range
        int normalizedZ = chunkZ + 100;
        
        // Encode using bit shifting for efficient packing
        // New layout: R(11 bits) + Theta(9 bits) + Z(8 bits) + StarIndex(36 bits) = 64 bits total
        // This allows up to ~68 billion stars per chunk
        long seed = ((long)chunkR << 53) | ((long)chunkTheta << 44) | ((long)normalizedZ << 36) | (long)starIndex;
        
        return seed;
    }
    
    /// <summary>
    /// Decode a seed back to chunk coordinates and star index
    /// </summary>
    public static (int chunkR, int chunkTheta, int chunkZ, long starIndex) DecodeSeed(long seed)
    {
        // New layout: R(11 bits) + Theta(9 bits) + Z(8 bits) + StarIndex(36 bits) = 64 bits total
        int chunkR = (int)((seed >> 53) & 0x7FF); // 11 bits
        int chunkTheta = (int)((seed >> 44) & 0x1FF); // 9 bits
        int normalizedZ = (int)((seed >> 36) & 0xFF); // 8 bits
        long starIndex = (long)(seed & 0xFFFFFFFFF); // 36 bits
        
        int chunkZ = normalizedZ - 100; // Convert back to -100 to 100 range
        
        return (chunkR, chunkTheta, chunkZ, starIndex);
    }
    
private List<RoguePlanet> GenerateRoguePlanetsForChunk(ChunkCoordinate chunk)
{
    var roguePlanets = new List<RoguePlanet>();
    var bounds       = chunk.GetBounds();

    // 1) Compute center position for density
    float centerR     = (float)((bounds.rMin + bounds.rMax) * 0.5);
    float centerTheta = (float)((bounds.thetaMin + bounds.thetaMax) * 0.5);
    float centerZ     = (float)((bounds.zMin + bounds.zMax) * 0.5);
    float xC = centerR * (float)Math.Cos(centerTheta);
    float yC = centerR * (float)Math.Sin(centerTheta);
    var cartesianPos = new GalaxyGenerator.Vector3(xC, yC, centerZ);

    // 2) Evaluate density and expected count
    float rogueDensity = GalaxyGenerator.CalculateRoguePlanetDensity(cartesianPos);
    float chunkVolume  = (float)(
        (bounds.rMax - bounds.rMin)
      * (bounds.rMax * (bounds.thetaMax - bounds.thetaMin))
      * (bounds.zMax - bounds.zMin)
    );
    float expectedRogues = rogueDensity * chunkVolume;

    // 3) Deterministic count via SplitMix64
    ulong smStateCount = ((ulong)EncodeSeed(chunk.R, chunk.Theta, chunk.Z, 0))
                         ^ 0xDEADBEEFUL;
    int rogueCount;
    if (expectedRogues < 1f)
    {
        double u0     = (SplitMix64(ref smStateCount) >> 11) * (1.0 / (1UL << 53));
        rogueCount    = (u0 < expectedRogues) ? 1 : 0;
    }
    else
    {
        rogueCount    = (int)expectedRogues;
        double uFrac  = (SplitMix64(ref smStateCount) >> 11) * (1.0 / (1UL << 53));
        if (uFrac < (expectedRogues - rogueCount))
            rogueCount++;
    }

    // 4) Generate each rogue planet with the same PRNG style as stars
    for (int i = 0; i < rogueCount; i++)
    {
        long rogueSeed  = EncodeSeed(chunk.R, chunk.Theta, chunk.Z, (long)i | 0x800000000L);
        ulong smStatePos = (ulong)rogueSeed;

        // three uniforms in [0,1)
        double u1 = (SplitMix64(ref smStatePos) >> 11) * (1.0 / (1UL << 53));
        double u2 = (SplitMix64(ref smStatePos) >> 11) * (1.0 / (1UL << 53));
        double u3 = (SplitMix64(ref smStatePos) >> 11) * (1.0 / (1UL << 53));

        // --- here are the explicit casts of the whole RHS ---
        float rr     = (float)(bounds.rMin
                            + u1 * (bounds.rMax - bounds.rMin));
        float thetaF = (float)(bounds.thetaMin
                            + u2 * (bounds.thetaMax - bounds.thetaMin));
        float zz     = (float)(bounds.zMin
                            + u3 * (bounds.zMax - bounds.zMin));
        // --------------------------------------------------------

        var position = new GalaxyGenerator.Vector3(
            rr * (float)Math.Cos(thetaF),
            rr * (float)Math.Sin(thetaF),
            zz
        );

        var rogue = RoguePlanet.Generate(
            rogueSeed, position,
            chunk.R, chunk.Theta, chunk.Z,
            -(i + 1)
        );
        roguePlanets.Add(rogue);
    }

    return roguePlanets;
}

    
    /// <summary>
    /// Check if a seed refers to a rogue planet
    /// </summary>
    public static bool IsRoguePlanetSeed(long seed)
    {
        var (_, _, _, index) = DecodeSeed(seed);
        return (index & 0x800000000) != 0;
    }
    
    /// <summary>
    /// Check if a seed refers to a real star
    /// </summary>
    public static bool IsRealStarSeed(long seed)
    {
        // Real stars use bit 35 (the highest bit of the 36-bit index) as a marker
        var (_, _, _, index) = DecodeSeed(seed);
        return (index & 0x400000000) != 0 && (index & 0x800000000) == 0;
    }
    
    /// <summary>
    /// Generate special seeds for all real stars
    /// </summary>
    private void GenerateRealStarSeeds()
    {
        if (realStellarData == null) return;
        
        var allRealStars = realStellarData.GetAllStars();
        int catalogId = 0;
        
        foreach (var realStar in allRealStars)
        {
            // Skip companion stars that are already part of another star's system
            // Check if this star is listed as a companion of any other star
            bool isCompanion = false;
            foreach (var otherStar in allRealStars)
            {
                if (otherStar != realStar && otherStar.CompanionStars.Contains(realStar))
                {
                    isCompanion = true;
                    break;
                }
            }
            
            if (isCompanion)
            {
                // Don't generate a separate seed for companions
                catalogId++;
                continue;
            }
            
            // Find which chunk the star belongs to
            var cylindrical = CartesianToCylindrical(
                (float)realStar.X, 
                (float)realStar.Y, 
                (float)realStar.Z
            );
            
            int chunkR = (int)(cylindrical.r / CHUNK_SIZE);
            int chunkTheta = (int)(cylindrical.theta * 180 / Math.PI);
            int chunkZ = (int)(cylindrical.z / CHUNK_SIZE);
            
            // Clamp to valid ranges
            chunkR = Math.Max(0, Math.Min(MAX_RADIUS_CHUNKS - 1, chunkR));
            chunkTheta = Math.Max(0, Math.Min(MAX_ANGULAR_CHUNKS - 1, chunkTheta));
            chunkZ = Math.Max(-100, Math.Min(100, chunkZ));
            
            // Create special index for real star:
            // Bit 35 (0x400000000) marks it as real
            // Lower bits store catalog ID
            long realStarIndex = 0x400000000L | (long)catalogId;
            
            // Generate the seed
            long seed = EncodeSeed(chunkR, chunkTheta, chunkZ, realStarIndex);
            
            // Store mappings
            realStarSeedMap[realStar.SystemName] = seed;
            
            // Pre-convert and cache the star
            var star = ConvertRealStar(realStar, seed);
            star.IsRealStar = true;
            realStarCache[seed] = star;
            
            catalogId++;
        }
        
        Console.WriteLine($"Generated seeds for {catalogId} real stars");
    }
    
    /// <summary>
    /// Convert Cartesian to cylindrical coordinates
    /// </summary>
    private (float r, float theta, float z) CartesianToCylindrical(float x, float y, float z)
    {
        float r = (float)Math.Sqrt(x * x + y * y);
        float theta = (float)Math.Atan2(y, x);
        if (theta < 0) theta += 2 * (float)Math.PI;
        return (r, theta, z);
    }
    
    /// <summary>
    /// Get a real star's seed by its name
    /// </summary>
    public long? GetRealStarSeedByName(string starName)
    {
        // First check if this is a companion star name
        // If searching for a companion (e.g., "Procyon B"), redirect to primary star
        if (realStellarData != null)
        {
            var allStars = realStellarData.GetAllStars();
            var searchedStar = allStars.FirstOrDefault(s => 
                s.SystemName.Equals(starName, StringComparison.OrdinalIgnoreCase) ||
                s.SystemName.StartsWith(starName, StringComparison.OrdinalIgnoreCase));
                
            if (searchedStar != null)
            {
                // Check if this star is a companion of another star
                foreach (var otherStar in allStars)
                {
                    if (otherStar.CompanionStars.Contains(searchedStar))
                    {
                        // Redirect to the primary star
                        starName = otherStar.SystemName;
                        break;
                    }
                }
            }
        }
        
        // First try exact match
        if (realStarSeedMap.TryGetValue(starName, out var seed))
        {
            return seed;
        }
        
        // Try case-insensitive exact match
        var key = realStarSeedMap.Keys.FirstOrDefault(k => k.Equals(starName, StringComparison.OrdinalIgnoreCase));
        if (key != null)
        {
            return realStarSeedMap[key];
        }
        
        // Try partial match (e.g., "Procyon" matches "Procyon A")
        key = realStarSeedMap.Keys.FirstOrDefault(k => k.StartsWith(starName, StringComparison.OrdinalIgnoreCase));
        if (key != null)
        {
            return realStarSeedMap[key];
        }
        
        // Try if the input is contained in any star name
        key = realStarSeedMap.Keys.FirstOrDefault(k => k.IndexOf(starName, StringComparison.OrdinalIgnoreCase) >= 0);
        if (key != null)
        {
            return realStarSeedMap[key];
        }
        
        return null;
    }
    
    /// <summary>
    /// List all real star names and their seeds
    /// </summary>
    public void ListRealStars()
    {
        if (realStarSeedMap.Count == 0)
        {
            Console.WriteLine("No real stars loaded.");
            return;
        }
        
        Console.WriteLine($"\nReal stars ({realStarSeedMap.Count} total):");
        foreach (var kvp in realStarSeedMap.OrderBy(k => k.Key))
        {
            var star = realStarCache[kvp.Value];
            var (chunkR, chunkTheta, chunkZ, _) = DecodeSeed(kvp.Value);
            Console.WriteLine($"  {kvp.Key}: seed={kvp.Value}, chunk={chunkR}_{chunkTheta}_{chunkZ}, type={star.Type}");
        }
    }
    
    /// <summary>
    /// Get a star by its seed
    /// </summary>
    public Star GetStarBySeed(long seed)
    {
        // Special case for Sagittarius A*
        if (seed == 0)
        {
            var sgrA = new Star
            {
                Seed = 0,
                Position = GalaxyGenerator.Vector3.Zero,
                Type = StellarTypeGenerator.StellarType.SMBH,
                Mass = 4310000f,
                Temperature = 0f,
                Luminosity = 0f,
                Color = GalaxyGenerator.Vector3.Zero,
                Population = "Bulge",
                Region = "Galactic Center",
                PlanetCount = 0,
                IsMultiple = false,
                SystemName = "Sagittarius A*"
            };
            return sgrA;
        }
        
        // Check if it's a real star seed
        if (IsRealStarSeed(seed))
        {
            if (realStarCache.TryGetValue(seed, out var realStar))
            {
                return realStar;
            }
            else
            {
                throw new ArgumentException($"Real star with seed {seed} not found in cache");
            }
        }
        
        // Decode the seed
        var (chunkR, chunkTheta, chunkZ, starIndex) = DecodeSeed(seed);
        
        // Check if this is a rogue planet (negative index)
        if ((starIndex & 0x800000000) != 0)
        {
            throw new ArgumentException($"Seed {seed} refers to a rogue planet, not a star. Use GetRoguePlanetBySeed instead.");
        }
        
        // Get chunk bounds
        var chunk = new ChunkCoordinate(chunkR, chunkTheta, chunkZ);
        var bounds = chunk.GetBounds();
        
        // Calculate expected stars in this chunk
        int expectedStars = CalculateExpectedStars(chunk);
        
        // If star index exceeds expected stars, it doesn't exist
        if (starIndex >= expectedStars)
        {
            return null;
        }
        
                // replace whatever x1/x2 rounds you had with:
        ulong smState = (ulong)seed;
        double u1 = (SplitMix64(ref smState) >> 11) * (1.0 / (1UL << 53));
        double u2 = (SplitMix64(ref smState) >> 11) * (1.0 / (1UL << 53));
        double u3 = (SplitMix64(ref smState) >> 11) * (1.0 / (1UL << 53));

        // then map into chunk bounds as before:
        double r     = bounds.rMin     + u1 * (bounds.rMax - bounds.rMin);
        double theta = bounds.thetaMin + u2 * (bounds.thetaMax - bounds.thetaMin);
        double z     = bounds.zMin     + u3 * (bounds.zMax - bounds.zMin);

        // Convert to Cartesian
        float x = (float)(r * Math.Cos(theta));
        float y = (float)(r * Math.Sin(theta));
        var position = new GalaxyGenerator.Vector3(x, y, (float)z);
        
        // Check if position is within 80 ly of Sol (moved to 26550 ly, 0.5 degrees)
        const float SOL_R = 26550f;
        const float SOL_THETA_DEG = 0.5f;
        const float SOL_Z = 0f;
        
        // Calculate Sol's position in Cartesian coordinates
        float solTheta = SOL_THETA_DEG * (float)Math.PI / 180f;
        float solX = SOL_R * (float)Math.Cos(solTheta);
        float solY = SOL_R * (float)Math.Sin(solTheta);
        
        float dx = position.X - solX;
        float dy = position.Y - solY;
        float dz = position.Z - SOL_Z;
        float distanceFromSol = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

        // exclude any procedurals within 80 ly of Sol
        double p = 0.0;
        if (distanceFromSol <= 20f) p = 1.0;
        else if (distanceFromSol <= 40f) p = 0.67;
        else if (distanceFromSol <= 60f) p = 0.25;
        else if (distanceFromSol <= 80f) p = 0.1;

        if (p > 0.0)
        {
            double u = (SplitMix64(ref smState) >> 11) * (1.0 / (1UL << 53));
            if (u < p)
                return null;
        }

        
        // Generate star using unified system
            var star = Star.GenerateAtPosition(position, seed);
        
        return star;
    }
    
    /// <summary>
    /// Get a rogue planet by its seed
    /// </summary>
    public RoguePlanet GetRoguePlanetBySeed(long seed)
    {
        // Decode the seed
        var (chunkR, chunkTheta, chunkZ, index) = DecodeSeed(seed);
        
        // Check if this is actually a star
        if ((index & 0x800000000) == 0)
        {
            throw new ArgumentException($"Seed {seed} refers to a star, not a rogue planet. Use GetStarBySeed instead.");
        }
        
        // Get the actual rogue planet index
        long rogueIndex = index & 0x7FFFFFFFF;
        
        // Get chunk
        var chunk = new ChunkCoordinate(chunkR, chunkTheta, chunkZ);
        
        // First, we need to determine how many rogue planets are actually in this chunk
        var roguesInChunk = GenerateRoguePlanetsForChunk(chunk);
        int rogueCount = roguesInChunk.Count;
        
        // Check if the index is within bounds
        if (rogueIndex >= rogueCount)
        {
            throw new ArgumentException($"Rogue planet index {rogueIndex} exceeds the count of {rogueCount} rogue planets in chunk {chunk}");
        }
        
        // Return the actual rogue planet from the generated list
        return roguesInChunk[(int)rogueIndex];
    }
    
    /// <summary>
    /// Calculate expected number of stars in a chunk based on density
    /// </summary>
    private int CalculateExpectedStars(ChunkCoordinate chunk)
    {
        var bounds = chunk.GetBounds();
        
        // Calculate chunk volume (cylindrical wedge)
        double deltaTheta = bounds.thetaMax - bounds.thetaMin;
        double avgRadius = (bounds.rMin + bounds.rMax) / 2;
        double deltaR = bounds.rMax - bounds.rMin;
        double deltaZ = bounds.zMax - bounds.zMin;
        double volume = avgRadius * deltaR * deltaTheta * deltaZ;
        
        // Sample density at multiple points for better accuracy
        double totalStarCount = 0;
        int samples = 0;
        
        for (int i = 0; i < 3; i++)
        {
            double r = bounds.rMin + (bounds.rMax - bounds.rMin) * (i + 0.5) / 3;
            for (int j = 0; j < 3; j++)
            {
                double theta = bounds.thetaMin + (bounds.thetaMax - bounds.thetaMin) * (j + 0.5) / 3;
                for (int k = 0; k < 3; k++)
                {
                    double z = bounds.zMin + (bounds.zMax - bounds.zMin) * (k + 0.5) / 3;
                    
                    // Convert to Cartesian for density calculation
                    float x = (float)(r * Math.Cos(theta));
                    float y = (float)(r * Math.Sin(theta));
                    
                    // Use unified GalaxyGenerator for stellar density
                    var galaxyPos = new GalaxyGenerator.Vector3(x, y, (float)z);
                    var stellarDensity = GalaxyGenerator.GetExpectedStarDensity(galaxyPos);
                    
                    // Calculate small volume around sample point
                    // This gives us stars per sample volume
                    var sampleVolume = volume / 27.0; // 3x3x3 = 27 samples
                    var starsInSample = stellarDensity * sampleVolume;
                    
                    totalStarCount += starsInSample;
                    samples++;
                }
            }
        }
        
        // Total expected stars is sum of all samples
        int expectedStars = Math.Max(0, (int)Math.Round(totalStarCount));
        
        // No cap - let density determine star count
        return expectedStars;
    }
    
    /// <summary>
    /// Generate all stars in a chunk - SUPER FAST!
    /// </summary>
    public List<Star> GenerateChunkStars(string chunkId)
{
    var chunk = new ChunkCoordinate(chunkId);
    int expectedStars = CalculateExpectedStars(chunk);
    var stars = new List<Star>(expectedStars);

    // 1) Any fixed special objects (Sgr A*, etc.)
    var bounds = chunk.GetBounds();
    var special = GalacticAnalytics
        .GetSpecialObjectsInChunk(
            bounds.rMin, bounds.rMax,
            bounds.thetaMin, bounds.thetaMax,
            bounds.zMin, bounds.zMax);
    stars.AddRange(special);

    // 2) Procedural stars, exactly expectedStars attempts, skipping nulls
    for (int i = 0; i < expectedStars; i++)
    {
        long seed = EncodeSeed(chunk.R, chunk.Theta, chunk.Z, i);
        Star s = GetStarBySeed(seed);
        if (s != null)
            stars.Add(s);
        // else it was inside 80 ly of Sol, so we skip it
    }

    // 3) Now tack on your real‐data stars (fills the 80 ly bubble)
    GenerateRealStarsInChunk(chunk, ref stars);

    return stars;
}


    
    /// <summary>
    /// Generate all objects in a chunk (stars and optionally rogue planets)
    /// </summary>
    public (List<Star> stars, List<RoguePlanet>? roguePlanets) GenerateChunkObjects(string chunkId, bool includeRoguePlanets = false)
    {
        var stars = GenerateChunkStars(chunkId);
        
        List<RoguePlanet>? roguePlanets = null;
        if (includeRoguePlanets)
        {
            var chunk = new ChunkCoordinate(chunkId);
            roguePlanets = GenerateRoguePlanetsForChunk(chunk);
        }
        
        return (stars, roguePlanets);
    }
    
    /// <summary>
    /// Find which chunk a position belongs to
    /// </summary>
    public ChunkCoordinate GetChunkForPosition(float x, float y, float z)
    {
        double r = Math.Sqrt(x * x + y * y);
        double theta = Math.Atan2(y, x) * 180 / Math.PI;
        if (theta < 0) theta += 360;
        
        int rIndex = Math.Min((int)(r / CHUNK_SIZE), MAX_RADIUS_CHUNKS - 1);
        int thetaIndex = (int)(theta);
        int zIndex = Math.Max(-50, Math.Min(50, (int)(z / CHUNK_SIZE)));
        
        return new ChunkCoordinate(rIndex, thetaIndex, zIndex);
    }
    
    /// <summary>
    /// Investigate a chunk and export to CSV
    /// </summary>
    public void InvestigateChunk(string chunkId, string? outputPath = null, bool includeRoguePlanets = false)
{
    var chunk = new ChunkCoordinate(chunkId);
    var bounds = chunk.GetBounds();
    
    Console.WriteLine($"\n=== Investigating Chunk {chunkId} ===");
    Console.WriteLine($"Radial range: {bounds.rMin:F0} - {bounds.rMax:F0} ly");
    Console.WriteLine($"Angular range: {bounds.thetaMin * 180 / Math.PI:F0}° - {bounds.thetaMax * 180 / Math.PI:F0}°");
    Console.WriteLine($"Vertical range: {bounds.zMin:F0} - {bounds.zMax:F0} ly");
    
    // Calculate chunk volume
    var deltaTheta = bounds.thetaMax - bounds.thetaMin;
    var avgRadius = (bounds.rMin + bounds.rMax) / 2;
    var deltaR = bounds.rMax - bounds.rMin;
    var deltaZ = bounds.zMax - bounds.zMin;
    var volume = avgRadius * deltaR * deltaTheta * deltaZ;
    Console.WriteLine($"Chunk volume: {volume:E2} ly³");
    
    // Calculate expected density at chunk center
    var centerR = (bounds.rMin + bounds.rMax) / 2;
    var centerTheta = (bounds.thetaMin + bounds.thetaMax) / 2;
    var centerZ = (bounds.zMin + bounds.zMax) / 2;
    var centerX = (float)(centerR * Math.Cos(centerTheta));
    var centerY = (float)(centerR * Math.Sin(centerTheta));
    var centerPos = new GalaxyGenerator.Vector3(centerX, centerY, (float)centerZ);
    var expectedDensity = GalaxyGenerator.GetExpectedStarDensity(centerPos);
    Console.WriteLine($"Expected density (from formula): {expectedDensity:E3} stars/ly³");
    
    // Generate stars - THIS IS FAST NOW!
    var startTime = DateTime.Now;
    var stars = new List<Star>();
    int suppressedCount = 0;
    
    // First add special objects (like Sgr A*)
    var specialObjects = GalacticAnalytics.GetSpecialObjectsInChunk(
        bounds.rMin, bounds.rMax, bounds.thetaMin, bounds.thetaMax, bounds.zMin, bounds.zMax);
    stars.AddRange(specialObjects);
    
    // Calculate expected stars
    int expectedStars = CalculateExpectedStars(chunk);
    
    // Generate all stars in chunk - count suppressed ones
    for (int i = 0; i < expectedStars; i++)
    {
        long seed = EncodeSeed(chunk.R, chunk.Theta, chunk.Z, i);
        try
        {
            var star = GetStarBySeed(seed);
            if (star != null)
            {
                stars.Add(star);
            }
            else
            {
                // Suppressed by real-data rules
                suppressedCount++;
            }
        }
        catch (ArgumentException ex)
        {
            // Count suppressed stars within Sol bubble
            if (ex.Message.Contains("suppressed for real stellar data"))
            {
                suppressedCount++;
                continue;
            }
            else
            {
                throw; // Re-throw other exceptions
            }
        }
    }
    
    var elapsed = (DateTime.Now - startTime).TotalSeconds;
    
    // Add real stars if in Sol bubble
    int realStarsBefore = stars.Count;
    GenerateRealStarsInChunk(chunk, ref stars);
    int realStarsAdded = stars.Count - realStarsBefore;
    if (realStarsAdded > 0)
    {
        Console.WriteLine($"Real stars added to chunk: {realStarsAdded}");
    }
    
    // Fill gaps with procedural stars if we're in the Sol bubble but have too few stars
    if (suppressedCount > 0)
    {
        Console.WriteLine($"Filling gaps: have {stars.Count} stars, expected ~{expectedStars}");
    }
    
    Console.WriteLine($"Stars in chunk: {stars.Count} (generated in {elapsed:F2}s)");
    if (suppressedCount > 0)
    {
        Console.WriteLine($"Stars suppressed (Sol bubble): {suppressedCount}");
        Console.WriteLine($"  Note: These {suppressedCount} positions are reserved for real stellar data");
    }
    
    // Calculate actual density
    var actualDensity = stars.Count / volume;
    var densityRatio = actualDensity / expectedDensity;
    Console.WriteLine($"Actual density: {actualDensity:E3} stars/ly³");
    Console.WriteLine($"Density ratio (actual/expected): {densityRatio:F2}");
    Console.WriteLine($"Region: {GalaxyGenerator.DetermineRegion(centerPos)}");
    
    // Generate rogue planets if requested
    List<RoguePlanet>? roguePlanets = null;
    if (includeRoguePlanets)
    {
        roguePlanets = GenerateRoguePlanetsForChunk(chunk);
        Console.WriteLine($"\nRogue planets in chunk: {roguePlanets.Count}");
        
        if (roguePlanets.Count > 0)
        {
            var rogueTypes = roguePlanets.GroupBy(r => r.Type).OrderByDescending(g => g.Count());
            Console.WriteLine("Rogue planet types:");
            foreach (var group in rogueTypes)
            {
                Console.WriteLine($"  {group.Key}: {group.Count()} ({group.Count() * 100.0 / roguePlanets.Count:F1}%)");
            }
            
            // Rogue-to-star ratio
            Console.WriteLine($"Rogue-to-star ratio: 1:{(stars.Count > 0 ? stars.Count / (double)Math.Max(1, roguePlanets.Count) : 0):F1}");
        }
    }
    
    // Statistics
    if (stars.Count > 0)
    {
        var typeGroups = stars.GroupBy(s => s.Type).OrderByDescending(g => g.Count());
        Console.WriteLine("\nStellar types:");
        foreach (var group in typeGroups)
        {
            string typeDisplay = group.Key.ToString();
            Console.WriteLine($"  {typeDisplay}: {group.Count()} ({group.Count() * 100.0 / stars.Count:F1}%)");
        }
    }
    
    // Export to CSV
    if (outputPath == null)
    {
        outputPath = includeRoguePlanets ? $"chunk_{chunkId}_with_rogues_data.csv" : $"chunk_{chunkId}_data.csv";
    }
    
    using (var writer = new StreamWriter(outputPath))
    {
        if (includeRoguePlanets && roguePlanets != null && roguePlanets.Count > 0)
        {
            writer.WriteLine("ChunkID,ObjectType,Index,Seed,X,Y,Z,R,Theta,Type,Mass,Radius,Temperature,Luminosity,ColorR,ColorG,ColorB,Population,Region,Planets,IsMultiple,SystemName,MoonCount,Origin,IsRealStar");
            
            // Write stars
            foreach (var star in stars)
            {
                var r = Math.Sqrt(star.Position.X * star.Position.X + star.Position.Y * star.Position.Y);
                var theta = Math.Atan2(star.Position.Y, star.Position.X) * 180 / Math.PI;
                if (theta < 0) theta += 360;
                
                long starIndex;
                if (IsRealStarSeed(star.Seed))
                {
                    var (_, _, _, idx) = DecodeSeed(star.Seed);
                    starIndex = idx & 0x3FFFFFFF; // Remove the real star bit
                }
                else
                {
                    var (_, _, _, idx) = DecodeSeed(star.Seed);
                    starIndex = idx;
                }
                
                writer.WriteLine($"{chunk},Star,{starIndex},{star.Seed},{star.Position.X:F2},{star.Position.Y:F2},{star.Position.Z:F2}," +
                    $"{r:F2},{theta:F2},{star.Type},{star.Mass:F3},0,{star.Temperature:F0},{star.Luminosity:F4}," +
                    $"{star.Color.X:F3},{star.Color.Y:F3},{star.Color.Z:F3},{star.Population},{star.Region},{star.PlanetCount}," +
                    $"{star.IsMultiple},{star.SystemName},,{star.IsRealStar}");
            }
            
            // Write rogue planets
            foreach (var rogue in roguePlanets)
            {
                var r = Math.Sqrt(rogue.Position.X * rogue.Position.X + rogue.Position.Y * rogue.Position.Y);
                var theta = Math.Atan2(rogue.Position.Y, rogue.Position.X) * 180 / Math.PI;
                if (theta < 0) theta += 360;
                
                writer.WriteLine($"{chunk},RoguePlanet,{rogue.Index},{rogue.Seed},{rogue.Position.X:F2},{rogue.Position.Y:F2},{rogue.Position.Z:F2}," +
                    $"{r:F2},{theta:F2},{rogue.Type},{rogue.Mass:F3},{rogue.Radius:F1},{rogue.Temperature:F0},0," +
                    $"0,0,0,,,0," +
                    $"false,Rogue,{rogue.MoonCount},{rogue.Origin},false");
            }
        }
        else
        {
            writer.WriteLine("ChunkID,Seed,X,Y,Z,R,Theta,Type,Mass,Temperature,Luminosity,ColorR,ColorG,ColorB,Population,Region,Planets,IsMultiple,SystemName,IsRealStar");
            
            foreach (var star in stars)
            {
                var r = Math.Sqrt(star.Position.X * star.Position.X + star.Position.Y * star.Position.Y);
                var theta = Math.Atan2(star.Position.Y, star.Position.X) * 180 / Math.PI;
                if (theta < 0) theta += 360;
                
                writer.WriteLine($"{chunk},{star.Seed},{star.Position.X:F2},{star.Position.Y:F2},{star.Position.Z:F2}," +
                    $"{r:F2},{theta:F2},{star.Type},{star.Mass:F3},{star.Temperature:F0},{star.Luminosity:F4}," +
                    $"{star.Color.X:F3},{star.Color.Y:F3},{star.Color.Z:F3},{star.Population},{star.Region},{star.PlanetCount}," +
                    $"{star.IsMultiple},{star.SystemName},{star.IsRealStar}");
            }
        }
    }
    
    Console.WriteLine($"Data exported to: {outputPath}");
}

    
    
    /// <summary>
    /// Estimate total star count using density formula integration
    /// </summary>
    public void EstimateTotalStarCount()
    {
        Console.WriteLine("\n=== Estimating Total Galaxy Star Count ===");
        Console.WriteLine("Using density formula integration...\n");
        
        // First check some key density values
        Console.WriteLine("Sample density values:");
        var testPoints = new[] {
            (0.0f, 0.0f, 0.0f, "Galactic center"),
            (100.0f, 0.0f, 0.0f, "100 ly from center"),
            (500.0f, 0.0f, 0.0f, "500 ly from center"),
            (1000.0f, 0.0f, 0.0f, "1000 ly from center"),
            (2500.0f, 0.0f, 0.0f, "2500 ly from center"),
            (5000.0f, 0.0f, 0.0f, "5000 ly from center"),
            (8000.0f, 0.0f, 0.0f, "Inner disk"),
            (26000.0f, 0.0f, 0.0f, "Solar neighborhood (actual Sun position)"),
            (50000.0f, 0.0f, 0.0f, "Outer disk")
        };
        
        foreach (var (x, y, z, desc) in testPoints)
        {
            var pos = new GalaxyGenerator.Vector3(x, y, z);
            var density = GalaxyGenerator.GetExpectedStarDensity(pos);
            var rogueDensity = GalaxyGenerator.CalculateRoguePlanetDensity(pos);
            Console.WriteLine($"  {desc}: {density:E3} stars/ly³, {rogueDensity:E3} rogues/ly³");
        }
        Console.WriteLine();
        
        var startTime = DateTime.Now;
        double totalStars = 0;
        double totalRogues = 0;
        
        // Integration parameters - sample points for numerical integration
        // Use logarithmic sampling in radius to better capture high density regions
        int rSamples = 500;     // Radial samples
        int thetaSamples = 50;  // Angular samples (full circle)
        int zSamples = 100;     // Vertical samples
        
        double minRadius = 10;      // Start at 10 ly to avoid singularity
        double maxRadius = 150000;  // Maximum radius in light years
        double logMin = Math.Log10(minRadius);
        double logMax = Math.Log10(maxRadius);
        double dlogr = (logMax - logMin) / rSamples;
        double dtheta = 2 * Math.PI / thetaSamples;
        double maxZ = 10000.0;
        double dz = 2 * maxZ / zSamples;
        
        // Perform numerical integration
        for (int i = 0; i < rSamples; i++)
        {
            // Use logarithmic sampling
            double logr = logMin + (i + 0.5) * dlogr;
            double r = Math.Pow(10, logr);
            double r_next = Math.Pow(10, logr + dlogr);
            double dr = r_next - r; // Width of this radial shell
            
            double ringDensity = 0;
            double ringRogues = 0;
            
            for (int j = 0; j < thetaSamples; j++)
            {
                double theta = (j + 0.5) * dtheta;
                
                for (int k = 0; k < zSamples; k++)
                {
                    double z = -maxZ + (k + 0.5) * dz;
                    
                    // Convert to cartesian
                    float x = (float)(r * Math.Cos(theta));
                    float y = (float)(r * Math.Sin(theta));
                    var pos = new GalaxyGenerator.Vector3(x, y, (float)z);
                    
                    // Get density at this point
                    double density = GalaxyGenerator.GetExpectedStarDensity(pos);
                    double rogueDensity = GalaxyGenerator.CalculateRoguePlanetDensity(pos); // No scale factor needed
                    
                    // Volume element in cylindrical coordinates
                    double dV = r * dr * dtheta * dz;
                    
                    ringDensity += density * dV;
                    ringRogues += rogueDensity * dV;
                }
            }
            
            totalStars += ringDensity;
            totalRogues += ringRogues;
            
            // Progress update
            if (i % 50 == 0 && i > 0)
            {
                var elapsed = (DateTime.Now - startTime).TotalSeconds;
                Console.WriteLine($"Progress: {i}/{rSamples} ({i * 100.0 / rSamples:F1}%), " +
                    $"r={r:F0} ly, Running total: {totalStars:E2} stars, {totalRogues:E2} rogues");
            }
        }
        
        var totalTime = (DateTime.Now - startTime).TotalSeconds;
        
        Console.WriteLine($"\n=== Integration Complete ===");
        Console.WriteLine($"Integration grid: {rSamples} x {thetaSamples} x {zSamples} = {rSamples * thetaSamples * zSamples:N0} samples");
        Console.WriteLine($"Radial sampling: logarithmic from {minRadius} to {maxRadius} ly");
        Console.WriteLine($"Integration time: {totalTime:F1} seconds");
        Console.WriteLine($"\nEstimated total stars in galaxy: {totalStars:E2} ({totalStars:N0})");
        Console.WriteLine($"Estimated total rogue planets: {totalRogues:E2} ({totalRogues:N0})");
        
        // Compare to expected 225 billion
        double ratio = totalStars / 225e9;
        Console.WriteLine($"\nRatio to 225 billion target: {ratio:F2}x");
        Console.WriteLine($"Rogue planet to star ratio: {totalRogues / totalStars:F2}:1");
    }
    
    /// <summary>
    /// Convert a real star to our Star type
    /// </summary>
    private Star ConvertRealStar(RealStellarData.RealStar realStar, long seed)
    {
        // Use the pre-assigned seed for this real star if it exists
        long actualSeed = seed;
        if (realStarSeedMap.TryGetValue(realStar.SystemName, out var mappedSeed))
        {
            actualSeed = mappedSeed;
        }
        
        var star = new Star
        {
            Seed = actualSeed,
            Position = new GalaxyGenerator.Vector3((float)realStar.X, (float)realStar.Y, (float)realStar.Z),
            Type = RealStellarData.ConvertSpectralType(realStar.Type),
            Mass = (float)realStar.Mass,
            Temperature = (float)realStar.Temperature,
            Luminosity = (float)realStar.Luminosity,
            SystemName = realStar.SystemName,
            PlanetCount = realStar.PlanetCount,
            IsMultiple = realStar.IsMultiple,
            IsRealStar = true,
            RealStarData = realStar  // Store reference to real star data for planet/companion access
        };
        
        // Calculate color from temperature
        star.Color = CalculateStarColor(star.Temperature);
        
        // Set population and region
        var galPos = star.Position;
        var pop = GalaxyGenerator.DeterminePopulation(galPos);
        star.Population = pop.ToString();
        star.Region = GalaxyGenerator.DetermineRegion(galPos);
        
        return star;
    }
    
    /// <summary>
    /// Calculate star color from temperature
    /// </summary>
    private GalaxyGenerator.Vector3 CalculateStarColor(float temperature)
    {
        // Simplified blackbody color calculation
        float r, g, b;
        
        if (temperature < 3700) // M class - red
        {
            r = 1.0f;
            g = 0.3f;
            b = 0.0f;
        }
        else if (temperature < 5200) // K class - orange
        {
            r = 1.0f;
            g = 0.6f;
            b = 0.2f;
        }
        else if (temperature < 6000) // G class - yellow
        {
            r = 1.0f;
            g = 0.9f;
            b = 0.7f;
        }
        else if (temperature < 7500) // F class - yellow-white
        {
            r = 1.0f;
            g = 0.95f;
            b = 0.9f;
        }
        else if (temperature < 10000) // A class - white
        {
            r = 0.9f;
            g = 0.9f;
            b = 1.0f;
        }
        else if (temperature < 30000) // B class - blue-white
        {
            r = 0.7f;
            g = 0.8f;
            b = 1.0f;
        }
        else // O class - blue
        {
            r = 0.6f;
            g = 0.7f;
            b = 1.0f;
        }
        
        return new GalaxyGenerator.Vector3(r, g, b);
    }
    
    /// <summary>
    /// Generate real stars in the Sol bubble to fill unused positions
    /// </summary>
    public void GenerateRealStarsInChunk(ChunkCoordinate chunk, ref List<Star> stars)
    {
        if (realStellarData == null) return;
        
        var bounds = chunk.GetBounds();
        
        // Get chunk center in Cartesian
        var centerR = (float)((bounds.rMin + bounds.rMax) / 2);
        var centerTheta = (float)((bounds.thetaMin + bounds.thetaMax) / 2);
        var centerZ = (float)((bounds.zMin + bounds.zMax) / 2);
        float centerX = centerR * (float)Math.Cos(centerTheta);
        float centerY = centerR * (float)Math.Sin(centerTheta);
        
        // Check if this chunk could contain the Sol bubble
        const float SOL_R = 26550f;
        const float SOL_THETA_DEG = 0.5f;
        const float SOL_Z = 0f; 
        
        float solTheta = SOL_THETA_DEG * (float)Math.PI / 180f;
        float solX = SOL_R * (float)Math.Cos(solTheta);
        float solY = SOL_R * (float)Math.Sin(solTheta);
        
        float dx = centerX - solX;
        float dy = centerY - solY;
        float dz = centerZ - SOL_Z;
        float distanceFromSol = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        
        // Only process chunks that overlap with the Sol bubble
        // The chunk diagonal is about 100 ly, so check if any part of chunk could overlap
        
        // Get all real stars in this chunk's bounds
        var realStarsInBounds = realStellarData.GetAllStars().Where(rs =>
        {
            // Convert to cylindrical to check bounds
            double r = Math.Sqrt(rs.X * rs.X + rs.Y * rs.Y);
            double theta = Math.Atan2(rs.Y, rs.X);
            
            return r >= bounds.rMin && r <= bounds.rMax &&
                   theta >= bounds.thetaMin && theta <= bounds.thetaMax &&
                   rs.Z >= bounds.zMin && rs.Z <= bounds.zMax;
        }).ToList();
        
        // Add each real star using their pre-generated seeds
        foreach (var realStar in realStarsInBounds)
        {
            // Get the pre-generated seed for this real star
            if (realStarSeedMap.TryGetValue(realStar.SystemName, out var seed))
            {
                // Check if we already have this star cached
                if (!realStarCache.TryGetValue(seed, out var star))
                {
                    star = ConvertRealStar(realStar, seed);
                    realStarCache[seed] = star;
                }
                stars.Add(star);
            }
            else
            {
                Console.WriteLine($"Warning: Real star {realStar.SystemName} has no pre-generated seed!");
            }
        }
        
        if (realStarsInBounds.Count > 0)
        {
            Console.WriteLine($"  Found {realStarsInBounds.Count} real stars in chunk {chunk}");
            foreach (var rs in realStarsInBounds.Take(5)) // Show first 5
            {
                Console.WriteLine($"    - {rs.SystemName} at ({rs.X:F1}, {rs.Y:F1}, {rs.Z:F1})");
            }
        }
    }
    
    /// <summary>
    /// Fill gaps between real stars with procedural stars
    /// </summary>
}