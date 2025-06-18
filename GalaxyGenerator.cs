using System;
using System.Collections.Generic;

/// <summary>
/// Unified Galaxy Generator that determines star positions and properties based on location.
/// This is the master algorithm that defines how the galaxy looks.
/// </summary>
public static class GalaxyGenerator
{

  private const float TARGET_SOLAR_DENSITY = 0.004f;
  private static readonly float GLOBAL_DENSITY_SCALE;
    static GalaxyGenerator()
{
    // CORRECT: Sun sits at ~8.13 kpc ≈ 26500 ly in this simulation
    const float SOLAR_RADIUS = 26500f;
    float solarNorm = CalculateTotalDensity(new Vector3(SOLAR_RADIUS, 0f, 0f));
    GLOBAL_DENSITY_SCALE = TARGET_SOLAR_DENSITY / solarNorm;
}
    /// <summary>
    /// Stellar population types based on age and metallicity
    /// </summary>
    public enum StellarPopulation
    {
        ThinDisk,    // Young, metal-rich (Population I)
        ThickDisk,   // Intermediate age/metallicity
        Bulge,       // Old, metal-rich
        Halo         // Old, metal-poor (Population II)
    }
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
    /// Calculate the density of rogue planets at a given position.
    /// For gameplay purposes, rogue planets are very rare - similar to number of stars.
    /// </summary>
    public static float CalculateRoguePlanetDensity(Vector3 position)
    {
        float r = position.Length2D();
        float z = Math.Abs(position.Z);
        float r3d = position.Length();

        // Get stellar density as base
        float stellarDensity = GetExpectedStarDensity(position);

        // Base rogue planet rate - lower for gameplay but not too rare
        // This gives roughly 1 rogue planet per 10-100 stars depending on location
        float baseRate = 0.1f;

        // Halo boost - more rogues in the halo (ejected planets)
        float haloBoost = 1.0f;
        if (r3d > 30_000f) // In halo
        {
            haloBoost = 1.0f + (r3d - 30_000f) / 50_000f; // Up to 2x at edge
        }

        // Dense region penalty - fewer rogues where they'd be captured
        float densityPenalty = 1.0f;
        if (stellarDensity > 0.01f) // Very dense regions (remember this is stars/ly³)
        {
            densityPenalty = 0.3f; // 70% reduction
        }
        else if (stellarDensity > 0.001f) // Moderately dense
        {
            densityPenalty = 0.6f; // 40% reduction
        }

        // Height bonus - slightly more rogues above/below plane
        float heightBonus = 1.0f + Math.Min(z / 5_000f, 0.5f);

        return stellarDensity * baseRate * haloBoost * densityPenalty * heightBonus;
    }

