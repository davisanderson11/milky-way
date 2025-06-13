using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Advanced galaxy statistics using analytical density models
/// </summary>
public static class AdvancedGalaxyStatistics
{
    // Physical constants for the Milky Way
    private const double DISK_SCALE_LENGTH = 2600; // ly
    private const double DISK_SCALE_HEIGHT = 300; // ly (vertical scale height)
    private const double THIN_DISK_HEIGHT = 300; // ly
    private const double THICK_DISK_HEIGHT = 1000; // ly
    private const double BULGE_RADIUS = 3000; // ly
    private const double CORE_RADIUS = 500; // ly
    private const double TOTAL_STARS = 400e9; // 400 billion stars
    private const double GALAXY_RADIUS = 50000; // ly
    private const double SUN_DISTANCE = 26000; // ly from center
    
    // Spiral arm enhancement factor
    private const double ARM_DENSITY_FACTOR = 2.5; // Arms are 2.5x denser than inter-arm
    
    /// <summary>
    /// Calculate stellar density at a given position (stars per cubic light-year)
    /// </summary>
    public static double CalculateStellarDensity(double r, double z, double theta = 0)
    {
        // Core region - extremely dense
        if (r < CORE_RADIUS)
        {
            double coreDensity = 100.0 * Math.Exp(-r / 100); // ~100 stars/ly³ at center
            return coreDensity;
        }
        
        // Bulge region
        if (r < BULGE_RADIUS)
        {
            double bulgeNorm = Math.Pow(1 + r / 500, -3.5); // Hernquist-like profile
            double bulgeDensity = 10.0 * bulgeNorm * Math.Exp(-Math.Abs(z) / 500);
            return bulgeDensity;
        }
        
        // Disk region
        double diskDensity = 0;
        
        // Thin disk component with smoother transition
        double thinDiskNorm = Math.Exp(-r / DISK_SCALE_LENGTH);
        double thinDiskVertical = Math.Exp(-Math.Abs(z) / THIN_DISK_HEIGHT);
        
        double thinDiskDensity = 0.14 * thinDiskNorm * thinDiskVertical;
        
        // Thick disk component
        double thickDiskNorm = Math.Exp(-r / (DISK_SCALE_LENGTH * 1.5));
        double thickDiskVertical = Math.Exp(-Math.Abs(z) / THICK_DISK_HEIGHT);
        double thickDiskDensity = 0.01 * thickDiskNorm * thickDiskVertical;
        
        diskDensity = thinDiskDensity + thickDiskDensity;
        
        // Spiral arm enhancement
        double armEnhancement = CalculateSpiralArmEnhancement(r, theta);
        diskDensity *= armEnhancement;
        
        // Halo component (very sparse)
        if (r > 30000 || Math.Abs(z) > 5000)
        {
            double haloDensity = 0.0001 * Math.Pow(1 + r / 20000, -3);
            diskDensity += haloDensity;
        }
        
        return diskDensity;
    }
    
    /// <summary>
    /// Calculate spiral arm density enhancement
    /// </summary>
    private static double CalculateSpiralArmEnhancement(double r, double theta)
    {
        if (r < 5000 || r > 40000) return 1.0; // No arms in core or far outer regions
        
        // Logarithmic spiral parameters
        double pitchAngle = 12.0 * Math.PI / 180.0; // 12 degree pitch
        double r0 = 5000.0;
        
        // Calculate distance to nearest spiral arm
        double spiralPhase = Math.Log(r / r0) / Math.Tan(pitchAngle);
        double minArmDistance = double.MaxValue;
        
        // Check 4 major arms (90 degrees apart)
        for (int i = 0; i < 4; i++)
        {
            double armAngle = spiralPhase + i * Math.PI / 2;
            double angleDiff = Math.Abs(NormalizeAngle(theta - armAngle));
            double armDistance = r * angleDiff; // Approximate linear distance
            minArmDistance = Math.Min(minArmDistance, armDistance);
        }
        
        // Gaussian profile for arm density
        double armWidth = r * 0.1; // Arm width scales with radius (~10% of radius)
        double enhancement = 1.0 + (ARM_DENSITY_FACTOR - 1.0) * Math.Exp(-minArmDistance * minArmDistance / (2 * armWidth * armWidth));
        
        return enhancement;
    }
    
    /// <summary>
    /// Calculate average stellar separation at a given position
    /// </summary>
    public static double CalculateAverageStellarSeparation(double r, double z, double theta = 0)
    {
        double density = CalculateStellarDensity(r, z, theta);
        if (density <= 0) return double.PositiveInfinity;
        
        // Average separation = (1/density)^(1/3)
        // This assumes uniform distribution in a volume
        double separation = Math.Pow(1.0 / density, 1.0 / 3.0);
        
        // Apply a correction factor for disk geometry
        if (Math.Abs(z) < THIN_DISK_HEIGHT && r > BULGE_RADIUS)
        {
            // In the thin disk, stars are more spread out horizontally
            separation *= 1.2;
        }
        
        return separation;
    }
    
