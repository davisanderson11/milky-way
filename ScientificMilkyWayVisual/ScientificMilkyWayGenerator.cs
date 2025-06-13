using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Scientifically accurate Milky Way Galaxy Generator based on latest astronomical data
/// Incorporates real density distributions, spiral arm structure, and stellar populations
/// </summary>
public class ScientificMilkyWayGenerator
{
    // Galaxy Structure Parameters (based on 2024 data)
    private const long TOTAL_STARS = 100_000_000_000L; // 100 billion stars
    private const float GALAXY_RADIUS = 60_000f; // light years
    private const float BULGE_RADIUS = 10_000f; // Central bulge/bar radius
    private const float BULGE_SCALE_HEIGHT = 2_000f; // Bulge vertical scale
    private const float BAR_LENGTH = 10_000f; // Central bar full length
    private const float BAR_WIDTH = 3_000f; // Bar width
    private const float BAR_ANGLE = 0.44f; // ~25 degrees from x-axis
    
    // Disk Parameters (exponential disk model)
    private const float DISK_SCALE_RADIUS = 3_500f; // Radial scale length
    private const float THIN_DISK_HEIGHT = 300f; // Thin disk scale height
    private const float THICK_DISK_HEIGHT = 900f; // Thick disk scale height
    private const float DISK_CUTOFF_RADIUS = 50_000f; // Where disk drops off
    
    // Halo Parameters (NFW profile)
    private const float HALO_SCALE_RADIUS = 20_000f; // NFW scale radius
    private const float HALO_DENSITY_0 = 0.002f; // Central halo density (relative)
    
    // Spiral Arms (2 major + 2 minor arms)
    private const int NUM_MAJOR_ARMS = 2; // Perseus and Scutum-Centaurus
    private const int NUM_MINOR_ARMS = 2; // Sagittarius and Norma
    private const float SPIRAL_PITCH_ANGLE = 0.22f; // ~12.5 degrees
    private const float ARM_WIDTH = 3_000f; // Width of spiral arms
    
    // Solar position
    private const float SUN_DISTANCE = 26_000f; // Distance from galactic center
    private const float SUN_HEIGHT = 20f; // Height above galactic plane
    
    private Random _random;
    
    public enum StellarType
    {
        // Main sequence stars (Luminosity class V)
        O5V, B0V, B5V, A0V, A5V, F0V, F5V, G0V, G5V, K0V, K5V, M0V, M5V, M8V,
        
        // Giants (Luminosity class III)
        G5III, K0III, K5III, M0III, // Red giants
        B0III, // Blue giant
        
        // Supergiants (Luminosity class I)
        M2I, // Red supergiant
        B0I, // Blue supergiant
        
        // Compact objects and stellar remnants
        DA, // White dwarf (DA = hydrogen atmosphere)
        NS, // Neutron star/Pulsar
        BH, // Stellar-mass black hole
        SMBH // Supermassive black hole (special case for Sgr A*)
    }
    
    public enum StellarPopulation
    {
        PopIII,     // First generation stars, metal-free
        PopII,      // Old, metal-poor stars
        ThinDisk,   // Young, metal-rich stars
        ThickDisk,  // Intermediate age and metallicity
        Halo,       // Very old, metal-poor stars in the halo
        Bulge       // Old stars in the galactic bulge
    }
    
    public struct Vector3
    {
        public float X, Y, Z;
        
        public Vector3(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }
        
        public float Length()
        {
            return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        }
        
        public float Length2D()
        {
            return (float)Math.Sqrt(X * X + Y * Y);
        }
        
        public static Vector3 Zero => new Vector3(0, 0, 0);
    }
    
    public struct Star
    {
        public Vector3 Position;
        public StellarType Type;
        public float Mass;
        public float Temperature;
        public Vector3 Color;
        public float Luminosity;
        public long Seed;
        public StellarPopulation Population;
        public string Region; // Additional region information
        public int PlanetCount; // Number of planets in the system
        public bool IsMultiple; // Is this star part of a multiple star system?
        public string SystemName; // Name in multiple star system (e.g., "12345-B")
    }
    
    public ScientificMilkyWayGenerator()
    {
        _random = new Random();
    }
    