    /// <summary>
    /// Calculate the total stellar density at a given position.
    /// This is the master density function that defines the galaxy's shape.
    /// </summary>
    /// <summary>
    /// Milky Way normalized density model:
    ///   • Sérsic bulge
    ///   • Ferrers bar
    ///   • Double‐exponential disk (sech² vertical)
    ///   • Power‐law halo
    ///   • 4‐armed logarithmic spiral arms
    ///   • Optional warp in the outer disk
    /// </summary>
public static float CalculateTotalDensity(Vector3 pos)
{
    // 1) Cylindrical & spherical radii
    float R   = (float)Math.Sqrt(pos.X*pos.X + pos.Y*pos.Y);
    float z   = Math.Abs(pos.Z);
    float r3d = (float)Math.Sqrt(pos.X*pos.X + pos.Y*pos.Y + pos.Z*pos.Z);

    // 2) Sérsic bulge, now flattened so it falls off faster with height
    const float n            = 1.8f;
    const float Re           = 3300f;
    float       bn           = 2f*n - 1f/3f;
    const float rhoB0        = 0.00086f;
    const float bulgeFlatten = 0.7f;    // z/0.7 → vertical decay ×1.4 faster
    float rEllBulge = (float)Math.Sqrt(
        R*R + (z/bulgeFlatten)*(z/bulgeFlatten)
    );
    float rhoBulge = rhoB0
        * (float)Math.Exp(-bn * (Math.Pow(rEllBulge/Re, 1f/n) - 1f));

    // 3) Ferrers bar (unchanged)
    const float barA     = 5000f, barB = 1500f, barC = 1000f;
    const float barExp   = 2f;
    const float rhoBar0  = 0.2f;
    const float barAngle = 25f * (float)Math.PI/180f;
    float cosφ = (float)Math.Cos(barAngle), sinφ = (float)Math.Sin(barAngle);
    float xRot =  pos.X*cosφ + pos.Y*sinφ;
    float yRot = -pos.X*sinφ + pos.Y*cosφ;
    float m2   = (xRot*xRot)/(barA*barA)
               + (yRot*yRot)/(barB*barB)
               + (pos.Z*pos.Z)/(barC*barC);
    float rhoBar = m2 < 1f
        ? rhoBar0 * (float)Math.Pow(1f - m2, barExp)
        : 0f;

    // 4) Double‐exponential disk (50/50 thin+thick vertical)
    const float Rd        = 8500f;
    const float rhoD0     = 0.0028f;
    const float thinFrac  = 0.50f, thinH  = 350f;
    const float thickFrac = 0.50f, thickH = 1500f;
    float vertFactor = thinFrac  * (float)Math.Exp(-z/thinH)
                     + thickFrac * (float)Math.Exp(-z/thickH);
    float rhoDisk = rhoD0
        * (float)Math.Exp(-R / Rd)
        * vertFactor;

    // 5) Power-law halo, now flattened to fall off faster with height
    const float alpha       = 3.0f;
    const float rhoH0       = 1e-6f;
    const float rscale      = 20000f;
    const float haloFlatten = 0.7f;      // z/0.7 → vertical decay ×1.4 faster
    float rEllHalo = (float)Math.Sqrt(
        R*R + (z/haloFlatten)*(z/haloFlatten)
    );
    float rhoHalo = rhoH0 * (float)Math.Pow(rEllHalo/rscale, -alpha);

    // 6) 4‐armed spiral arms (strong contrast)
    const float armContrast = 0.5f;
    const float armSigma    = 0.3f;
    float pitch = 12f * (float)Math.PI/180f;
    const float r0Arm = 5000f;
    float θ = (float)Math.Atan2(pos.Y, pos.X);
    float armMod = 1f;
    for (int k = 0; k < 4; k++)
    {
        float θ0        = k * (float)(Math.PI/2);
        float expectedθ = θ0 + (float)(Math.Log(R/r0Arm)/Math.Tan(pitch));
        float dθ        = Math.Abs(NormalizeAngle(θ - expectedθ));
        armMod += armContrast * (float)Math.Exp(-dθ*dθ/(2*armSigma*armSigma));
    }

    // 7) Combine all components
    return (rhoBulge + rhoBar + rhoDisk + rhoHalo) * armMod;
}