    /// <summary>
    /// Generate comprehensive statistics report
    /// </summary>
    public static void GenerateReport()
    {
        Console.WriteLine("\n=== Advanced Galaxy Statistics (Analytical) ===\n");
        
        // Regional statistics
        Console.WriteLine("=== Stellar Densities and Separations by Region ===");
        Console.WriteLine("{0,-25} | {1,-20} | {2,-15} | {3,-20}", 
            "Region", "Density (stars/ly³)", "Avg Separation", "Total Stars (est.)");
        Console.WriteLine(new string('-', 85));
        
        var regions = new (string name, double r, double z, double volumeSize, double height)[]
        {
            ("Galactic Core", 0, 0, CORE_RADIUS, 100),
            ("Galactic Bulge", 1500, 0, BULGE_RADIUS, 1000),
            ("Inner Disk", 8000, 0, 15000, 300),
            ("Solar Neighborhood", SUN_DISTANCE, 0, 5000, 300),
            ("Solar Position (in arm)", SUN_DISTANCE, 0, 10, 10),
            ("Between Arms @ Sun", SUN_DISTANCE, Math.PI/4, 10, 10), // 45° off arm
            ("Outer Disk", 35000, 0, 20000, 500),
            ("Thick Disk", 15000, 800, 10000, 1000),
            ("Halo", 30000, 10000, 50000, 5000)
        };
        
        foreach (var (name, r, z, volumeSize, height) in regions)
        {
            double density = CalculateStellarDensity(r, z);
            double separation = CalculateAverageStellarSeparation(r, z);
            
            // Estimate total stars in region
            double volume = Math.PI * volumeSize * volumeSize * height;
            double totalStars = density * volume;
            
            Console.WriteLine("{0,-25} | {1,-20:E2} | {2,-15:F1} ly | {3,-20:E2}", 
                name, density, separation, totalStars);
        }
        
        // Specific solar neighborhood analysis
        Console.WriteLine("\n=== Solar Neighborhood Detail ===");
        double sunDensity = CalculateStellarDensity(SUN_DISTANCE, 0, 0.7); // Sun is in Local Arm
        double sunSeparation = CalculateAverageStellarSeparation(SUN_DISTANCE, 0, 0.7);
        
        Console.WriteLine($"Stellar density at Sun's position: {sunDensity:F4} stars/ly³");
        Console.WriteLine($"Average stellar separation at Sun: {sunSeparation:F1} ly");
        Console.WriteLine($"Nearest neighbor distance (statistical): {sunSeparation * 0.554:F1} ly"); // Poisson factor
        
        // Density profile along radial direction
        Console.WriteLine("\n=== Radial Density Profile (z=0, in spiral arm) ===");
        Console.WriteLine("{0,-15} | {1,-20} | {2,-15}", "Distance (ly)", "Density (stars/ly³)", "Avg Separation (ly)");
        Console.WriteLine(new string('-', 55));
        
        for (double r = 100; r <= 50000; r *= 1.5)
        {
            double density = CalculateStellarDensity(r, 0, 0);
            double separation = CalculateAverageStellarSeparation(r, 0, 0);
            Console.WriteLine("{0,-15:F0} | {1,-20:E2} | {2,-15:F1}", r, density, separation);
        }
        
        // Vertical density profile
        Console.WriteLine("\n=== Vertical Density Profile (r=26,000 ly) ===");
        Console.WriteLine("{0,-15} | {1,-20} | {2,-15}", "Height (ly)", "Density (stars/ly³)", "Avg Separation (ly)");
        Console.WriteLine(new string('-', 55));
        
        for (double z = 0; z <= 5000; z += 250)
        {
            double density = CalculateStellarDensity(SUN_DISTANCE, z);
            double separation = CalculateAverageStellarSeparation(SUN_DISTANCE, z);
            Console.WriteLine("{0,-15:F0} | {1,-20:E2} | {2,-15:F1}", z, density, separation);
        }
        
        // Spiral arm analysis
        Console.WriteLine("\n=== Spiral Arm Density Enhancement ===");
        Console.WriteLine("Distance from arm center vs density at r=26,000 ly:");
        
        double baseDensity = CalculateStellarDensity(SUN_DISTANCE, 0, Math.PI/4); // Inter-arm
        Console.WriteLine($"Inter-arm density: {baseDensity:E2} stars/ly³");
        
        double armDensity = CalculateStellarDensity(SUN_DISTANCE, 0, 0); // In arm
        Console.WriteLine($"In-arm density: {armDensity:E2} stars/ly³");
        Console.WriteLine($"Enhancement factor: {armDensity/baseDensity:F1}x");
        
        // Total star count verification
        Console.WriteLine("\n=== Total Star Count Estimate ===");
        double totalEstimate = EstimateTotalStars();
        Console.WriteLine($"Integrated total stars: {totalEstimate:E2}");
        Console.WriteLine($"Target total stars: {TOTAL_STARS:E2}");
        Console.WriteLine($"Ratio: {totalEstimate/TOTAL_STARS:F2}");
    }
    
    /// <summary>
    /// Estimate total number of stars by integrating density
    /// </summary>
    private static double EstimateTotalStars()
    {
        double total = 0;
        double dr = 500; // 500 ly steps
        double dz = 100; // 100 ly steps
        double dtheta = Math.PI / 20; // 9 degree steps
        
        for (double r = 0; r < GALAXY_RADIUS; r += dr)
        {
            for (double z = -5000; z <= 5000; z += dz)
            {
                for (double theta = 0; theta < 2 * Math.PI; theta += dtheta)
                {
                    double density = CalculateStellarDensity(r + dr/2, z + dz/2, theta + dtheta/2);
                    double volume = r * dr * dtheta * dz; // Cylindrical volume element
                    total += density * volume;
                }
            }
        }
        
        return total;
    }
    
    private static double NormalizeAngle(double angle)
    {
        while (angle > Math.PI) angle -= 2 * Math.PI;
        while (angle < -Math.PI) angle += 2 * Math.PI;
        return angle;
    }
}