    public Star GenerateStarAtSeed(long seed)
    {
        // Sagittarius A* at galactic center
        if (seed == 0)
        {
            return new Star
            {
                Position = Vector3.Zero,
                Type = StellarType.SMBH,
                Mass = 4_310_000f, // 4.31 million solar masses
                Temperature = 0f,
                Color = Vector3.Zero,
                Luminosity = 0f,
                Seed = 0,
                Population = StellarPopulation.Bulge,
                Region = "Galactic Center",
                PlanetCount = 0,
                IsMultiple = false,
                SystemName = "0"
            };
        }
        
        _random = new Random((int)(seed % int.MaxValue));
        
        // Generate position using importance sampling
        var position = GeneratePositionByDensity(seed);
        var population = DeterminePopulation(position);
        var type = DetermineStellarType(position, population, seed);
        var properties = GetStellarProperties(type);
        
        var mass = properties.mass * (0.8f + 0.4f * (float)_random.NextDouble());
        var planetarySystemGen = new PlanetarySystemGenerator();
        var planetarySystem = planetarySystemGen.GeneratePlanetarySystem(seed, type, mass, $"Star-{seed}");
        
        // Check for companions using global database
        var (isMultiple, companionCount, companions) = CompanionStarDatabase.GetCompanionInfo(seed, type);
        
        return new Star
        {
            Position = position,
            Type = type,
            Mass = mass,
            Temperature = properties.temperature,
            Color = properties.color,
            Luminosity = properties.luminosity,
            Seed = seed,
            Population = population,
            Region = DetermineRegion(position),
            PlanetCount = planetarySystem.Planets.Count,
            IsMultiple = isMultiple,
            SystemName = CompanionStarDatabase.GetSystemName(seed, type)
        };
    }    
    private Vector3 GeneratePositionByDensity(long seed)
    {
        var rng = new Random((int)(seed % int.MaxValue));
        
        // Use rejection sampling with importance sampling for efficiency
        for (int attempts = 0; attempts < 100; attempts++)
        {
            // Sample from composite distribution
            var componentRoll = rng.NextDouble();
            Vector3 position;
            
            if (componentRoll < 0.15) // Bulge/Bar
            {
                position = GenerateBulgeBarPosition(rng);
            }
            else if (componentRoll < 0.85) // Disk
            {
                position = GenerateDiskPosition(rng);
            }
            else // Halo
            {
                position = GenerateHaloPosition(rng);
            }
            
            // Calculate total density at this position
            var density = CalculateTotalDensity(position);
            
            // Accept/reject based on density
            if (rng.NextDouble() < density)
            {
                return position;
            }
        }
        
        // Fallback to simple disk position
        return GenerateDiskPosition(rng);
    }    
    private float CalculateTotalDensity(Vector3 position)
    {
        var r = position.Length2D(); // Cylindrical radius
        var z = Math.Abs(position.Z);
        
        // Component densities
        var bulgeDensity = CalculateBulgeDensity(position);
        var barDensity = CalculateBarDensity(position);
        var diskDensity = CalculateDiskDensity(r, z);
        var haloDensity = CalculateHaloDensity(position.Length());
        var spiralModulation = CalculateSpiralArmDensity(position);
        
        // Combine densities
        var totalDensity = bulgeDensity + barDensity + diskDensity * spiralModulation + haloDensity;
        
        // Normalize to [0,1]
        return Math.Min(1.0f, totalDensity);
    }
    
    private float CalculateBulgeDensity(Vector3 position)
    {
        var r = position.Length();
        if (r > BULGE_RADIUS) return 0;
        
        // Hernquist profile for bulge
        var a = BULGE_RADIUS * 0.5f;
        var density = 1.0f / (r / a * Math.Pow(1 + r / a, 3));
        
        // Vertical flattening
        var zScale = 1.0f - 0.5f * Math.Abs(position.Z) / BULGE_SCALE_HEIGHT;
        
        return (float)(density * zScale * 0.3);
    }    
    private float CalculateBarDensity(Vector3 position)
    {
        // Rotate to bar coordinates
        var xBar = position.X * (float)Math.Cos(BAR_ANGLE) + position.Y * (float)Math.Sin(BAR_ANGLE);
        var yBar = -position.X * (float)Math.Sin(BAR_ANGLE) + position.Y * (float)Math.Cos(BAR_ANGLE);
        var zBar = position.Z;
        
        // Check if within bar region
        if (Math.Abs(xBar) > BAR_LENGTH * 0.5f || Math.Abs(yBar) > BAR_WIDTH * 0.5f)
            return 0;
        
        // Exponential profile along bar
        var barDensity = (float)Math.Exp(-2.0 * Math.Abs(xBar) / BAR_LENGTH);
        
        // Gaussian profile perpendicular to bar
        barDensity *= (float)Math.Exp(-Math.Pow(yBar / (BAR_WIDTH * 0.3), 2));
        
        // Vertical profile
        barDensity *= (float)Math.Exp(-Math.Abs(zBar) / 500);
        
        return barDensity * 0.4f;
    }
    
    private float CalculateDiskDensity(float r, float z)
    {
        if (r > DISK_CUTOFF_RADIUS) return 0;
        
        // Exponential radial profile
        var radialProfile = (float)Math.Exp(-r / DISK_SCALE_RADIUS);
        
        // Two-component vertical profile (thin + thick disk)
        var thinDisk = 0.85f * (float)Math.Exp(-z / THIN_DISK_HEIGHT);
        var thickDisk = 0.15f * (float)Math.Exp(-z / THICK_DISK_HEIGHT);
        
        return radialProfile * (thinDisk + thickDisk) * 0.5f;
    }    
    private float CalculateHaloDensity(float r)
    {
        // NFW profile for dark matter halo (with stellar component)
        var x = r / HALO_SCALE_RADIUS;
        if (x < 0.01f) x = 0.01f; // Avoid division by zero
        
        var density = HALO_DENSITY_0 / (x * (float)Math.Pow(1 + x, 2));
        return density;
    }
    
