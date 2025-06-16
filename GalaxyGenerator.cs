using System;
using System.Collections.Generic;

/// <summary>
/// Unified Galaxy Generator that determines star positions and properties based on location.
/// This is the master algorithm that defines how the galaxy looks.
/// </summary>
public static class GalaxyGenerator
{
    // Galaxy Structure Parameters
    public const float GALAXY_RADIUS = 60_000f; // light years
    
    // Disk Parameters
    public const float DISK_SCALE_RADIUS = 3_500f; // Radial scale length
    public const float DISK_CUTOFF_RADIUS = 50_000f; // Where disk drops off
    
    // Halo Parameters
    public const float HALO_SCALE_RADIUS = 20_000f; // NFW scale radius
    public const float HALO_DENSITY_0 = 0.002f; // Central halo density (relative)
    
    // Solar position
    public const float SUN_DISTANCE = 26_000f; // Distance from galactic center
    public const float SUN_HEIGHT = 20f; // Height above galactic plane
    
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
    
    /// <summary>
    /// Generate a deterministic seed based on a 3D position.
    /// This ensures that the same position always generates the same star.
    /// </summary>
    public static long GenerateSeedFromPosition(Vector3 position)
    {
        // Quantize position to ensure consistency (1/10 light year precision)
        long xPart = (long)Math.Round(position.X * 10);
        long yPart = (long)Math.Round(position.Y * 10);
        long zPart = (long)Math.Round(position.Z * 10);
        
        // Use a hash-like combination to create unique seeds
        // This avoids patterns while maintaining determinism
        long seed = xPart * 73856093L ^ yPart * 19349663L ^ zPart * 83492791L;
        
        // Ensure positive - use bitwise AND to keep lower 63 bits
        seed = seed & 0x7FFFFFFFFFFFFFFF;
        
        return seed;
    }
    
    /// <summary>
    /// Calculate the total stellar density at a given position.
    /// This is the master density function that defines the galaxy's shape.
    /// </summary>
    public static float CalculateTotalDensity(Vector3 position)
    {
        var r = position.Length2D(); // Cylindrical radius
        var z = Math.Abs(position.Z);
        
        // Base disk density (includes bulge through height variation)
        var diskDensity = CalculateDiskDensity(r, z);
        
        // Spiral arm modulation
        var armMultiplier = CalculateSpiralArmMultiplier(position);
        
        // Simple halo for outer regions
        var haloDensity = CalculateHaloDensity(position.Length());
        
        // Combine: disk is modulated by arms, plus halo
        var totalDensity = diskDensity * armMultiplier + haloDensity;
        
        return totalDensity;
    }
    
    // Removed old bulge and bar functions - now integrated into disk
    
    /// <summary>
    /// Calculate disk density with built-in bulge through height variation
    /// </summary>
    public static float CalculateDiskDensity(float r, float z)
    {
        // Scale height varies smoothly with radius
        float scaleHeight;
        if (r < 1000)
        {
            // Central region - nearly spherical
            scaleHeight = 800f;
        }
        else if (r < 6000)  // Match new bulge size
        {
            // Smooth transition from bulge to disk
            var t = (r - 1000f) / 5000f;
            // Use smooth interpolation
            t = t * t * (3 - 2 * t); // Smoothstep function
            scaleHeight = 800f * (1 - t) + 300f * t;
        }
        else
        {
            // Disk region with slight flaring
            scaleHeight = 300f + (r / 100000f) * 300f;
        }
        
        // Radial density profile - smooth transitions everywhere
        float radialDensity;
        if (r < 100)
        {
            // Very center - highest density
            radialDensity = 1.0f;
        }
        else if (r < 1000)
        {
            // Core to inner bulge - gentle decline
            var t = (r - 100) / 900f;
            radialDensity = 1.0f - t * 0.2f; // Goes from 1.0 to 0.8
        }
        else if (r < 6000)  // Increased bulge size from 5000 to 6000
        {
            // Bulge region - smooth power law decline
            var t = (r - 1000) / 5000f;
            // Smooth interpolation from 0.8 to 0.1
            radialDensity = 0.8f * (float)Math.Pow(1 - t, 1.5f) + 0.1f * t;
        }
        else if (r < 8000)
        {
            // Transition zone between bulge and disk
            var t = (r - 6000) / 2000f;
            var bulgeEnd = 0.1f;
            var diskStart = 0.1f * (float)Math.Exp(-(1000) / DISK_SCALE_RADIUS); // Slightly lower
            radialDensity = bulgeEnd * (1 - t) + diskStart * t;
        }
        else
        {
            // Pure disk region - exponential
            var expFactor = (float)Math.Exp(-(r - 6000) / DISK_SCALE_RADIUS);
            radialDensity = 0.1f * expFactor;
        }
        
        // Smooth vertical profile
        var verticalProfile = (float)Math.Exp(-z * z / (2 * scaleHeight * scaleHeight));
        
        // Very smooth edge transition
        if (r > DISK_CUTOFF_RADIUS * 0.8f)
        {
            var edgeDistance = (r - DISK_CUTOFF_RADIUS * 0.8f) / (DISK_CUTOFF_RADIUS * 0.2f);
            var edgeFactor = 1.0f / (1.0f + (float)Math.Exp(10 * (edgeDistance - 1)));
            radialDensity *= edgeFactor;
        }
        
        return radialDensity * verticalProfile;
    }
    