    /// <summary> Normalize θ into [−π, π] </summary>
    private static float NormalizeAngle(float a)
    {
        // if we ever get ±Infinity or NaN, fold back to 0
        if (float.IsInfinity(a) || float.IsNaN(a))
            return 0f;

        // now safe to normalize into [-π, π]
        const float TWO_PI = 2f * (float)Math.PI;
        while (a > Math.PI) a -= TWO_PI;
        while (a < -Math.PI) a += TWO_PI;
        return a;
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

        // Now using 6 arms total: 2 major, 2 medium, 2 minor
        var armStrength = 0.0f;

        // Pitch angles for different arm types
        var majorPitchAngle = 15f * (float)Math.PI / 180f; // Major arms: 15 degrees
        var mediumPitchAngle = 18f * (float)Math.PI / 180f; // Medium arms: 18 degrees
        var minorPitchAngle = 22f * (float)Math.PI / 180f; // Minor arms: 22 degrees (tighter spiral)

        var r0 = 3000f; // Reference radius where arms start

        // Major arms - 2 arms, 180 degrees apart
        for (int i = 0; i < 2; i++)
        {
            var theta0 = i * (float)Math.PI; // 0 and 180 degrees
            var expectedTheta = theta0 + (float)Math.Log(r / r0) / (float)Math.Tan(majorPitchAngle);
            var angleDiff = (float)Math.Abs(NormalizeAngle(theta - expectedTheta));

            // Wider arms
            var armWidth = 0.18f + (r / GALAXY_RADIUS) * 0.25f;
            var thisArmStrength = (float)Math.Exp(-Math.Pow(angleDiff / armWidth, 2));

            var radialFade = 1.0f;
            if (r < 8000)
            {
                radialFade = (r - 3000) / 5000f;
            }
            else if (r > 40000)
            {
                radialFade = (float)Math.Exp(-Math.Pow((r - 40000) / 10000, 2));
            }

            armStrength += thisArmStrength * radialFade * 0.8f; // Major arms strongest
        }

        // Medium arms - 2 arms, offset by 90 degrees from major arms
        for (int i = 0; i < 2; i++)
        {
            var theta0 = (float)(Math.PI * 0.5 + i * Math.PI); // 90 and 270 degrees
            var expectedTheta = theta0 + (float)Math.Log(r / r0) / (float)Math.Tan(mediumPitchAngle);
            var angleDiff = (float)Math.Abs(NormalizeAngle(theta - expectedTheta));

            // Wider arms
            var armWidth = 0.15f + (r / GALAXY_RADIUS) * 0.2f;
            var thisArmStrength = (float)Math.Exp(-Math.Pow(angleDiff / armWidth, 2));

            var radialFade = 1.0f;
            if (r < 10000)
            {
                radialFade = (r - 3000) / 7000f;
            }
            else if (r > 35000)
            {
                radialFade = (float)Math.Exp(-Math.Pow((r - 35000) / 8000, 2));
            }

            armStrength += thisArmStrength * radialFade * 0.5f; // Medium arms moderate strength
        }

        // Minor arms - 2 arms, offset by 60 degrees from major arms (was 45)
        // These fill in the gaps between major and medium arms
        for (int i = 0; i < 2; i++)
        {
            var theta0 = (float)(Math.PI * 0.333 + i * Math.PI); // 60 and 240 degrees (was 45 and 225)
            var expectedTheta = theta0 + (float)Math.Log(r / r0) / (float)Math.Tan(minorPitchAngle);
            var angleDiff = (float)Math.Abs(NormalizeAngle(theta - expectedTheta));

            // Wider minor arms
            var armWidth = 0.12f + (r / GALAXY_RADIUS) * 0.15f;
            var thisArmStrength = (float)Math.Exp(-Math.Pow(angleDiff / armWidth, 2));

            // Minor arms have different radial profile
            var radialFade = 1.0f;
            if (r < 12000)
            {
                radialFade = (r - 5000) / 7000f;
            }
            else if (r > 30000)
            {
                radialFade = (float)Math.Exp(-Math.Pow((r - 30000) / 6000, 2));
            }

            armStrength += thisArmStrength * radialFade * 0.3f; // Minor arms weakest
        }

        // Additional very faint spiral features to fill remaining gaps
        // These are offset by 120 degrees from major arms (was 135)
        for (int i = 0; i < 2; i++)
        {
            var theta0 = (float)(Math.PI * 0.667 + i * Math.PI); // 120 and 300 degrees (was 135 and 315)
            var expectedTheta = theta0 + (float)Math.Log(r / r0) / (float)Math.Tan(minorPitchAngle);
            var angleDiff = (float)Math.Abs(NormalizeAngle(theta - expectedTheta));

            // Wider faint arms
            var armWidth = 0.1f + (r / GALAXY_RADIUS) * 0.12f;
            var thisArmStrength = (float)Math.Exp(-Math.Pow(angleDiff / armWidth, 2));

            var radialFade = 1.0f;
            if (r < 15000)
            {
                radialFade = (r - 8000) / 7000f;
            }
            else if (r > 28000)
            {
                radialFade = (float)Math.Exp(-Math.Pow((r - 28000) / 5000, 2));
            }

            armStrength += thisArmStrength * radialFade * 0.2f; // Very faint
        }

        // Return multiplier: 1 = inter-arm region, up to 3.5x in major arms
        return 1.0f + armStrength * 2.5f;
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
                    return i == 0 ? "Scutum-Centaurus Arm" : "Perseus Arm";
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
    public static StellarPopulation DeterminePopulation(Vector3 position)
    {
        var r = position.Length2D();
        var z = Math.Abs(position.Z);
        var rTotal = position.Length();

        // Central bulge region
        if (r < 6000) return StellarPopulation.Bulge;

        // Halo - far out or high above/below disk
        if (rTotal > 30000 || (z > 5000 && r > 20000))
            return StellarPopulation.Halo;

        // Thick disk - moderate height above disk
        if (z > 600 || (z > 400 && r > 30000))
            return StellarPopulation.ThickDisk;

        // Thin disk - everything else
        return StellarPopulation.ThinDisk;
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
    // 1) Apply the global calibration
    float dens = normalizedDensity * GLOBAL_DENSITY_SCALE;

    // 2) Never exceed 288 stars/ly³
    return Math.Min(dens, 288f);
}

    /// <summary>
    /// Check if a position should contain a star based on density
    /// </summary>
    public static bool ShouldHaveStar(Vector3 position, Random rng, float threshold = 1.0f)
    {
        var density = CalculateTotalDensity(position);
        return rng.NextDouble() < density * threshold;
    }
}