    private float CalculateSpiralArmDensity(Vector3 position)
    {
        var r = position.Length2D();
        if (r < 3000 || r > 45000) return 1.0f; // Arms only in certain radial range
        
        var theta = (float)Math.Atan2(position.Y, position.X);
        var maxDensity = 0.0f;
        
        // Major arms (Perseus and Scutum-Centaurus)
        for (int i = 0; i < NUM_MAJOR_ARMS; i++)
        {
            var armAngle = i * 2 * (float)Math.PI / NUM_MAJOR_ARMS;
            armAngle += SPIRAL_PITCH_ANGLE * (float)Math.Log(r / 8000);
            
            var angleDiff = Math.Abs(NormalizeAngle(theta - armAngle));
            if (angleDiff < Math.PI / 6)
            {
                var armStrength = (1 - angleDiff / (Math.PI / 6));
                armStrength *= (float)Math.Exp(-Math.Pow((r - 20000) / 10000, 2));
                maxDensity = (float)Math.Max(maxDensity, armStrength * 0.5);
            }
        }
        
        // Minor arms (Sagittarius and Norma)
        for (int i = 0; i < NUM_MINOR_ARMS; i++)
        {
            var armAngle = (i + 0.5f) * 2 * (float)Math.PI / NUM_MAJOR_ARMS;
            armAngle += SPIRAL_PITCH_ANGLE * (float)Math.Log(r / 8000);
            
            var angleDiff = Math.Abs(NormalizeAngle(theta - armAngle));
            if (angleDiff < Math.PI / 8)
            {
                var armStrength = (1 - angleDiff / (Math.PI / 8));
                armStrength *= (float)Math.Exp(-Math.Pow((r - 15000) / 8000, 2));
                maxDensity = (float)Math.Max(maxDensity, armStrength * 0.3);
            }
        }
        
        // Local arm (Orion Spur) - where Sun is located
        var localArmAngle = 1.2f + SPIRAL_PITCH_ANGLE * (float)Math.Log(SUN_DISTANCE / 8000);
        var localAngleDiff = Math.Abs(NormalizeAngle(theta - localArmAngle));
        if (r > 20000 && r < 30000 && localAngleDiff < Math.PI / 10)
        {
            var localStrength = (1 - localAngleDiff / (Math.PI / 10));
            localStrength *= (float)Math.Exp(-Math.Pow((r - SUN_DISTANCE) / 3000, 2));
            maxDensity = (float)Math.Max(maxDensity, localStrength * 0.2);
        }
        
        return 1.0f + maxDensity;
    }    
    private Vector3 GenerateBulgeBarPosition(Random rng)
    {
        // 50/50 chance of bulge vs bar
        if (rng.NextDouble() < 0.5)
        {
            // Spheroidal bulge
            var u = rng.NextDouble();
            var v = rng.NextDouble();
            var theta = 2 * Math.PI * u;
            var phi = Math.Acos(2 * v - 1);
            
            // Hernquist profile sampling
            var q = rng.NextDouble();
            var r = BULGE_RADIUS * 0.5f * (float)(Math.Sqrt(q) / (1 - Math.Sqrt(q)));
            
            // Flattened in Z
            var x = r * (float)(Math.Sin(phi) * Math.Cos(theta));
            var y = r * (float)(Math.Sin(phi) * Math.Sin(theta));
            var z = r * (float)Math.Cos(phi) * 0.4f; // Flattening factor
            
            return new Vector3(x, y, z);
        }
        else
        {
            // Bar structure
            var x = (rng.NextDouble() - 0.5) * BAR_LENGTH;
            var y = (rng.NextDouble() - 0.5) * BAR_WIDTH;
            var z = (float)NextGaussian(rng, 0, 300);
            
            // Rotate to bar angle
            var xRot = x * (float)Math.Cos(BAR_ANGLE) - y * (float)Math.Sin(BAR_ANGLE);
            var yRot = x * (float)Math.Sin(BAR_ANGLE) + y * (float)Math.Cos(BAR_ANGLE);
            
            return new Vector3((float)xRot, (float)yRot, z);
        }
    }    
    private Vector3 GenerateDiskPosition(Random rng)
    {
        // Exponential disk sampling
        var u = rng.NextDouble();
        var r = -DISK_SCALE_RADIUS * (float)Math.Log(1 - u * (1 - Math.Exp(-DISK_CUTOFF_RADIUS / DISK_SCALE_RADIUS)));
        
        // Check if in spiral arm
        var theta = rng.NextDouble() * 2 * Math.PI;
        var testPos = new Vector3(r * (float)Math.Cos(theta), r * (float)Math.Sin(theta), 0);
        var spiralDensity = CalculateSpiralArmDensity(testPos);
        
        // Bias towards spiral arms
        if (rng.NextDouble() > spiralDensity / 1.5f)
        {
            // Redistribute angle
            theta = rng.NextDouble() * 2 * Math.PI;
        }
        
        // Vertical distribution (thin vs thick disk)
        float z;
        if (rng.NextDouble() < 0.85) // Thin disk
        {
            z = (float)NextGaussian(rng, 0, THIN_DISK_HEIGHT);
        }
        else // Thick disk
        {
            z = (float)NextGaussian(rng, 0, THICK_DISK_HEIGHT);
        }
        
        return new Vector3(r * (float)Math.Cos(theta), r * (float)Math.Sin(theta), z);
    }    
    private Vector3 GenerateHaloPosition(Random rng)
    {
        // NFW profile sampling
        var u = rng.NextDouble();
        var concentrationParam = 10.0f; // Typical for Milky Way
        var fInv = (float)(Math.Log(1 + concentrationParam) - concentrationParam / (1 + concentrationParam));
        var s = InvertNFW((float)(u * fInv), concentrationParam);
        var r = s * HALO_SCALE_RADIUS;
        
        // Random direction
        var theta = rng.NextDouble() * 2 * Math.PI;
        var phi = Math.Acos(2 * rng.NextDouble() - 1);
        
        var x = r * (float)(Math.Sin(phi) * Math.Cos(theta));
        var y = r * (float)(Math.Sin(phi) * Math.Sin(theta));
        var z = r * (float)Math.Cos(phi);
        
        return new Vector3(x, y, z);
    }
    