    /// <summary>
    /// Calculate halo density using NFW profile
    /// </summary>
    public static float CalculateHaloDensity(float r)
    {
        // Only significant beyond disk
        if (r < 30000) return 0;
        
        // NFW profile for stellar halo - extremely low density
        var x = r / HALO_SCALE_RADIUS;
        if (x < 0.01f) x = 0.01f; // Avoid division by zero
        
        // Much lower density - halo stars are very rare
        var density = 0.00001f / (x * (float)Math.Pow(1 + x, 2));
        
        // Smooth transition from disk
        var transitionFactor = 1.0f - (float)Math.Exp(-(r - 30000) / 10000);
        
        return density * transitionFactor;
    }
    
    /// <summary>
    /// Calculate spiral arm multiplier using logarithmic spirals
    /// </summary>
    public static float CalculateSpiralArmMultiplier(Vector3 position)
    {
        var r = position.Length2D();
        var theta = (float)Math.Atan2(position.Y, position.X);
        
        // Don't apply arms too close to center
        if (r < 3000) return 1.0f;
        
        // Simple approach: 4 arms (2 major, 2 minor)
        var armStrength = 0.0f;
        
        // Pitch angle: angle between arm tangent and circle (perpendicular to radius)
        // For Milky Way: ~12-25 degrees for different arms
        // We'll use 15 degrees for major arms, 20 for minor (tighter)
        var majorPitchAngle = 15f * (float)Math.PI / 180f; // Convert to radians
        var minorPitchAngle = 20f * (float)Math.PI / 180f;
        
        // For a logarithmic spiral: theta = theta0 + ln(r/r0) / tan(pitch)
        // This gives much more winding than our previous linear approach
        var r0 = 3000f; // Reference radius where arms start
        
        // Major arms
        for (int i = 0; i < 2; i++)
        {
            // Starting angle for this arm
            var theta0 = i * (float)Math.PI; // 180 degrees apart
            
            // Logarithmic spiral equation
            var expectedTheta = theta0 + (float)Math.Log(r / r0) / (float)Math.Tan(majorPitchAngle);
            
            // Calculate angular distance to this arm
            var angleDiff = (float)Math.Abs(NormalizeAngle(theta - expectedTheta));
            
            // Arm width - gets wider with radius
            var armWidth = 0.1f + (r / GALAXY_RADIUS) * 0.15f;
            
            // Gaussian profile for arm
            var thisArmStrength = (float)Math.Exp(-Math.Pow(angleDiff / armWidth, 2));
            
            // Fade arms in and out radially
            var radialFade = 1.0f;
            if (r < 8000)
            {
                radialFade = (r - 3000) / 5000f;
            }
            else if (r > 40000)
            {
                radialFade = (float)Math.Exp(-Math.Pow((r - 40000) / 10000, 2));
            }
            
            armStrength += thisArmStrength * radialFade * 0.8f; // Major arms stronger
        }
        
        // Minor arms - offset by 90 degrees
        for (int i = 0; i < 2; i++)
        {
            var theta0 = (float)(Math.PI * 0.5 + i * Math.PI); // 90 and 270 degrees
            
            // Tighter pitch angle for minor arms
            var expectedTheta = theta0 + (float)Math.Log(r / r0) / (float)Math.Tan(minorPitchAngle);
            
            var angleDiff = (float)Math.Abs(NormalizeAngle(theta - expectedTheta));
            
            var armWidth = 0.08f + (r / GALAXY_RADIUS) * 0.12f;
            var thisArmStrength = (float)Math.Exp(-Math.Pow(angleDiff / armWidth, 2));
            
            // Minor arms fade differently
            var radialFade = 1.0f;
            if (r < 10000)
            {
                radialFade = (r - 3000) / 7000f;
            }
            else if (r > 35000)
            {
                radialFade = (float)Math.Exp(-Math.Pow((r - 35000) / 8000, 2));
            }
            
            armStrength += thisArmStrength * radialFade * 0.5f; // Minor arms weaker
        }
        
        // Return multiplier: 1 = inter-arm region, 2-4x in arms
        // Use smooth function to ensure gradual transitions
        return 1.0f + armStrength * 2.5f; // Up to 3.5x density in arms
    }
    
    /// <summary>
    /// Determine which galactic region a position belongs to
    /// </summary>
    public static string DetermineRegion(Vector3 position)
    {
        var r = position.Length2D();
        var z = Math.Abs(position.Z);
        
        // Check if near galactic center
        if (r < 100) return "Galactic Center";
        if (r < 1000) return "Central Molecular Zone";
        if (r < 6000) return "Galactic Bulge";
        
        // Check spiral arms using our multiplier
        var armMultiplier = CalculateSpiralArmMultiplier(position);
        if (armMultiplier > 1.5f && r > 5000)
        {
            // Determine which arm based on angle and radius
            var theta = (float)Math.Atan2(position.Y, position.X);
            var r0 = 3000f;
            var majorPitchAngle = 15f * (float)Math.PI / 180f;
            var minorPitchAngle = 20f * (float)Math.PI / 180f;
            
            // Check major arms
            for (int i = 0; i < 2; i++)
            {
                var theta0 = i * (float)Math.PI;
                var expectedTheta = theta0 + (float)Math.Log(r / r0) / (float)Math.Tan(majorPitchAngle);
                var angleDiff = (float)Math.Abs(NormalizeAngle(theta - expectedTheta));
                if (angleDiff < 0.3f)
                {
                    return i == 0 ? "Perseus Arm" : "Scutum-Centaurus Arm";
                }
            }
            
            // Check minor arms
            for (int i = 0; i < 2; i++)
            {
                var theta0 = (float)(Math.PI * 0.5 + i * Math.PI);
                var expectedTheta = theta0 + (float)Math.Log(r / r0) / (float)Math.Tan(minorPitchAngle);
                var angleDiff = (float)Math.Abs(NormalizeAngle(theta - expectedTheta));
                if (angleDiff < 0.25f)
                {
                    return i == 0 ? "Sagittarius Arm" : "Norma Arm";
                }
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
    /// Determine stellar population based on position
    /// </summary>
    public static ScientificMilkyWayGenerator.StellarPopulation DeterminePopulation(Vector3 position)
    {
        var r = position.Length2D();
        var z = Math.Abs(position.Z);
        var rTotal = position.Length();
        
        // Central bulge region
        if (r < 6000) return ScientificMilkyWayGenerator.StellarPopulation.Bulge;
        
        // Halo - far out or high above/below disk
        if (rTotal > 30000 || (z > 5000 && r > 20000))
            return ScientificMilkyWayGenerator.StellarPopulation.Halo;
        
        // Thick disk - moderate height above disk
        if (z > 600 || (z > 400 && r > 30000))
            return ScientificMilkyWayGenerator.StellarPopulation.ThickDisk;
        
        // Thin disk - everything else
        return ScientificMilkyWayGenerator.StellarPopulation.ThinDisk;
    }
    
    /// <summary>
    /// Get expected stellar density at a position (stars per cubic light year)
    /// </summary>
    public static float GetExpectedStarDensity(Vector3 position)
    {
        var density = CalculateTotalDensity(position);
        
        // Convert normalized density to actual stellar density
        // Using realistic astrophysical values
        return ConvertToStellarDensity(density, position);
    }
    
    /// <summary>
    /// Get expected number of stars in a volume element based on density
    /// </summary>
    public static float GetExpectedStarCount(Vector3 position, float volume)
    {
        var stellarDensity = GetExpectedStarDensity(position);
        return stellarDensity * volume;
    }
    
    /// <summary>
    /// Convert normalized density (0-1) to actual stellar density (stars/ly³)
    /// </summary>
    private static float ConvertToStellarDensity(float normalizedDensity, Vector3 position)
    {
        var r = position.Length2D();
        
        // Different regions have different density mappings
        // The normalized density already includes the exponential falloff,
        // so we just need to scale it to the right stellar density
        
        if (r < 130)
        {
            // Galactic center - 40 parsecs (130 ly) radius
            // Peak density ~288 stars/ly³
            return normalizedDensity * 288f;
        }
        else if (r < 6000)
        {
            // Bulge region - multi-phase exponential decay
            // At r=130 ly: ~288 stars/ly³
            // At r=326 ly (100 pc): ~2.9 stars/ly³
            // At r=1000 ly: ~0.1-0.5 stars/ly³ (transition region)
            // At r=6000 ly: ~0.006 stars/ly³ (disk average)
            
            float scaleFactor;
            
            if (r < 326)
            {
                // Inner bulge: steep decay from 288 to 2.9
                scaleFactor = 288f * (float)Math.Exp(-(r - 130f) / 85f);
            }
            else if (r < 2000)
            {
                // Middle bulge: smooth decay from 2.9 to ~0.3
                var t = (r - 326f) / (2000f - 326f);
                scaleFactor = 2.9f * (float)Math.Exp(-t * 2.5f);
            }
            else
            {
                // Outer bulge: smooth transition to disk
                // At r=2000: ~0.3 stars/ly³
                // At r=6000: ~0.01 stars/ly³ (slightly higher than disk average)
                var t = (r - 2000f) / (6000f - 2000f);
                scaleFactor = 0.3f * (float)Math.Exp(-t * 3.5f);
            }
            
            return normalizedDensity * scaleFactor;
        }
        else if (r < 50000)
        {
            // Disk region
            // Average disk density: 0.006 stars/ly³
            // Solar neighborhood (26000 ly): 0.004 stars/ly³
            // The normalized density already handles the exponential decay
            
            // Base disk scaling to achieve average of 0.006 stars/ly³
            float scaleFactor = 0.006f / 0.001f; // Assuming normalized disk density ~0.001
            
            // Slight adjustment for solar neighborhood
            if (Math.Abs(r - 26000) < 2000)
            {
                // Solar neighborhood gets 0.004 instead of 0.006
                scaleFactor *= 0.004f / 0.006f;
            }
            
            return normalizedDensity * scaleFactor;
        }
        else
        {
            // Far outer regions and halo
            // Very low density - 10^-6 of solar neighborhood
            return normalizedDensity * 0.000004f;
        }
    }
    
    /// <summary>
    /// Check if a position should contain a star based on density
    /// </summary>
    public static bool ShouldHaveStar(Vector3 position, Random rng, float threshold = 1.0f)
    {
        var density = CalculateTotalDensity(position);
        return rng.NextDouble() < density * threshold;
    }
    
    /// <summary>
    /// Generate positions within a region using rejection sampling
    /// </summary>
    public static List<Vector3> GenerateStarPositionsInRegion(Vector3 minBounds, Vector3 maxBounds, int targetCount, int seed)
    {
        var positions = new List<Vector3>();
        var rng = new Random(seed);
        
        // Calculate volume for density estimation
        var volume = (maxBounds.X - minBounds.X) * (maxBounds.Y - minBounds.Y) * (maxBounds.Z - minBounds.Z);
        
        // Use rejection sampling
        int attempts = 0;
        int maxAttempts = targetCount * 100; // Prevent infinite loops
        
        while (positions.Count < targetCount && attempts < maxAttempts)
        {
            // Generate random position in bounds
            var x = minBounds.X + (float)rng.NextDouble() * (maxBounds.X - minBounds.X);
            var y = minBounds.Y + (float)rng.NextDouble() * (maxBounds.Y - minBounds.Y);
            var z = minBounds.Z + (float)rng.NextDouble() * (maxBounds.Z - minBounds.Z);
            
            var position = new Vector3(x, y, z);
            
            // Accept/reject based on density
            if (ShouldHaveStar(position, rng))
            {
                positions.Add(position);
            }
            
            attempts++;
        }
        
        return positions;
    }
    
    private static float NormalizeAngle(float angle)
    {
        while (angle > Math.PI) angle -= 2 * (float)Math.PI;
        while (angle < -Math.PI) angle += 2 * (float)Math.PI;
        return angle;
    }
}