    private float InvertNFW(float f, float c)
    {
        // Binary search to invert NFW cumulative distribution
        float sMin = 0, sMax = c;
        for (int i = 0; i < 20; i++)
        {
            float sMid = (sMin + sMax) * 0.5f;
            float fMid = (float)(Math.Log(1 + sMid) - sMid / (1 + sMid));
            if (fMid < f) sMin = sMid;
            else sMax = sMid;
        }
        return (sMin + sMax) * 0.5f;
    }    
    public StellarPopulation DeterminePopulation(Vector3 position)
    {
        var r = position.Length2D();
        var z = Math.Abs(position.Z);
        var rTotal = position.Length();
        
        // Central region
        if (r < 1000) return StellarPopulation.Bulge;
        
        // Bulge/Bar
        if (r < BULGE_RADIUS && z < BULGE_SCALE_HEIGHT)
        {
            // Check if in bar
            var xBar = position.X * (float)Math.Cos(BAR_ANGLE) + position.Y * (float)Math.Sin(BAR_ANGLE);
            var yBar = -position.X * (float)Math.Sin(BAR_ANGLE) + position.Y * (float)Math.Cos(BAR_ANGLE);
            if (Math.Abs(xBar) < BAR_LENGTH * 0.5f && Math.Abs(yBar) < BAR_WIDTH * 0.5f)
                return StellarPopulation.Bulge;
            return StellarPopulation.Bulge;
        }
        
        // Halo
        if (rTotal > 30000 || (z > 5000 && r > 20000))
            return StellarPopulation.Halo;
        
        // Thick disk
        if (z > 600 || (z > 400 && r > 30000))
            return StellarPopulation.ThickDisk;
        
        // Thin disk
        return StellarPopulation.ThinDisk;
    }    
    public StellarType DetermineStellarType(Vector3 position, StellarPopulation population, long seed)
    {
        var rng = new Random((int)((seed * 31) % int.MaxValue));
        var roll = rng.NextDouble();
        
        // Calculate galactocentric distance
        var galacticRadius = position.Length2D();
        var isOuterDisc = galacticRadius > 15000f;
        
        // Check for special objects in central region
        if (population == StellarPopulation.Bulge && position.Length() < 100)
        {
            if (roll < 0.1) return StellarType.BH;
            if (roll < 0.3) return StellarType.NS;
        }
        
        // Population-specific distributions based on metallicity and age
        switch (population)
        {
            case StellarPopulation.Halo:
                // Old, metal-poor population
                if (roll < 0.0001) return StellarType.B0V;
                if (roll < 0.001) return StellarType.A0V;
                if (roll < 0.005) return StellarType.F0V;
                if (roll < 0.025) return StellarType.G5V;
                if (roll < 0.12) return StellarType.K5V;
                if (roll < 0.75) return StellarType.M5V;
                if (roll < 0.85) return StellarType.DA;
                if (roll < 0.95) return StellarType.K0III;
                // Reduce neutron star probability in outer disc
                if (isOuterDisc && roll < 0.975) return StellarType.K0III;
                return StellarType.NS;
                
            case StellarPopulation.ThickDisk:
                // Intermediate age population
                if (roll < 0.0002) return StellarType.B0V;
                if (roll < 0.002) return StellarType.A0V;
                if (roll < 0.01) return StellarType.F0V;
                if (roll < 0.04) return StellarType.G5V;
                if (roll < 0.15) return StellarType.K5V;
                if (roll < 0.77) return StellarType.M5V;
                if (roll < 0.85) return StellarType.DA;
                if (roll < 0.92) return StellarType.K0III;
                // Adjust neutron star probability for outer disc
                if (isOuterDisc)
                {
                    if (roll < 0.96) return StellarType.K0III;
                    if (roll < 0.99) return StellarType.NS;
                }
                else
                {
                    if (roll < 0.98) return StellarType.NS;
                }
                return StellarType.BH;                
            case StellarPopulation.ThinDisk:
                // Check if in spiral arm for star formation
                var spiralBoost = CalculateSpiralArmDensity(position) - 1.0f;
                
                // Young population in spiral arms
                if (spiralBoost > 0.2 && roll < 0.001 * (1 + spiralBoost))
                    return StellarType.O5V;
                if (roll < 0.0003 + 0.001 * spiralBoost) return StellarType.O5V;
                if (roll < 0.0013 + 0.003 * spiralBoost) return StellarType.B0V;
                if (roll < 0.006 + 0.006 * spiralBoost) return StellarType.A0V;
                if (roll < 0.03 + 0.01 * spiralBoost) return StellarType.F0V;
                if (roll < 0.076) return StellarType.G5V;
                if (roll < 0.121) return StellarType.K5V;
                if (roll < 0.885) return StellarType.M5V;
                if (roll < 0.92) return StellarType.DA;
                if (roll < 0.95) return StellarType.K0III;
                // Adjust neutron star probability for outer disc
                if (isOuterDisc)
                {
                    if (roll < 0.97) return StellarType.K0III;
                    if (roll < 0.995) return StellarType.NS;
                }
                else
                {
                    if (roll < 0.99) return StellarType.NS;
                }
                return StellarType.BH;
                
            case StellarPopulation.Bulge:
                // Old, metal-rich population
                if (roll < 0.0001) return StellarType.B0V;
                if (roll < 0.001) return StellarType.A0V;
                if (roll < 0.008) return StellarType.F0V;
                if (roll < 0.045) return StellarType.G5V;
                if (roll < 0.16) return StellarType.K5V;
                if (roll < 0.70) return StellarType.M5V;
                if (roll < 0.80) return StellarType.DA;
                if (roll < 0.90) return StellarType.K0III;
                // Bulge population rarely extends beyond 15000 ly, but adjust just in case
                if (isOuterDisc)
                {
                    if (roll < 0.935) return StellarType.K0III;
                    if (roll < 0.985) return StellarType.NS;
                }
                else
                {
                    if (roll < 0.97) return StellarType.NS;
                }
                return StellarType.BH;
                
            default:
                // Default to M dwarf
                return StellarType.M5V;
        }
    }    
    public (float mass, float temperature, Vector3 color, float luminosity) GetStellarProperties(StellarType type)
    {
        switch (type)
        {
            case StellarType.O5V:
                return (40f, 35000f, new Vector3(0.6f, 0.7f, 1.0f), 500000f);
            case StellarType.B0V:
                return (15f, 20000f, new Vector3(0.7f, 0.8f, 1.0f), 25000f);
            case StellarType.A0V:
                return (2.2f, 8500f, new Vector3(0.9f, 0.9f, 1.0f), 40f);
            case StellarType.F0V:
                return (1.4f, 6500f, new Vector3(1.0f, 1.0f, 0.95f), 3.5f);
            case StellarType.G5V:
                return (1.0f, 5500f, new Vector3(1.0f, 1.0f, 0.8f), 1f);
            case StellarType.K5V:
                return (0.7f, 4200f, new Vector3(1.0f, 0.85f, 0.65f), 0.4f);
            case StellarType.M5V:
                return (0.3f, 3200f, new Vector3(1.0f, 0.6f, 0.4f), 0.04f);
            case StellarType.K0III:
                return (1.2f, 3500f, new Vector3(1.0f, 0.4f, 0.2f), 200f);
            case StellarType.B0III:
                return (20f, 25000f, new Vector3(0.6f, 0.75f, 1.0f), 80000f);
            case StellarType.M2I:
                return (25f, 4000f, new Vector3(1.0f, 0.3f, 0.1f), 300000f);
            case StellarType.DA:
                return (0.6f, 10000f, new Vector3(0.95f, 0.95f, 1.0f), 0.01f);
            case StellarType.NS:
                return (1.4f, 1000000f, new Vector3(0.7f, 0.7f, 1.0f), 0.001f);
            case StellarType.BH:
                return (10f, 0f, Vector3.Zero, 0f);
            case StellarType.SMBH:
                return (4310000f, 0f, Vector3.Zero, 0f);
            default:
                return (1f, 5500f, new Vector3(1f, 1f, 1f), 1f);
        }
    }    
    private float NormalizeAngle(float angle)
    {
        while (angle > Math.PI) angle -= 2 * (float)Math.PI;
        while (angle < -Math.PI) angle += 2 * (float)Math.PI;
        return angle;
    }
    
    private double NextGaussian(Random rng, double mean, double stdDev)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * randStdNormal;
    }
    
    public List<Star> GenerateStars(int count)
    {
        var stars = new List<Star>();
        var usedSeeds = new HashSet<long>();
        
        // Always include Sgr A*
        stars.Add(GenerateStarAtSeed(0));
        usedSeeds.Add(0);
        
        // Progress tracking
        var startTime = DateTime.Now;
        var lastUpdate = DateTime.Now;
        
        while (stars.Count < count)
        {
            long seed;
            do
            {
                seed = (long)(_random.NextDouble() * TOTAL_STARS);
            } while (usedSeeds.Contains(seed));
            
            usedSeeds.Add(seed);
            stars.Add(GenerateStarAtSeed(seed));
            
            // Progress update every second
            if ((DateTime.Now - lastUpdate).TotalSeconds > 1.0)
            {
                var progress = 100.0 * stars.Count / count;
                var elapsed = (DateTime.Now - startTime).TotalSeconds;
                var rate = stars.Count / elapsed;
                var remaining = (count - stars.Count) / rate;
                
                Console.Write($"\rGenerating: {stars.Count:N0}/{count:N0} ({progress:F1}%) " +
                            $"Rate: {rate:F0}/s ETA: {remaining:F0}s");
                lastUpdate = DateTime.Now;
            }
        }
        
        Console.WriteLine($"\nGeneration complete in {(DateTime.Now - startTime).TotalSeconds:F1}s");
        return stars;
    }
    
    public Star GetStarBySeed(long seed)
    {
        return GenerateStarAtSeed(seed);
    }
    
    /// <summary>
    /// Generate a star at a specific position (for density-based sampling)
    /// </summary>
    public Star GenerateStarAtPosition(Vector3 position)
    {
        // Generate a pseudo-random seed based on position
        // Use integer math to ensure consistency
        long xPart = (long)Math.Round(position.X * 1000);
        long yPart = (long)Math.Round(position.Y * 1000);
        long zPart = (long)Math.Round(position.Z * 1000);
        
        // Combine parts with bit shifting to create unique seed
        long seed = Math.Abs(xPart + (yPart << 20) + (zPart << 40));
        seed = seed % TOTAL_STARS;
        
        var rng = new Random((int)(seed & 0x7FFFFFFF));
        
        // Determine stellar population based on position
        var r = Math.Sqrt(position.X * position.X + position.Y * position.Y);
        var z = Math.Abs(position.Z);
        
        StellarPopulation population;
        if (r < 1000 && z < 200)
            population = StellarPopulation.PopIII; // Very old stars in core
        else if (r < 5000 && z < 1000)
            population = StellarPopulation.PopII;
        else if (z > 5000)
            population = StellarPopulation.Halo;
        else
            population = StellarPopulation.ThinDisk;
        
        // Generate stellar properties
        var stellarType = GenerateStellarType(rng, population, position);
        var mass = GenerateStellarMass(stellarType, rng);
        var (temperature, luminosity) = CalculateStellarProperties(mass, stellarType);
        var color = CalculateStarColor(temperature);
        
        var planetarySystemGen = new PlanetarySystemGenerator();
        var planetarySystem = planetarySystemGen.GeneratePlanetarySystem(seed, stellarType, mass, $"Star-{seed}");
        
        // Check for companions using global database
        var (isMultiple, companionCount, companions) = CompanionStarDatabase.GetCompanionInfo(seed, stellarType);
        
        return new Star
        {
            Seed = seed,
            Position = position,
            Type = stellarType,
            Mass = mass,
            Temperature = temperature,
            Luminosity = luminosity,
            Color = color,
            Population = population,
            Region = DetermineRegion(position),
            PlanetCount = planetarySystem.Planets.Count,
            IsMultiple = isMultiple,
            SystemName = CompanionStarDatabase.GetSystemName(seed, stellarType)
        };
    }
    
    public string DetermineRegion(Vector3 position)
    {
        var r = position.Length2D();
        var z = Math.Abs(position.Z);
        
        // Check if near galactic center
        if (r < 100) return "Galactic Center";
        if (r < 1000) return "Central Molecular Zone";
        
        // Check if in bulge or bar
        if (r < BULGE_RADIUS && z < BULGE_SCALE_HEIGHT)
        {
            var xBar = position.X * (float)Math.Cos(BAR_ANGLE) + position.Y * (float)Math.Sin(BAR_ANGLE);
            var yBar = -position.X * (float)Math.Sin(BAR_ANGLE) + position.Y * (float)Math.Cos(BAR_ANGLE);
            if (Math.Abs(xBar) < BAR_LENGTH * 0.5f && Math.Abs(yBar) < BAR_WIDTH * 0.5f)
                return "Galactic Bar";
            return "Galactic Bulge";
        }
        
        // Check spiral arms
        var spiralDensity = CalculateSpiralArmDensity(position);
        if (spiralDensity > 1.3f)
        {
            // Determine which arm
            var theta = (float)Math.Atan2(position.Y, position.X);
            
            // Check major arms
            for (int i = 0; i < NUM_MAJOR_ARMS; i++)
            {
                var armAngle = i * 2 * (float)Math.PI / NUM_MAJOR_ARMS;
                armAngle += SPIRAL_PITCH_ANGLE * (float)Math.Log(r / 8000);
                var angleDiff = Math.Abs(NormalizeAngle(theta - armAngle));
                if (angleDiff < Math.PI / 6)
                {
                    return i == 0 ? "Perseus Arm" : "Scutum-Centaurus Arm";
                }
            }
            
            // Check minor arms
            for (int i = 0; i < NUM_MINOR_ARMS; i++)
            {
                var armAngle = (i + 0.5f) * 2 * (float)Math.PI / NUM_MAJOR_ARMS;
                armAngle += SPIRAL_PITCH_ANGLE * (float)Math.Log(r / 8000);
                var angleDiff = Math.Abs(NormalizeAngle(theta - armAngle));
                if (angleDiff < Math.PI / 8)
                {
                    return i == 0 ? "Sagittarius Arm" : "Norma Arm";
                }
            }
            
            // Check local arm
            var localArmAngle = 1.2f + SPIRAL_PITCH_ANGLE * (float)Math.Log(SUN_DISTANCE / 8000);
            var localAngleDiff = Math.Abs(NormalizeAngle(theta - localArmAngle));
            if (r > 20000 && r < 30000 && localAngleDiff < Math.PI / 10)
            {
                return "Local Arm (Orion Spur)";
            }
        }
        
        // Inter-arm regions
        if (r < 15000) return "Inner Disk";
        if (r < 30000) return "Solar Neighborhood" + (Math.Abs(r - SUN_DISTANCE) < 1000 ? " (Near Sun)" : "");
        if (r < 45000) return "Outer Disk";
        
        // Far regions
        if (z > 5000) return "Galactic Halo";
        return "Far Outer Disk";
    }
    
    /// <summary>
    /// Generate stellar type based on population and random distribution
    /// </summary>
    private StellarType GenerateStellarType(Random rng, StellarPopulation population)
    {
        return GenerateStellarType(rng, population, Vector3.Zero);
    }
    
    private StellarType GenerateStellarType(Random rng, StellarPopulation population, Vector3 position)
    {
        var roll = rng.NextDouble();
        
        // Calculate galactocentric distance
        var galacticRadius = position.Length2D();
        var isOuterDisc = galacticRadius > 15000f;
        
        switch (population)
        {
            case StellarPopulation.PopIII:
                // First generation stars - mostly massive
                if (roll < 0.3) return StellarType.O5V;
                if (roll < 0.5) return StellarType.B0V;
                if (roll < 0.7) return StellarType.A0V;
                if (roll < 0.9) return StellarType.BH;
                return StellarType.NS;
                
            case StellarPopulation.PopII:
            case StellarPopulation.Halo:
                // Old, metal-poor population
                if (roll < 0.0001) return StellarType.B0V;
                if (roll < 0.001) return StellarType.A0V;
                if (roll < 0.005) return StellarType.F0V;
                if (roll < 0.025) return StellarType.G5V;
                if (roll < 0.12) return StellarType.K5V;
                if (roll < 0.75) return StellarType.M5V;
                if (roll < 0.85) return StellarType.DA;
                if (roll < 0.95) return StellarType.K0III;
                // Reduce neutron star probability in outer disc
                if (isOuterDisc && roll < 0.975) return StellarType.K0III;
                return StellarType.NS;
                
            case StellarPopulation.ThickDisk:
                // Intermediate age population
                if (roll < 0.0002) return StellarType.B0V;
                if (roll < 0.002) return StellarType.A0V;
                if (roll < 0.01) return StellarType.F0V;
                if (roll < 0.04) return StellarType.G5V;
                if (roll < 0.15) return StellarType.K5V;
                if (roll < 0.77) return StellarType.M5V;
                if (roll < 0.85) return StellarType.DA;
                if (roll < 0.92) return StellarType.K0III;
                // Adjust neutron star probability for outer disc
                if (isOuterDisc)
                {
                    if (roll < 0.96) return StellarType.K0III;
                    if (roll < 0.99) return StellarType.NS;
                }
                else
                {
                    if (roll < 0.98) return StellarType.NS;
                }
                return StellarType.BH;
                
            case StellarPopulation.ThinDisk:
                // Young population - follows IMF
                if (roll < 0.0003) return StellarType.O5V;
                if (roll < 0.0013) return StellarType.B0V;
                if (roll < 0.006) return StellarType.A0V;
                if (roll < 0.03) return StellarType.F0V;
                if (roll < 0.076) return StellarType.G5V;
                if (roll < 0.121) return StellarType.K5V;
                if (roll < 0.885) return StellarType.M5V;
                if (roll < 0.92) return StellarType.DA;
                if (roll < 0.95) return StellarType.K0III;
                // Adjust neutron star probability for outer disc
                if (isOuterDisc)
                {
                    if (roll < 0.97) return StellarType.K0III;
                    if (roll < 0.995) return StellarType.NS;
                }
                else
                {
                    if (roll < 0.99) return StellarType.NS;
                }
                return StellarType.BH;
                
            case StellarPopulation.Bulge:
            default:
                // Old, metal-rich population
                if (roll < 0.0001) return StellarType.B0V;
                if (roll < 0.001) return StellarType.A0V;
                if (roll < 0.008) return StellarType.F0V;
                if (roll < 0.045) return StellarType.G5V;
                if (roll < 0.16) return StellarType.K5V;
                if (roll < 0.70) return StellarType.M5V;
                if (roll < 0.80) return StellarType.DA;
                if (roll < 0.90) return StellarType.K0III;
                // Bulge population rarely extends beyond 15000 ly, but adjust just in case
                if (isOuterDisc)
                {
                    if (roll < 0.935) return StellarType.K0III;
                    if (roll < 0.985) return StellarType.NS;
                }
                else
                {
                    if (roll < 0.97) return StellarType.NS;
                }
                return StellarType.BH;
        }
    }
    
    /// <summary>
    /// Generate stellar mass based on type and IMF
    /// </summary>
    private float GenerateStellarMass(StellarType type, Random rng)
    {
        var variation = 0.8f + 0.4f * (float)rng.NextDouble();
        
        switch (type)
        {
            case StellarType.O5V:
                return (20f + 80f * (float)rng.NextDouble()) * variation;
            case StellarType.B0V:
                return (3f + 17f * (float)rng.NextDouble()) * variation;
            case StellarType.A0V:
                return (1.5f + 1.5f * (float)rng.NextDouble()) * variation;
            case StellarType.F0V:
                return (1.1f + 0.4f * (float)rng.NextDouble()) * variation;
            case StellarType.G5V:
                return (0.8f + 0.3f * (float)rng.NextDouble()) * variation;
            case StellarType.K5V:
                return (0.5f + 0.3f * (float)rng.NextDouble()) * variation;
            case StellarType.M5V:
                return (0.08f + 0.42f * (float)rng.NextDouble()) * variation;
            case StellarType.K0III:
                return (0.8f + 2f * (float)rng.NextDouble()) * variation;
            case StellarType.B0III:
                return (10f + 30f * (float)rng.NextDouble()) * variation;
            case StellarType.M2I:
                return (20f + 80f * (float)rng.NextDouble()) * variation;
            case StellarType.DA:
                return (0.5f + 0.3f * (float)rng.NextDouble()) * variation;
            case StellarType.NS:
                return 1.4f * variation;
            case StellarType.BH:
                return (5f + 20f * (float)rng.NextDouble()) * variation;
            case StellarType.SMBH:
                return 4_310_000f;
            default:
                return 1.0f * variation;
        }
    }
    
    /// <summary>
    /// Calculate stellar properties from mass and type
    /// </summary>
    private (float temperature, float luminosity) CalculateStellarProperties(float mass, StellarType type)
    {
        // Main sequence mass-luminosity and mass-temperature relations
        switch (type)
        {
            case StellarType.O5V:
            case StellarType.B0V:
            case StellarType.A0V:
            case StellarType.F0V:
            case StellarType.G5V:
            case StellarType.K5V:
            case StellarType.M5V:
                // Main sequence relationships
                var luminosity = (float)Math.Pow(mass, 3.5); // L ∝ M^3.5
                var temperature = 5778f * (float)Math.Pow(mass, 0.57); // Approximate
                return (temperature, luminosity);
                
            case StellarType.K0III:
                return (3500f + 1000f * mass, 100f * mass);
                
            case StellarType.B0III:
                return (20000f + 5000f * mass / 20f, (float)Math.Pow(mass, 3.8));
                
            case StellarType.M2I:
                return (4000f + 2000f * mass / 50f, 100000f * mass / 50f);
                
            case StellarType.DA:
                return (10000f + 20000f * (1f - mass), 0.001f + 0.01f * (1f - mass));
                
            case StellarType.NS:
                return (1000000f, 0.001f);
                
            case StellarType.BH:
            case StellarType.SMBH:
                return (0f, 0f);
                
            default:
                return (5500f, 1f);
        }
    }
    
    /// <summary>
    /// Calculate star color from temperature using blackbody radiation
    /// </summary>
    private Vector3 CalculateStarColor(float temperature)
    {
        if (temperature <= 0) return Vector3.Zero;
        
        // Simplified blackbody color calculation
        float r, g, b;
        
        if (temperature < 3500)
        {
            // Cool stars (red)
            r = 1.0f;
            g = temperature / 3500f * 0.6f;
            b = temperature / 3500f * 0.3f;
        }
        else if (temperature < 5000)
        {
            // K-type stars (orange)
            r = 1.0f;
            g = 0.6f + (temperature - 3500f) / 1500f * 0.3f;
            b = 0.3f + (temperature - 3500f) / 1500f * 0.3f;
        }
        else if (temperature < 6000)
        {
            // G-type stars (yellow-white)
            r = 1.0f;
            g = 0.9f + (temperature - 5000f) / 1000f * 0.1f;
            b = 0.6f + (temperature - 5000f) / 1000f * 0.2f;
        }
        else if (temperature < 7500)
        {
            // F-type stars (white)
            r = 1.0f;
            g = 1.0f;
            b = 0.8f + (temperature - 6000f) / 1500f * 0.2f;
        }
        else if (temperature < 10000)
        {
            // A-type stars (blue-white)
            r = 0.9f + (10000f - temperature) / 2500f * 0.1f;
            g = 0.9f + (10000f - temperature) / 2500f * 0.1f;
            b = 1.0f;
        }
        else
        {
            // B and O-type stars (blue)
            r = 0.6f + (30000f - Math.Min(temperature, 30000f)) / 20000f * 0.3f;
            g = 0.7f + (30000f - Math.Min(temperature, 30000f)) / 20000f * 0.2f;
            b = 1.0f;
        }
        
        return new Vector3(r, g, b);
    }
}