using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

/// <summary>
/// Improved galaxy visualizer that samples based on density distributions
/// </summary>
public class ScientificGalaxyVisualizer2
{
    private readonly ScientificMilkyWayGenerator generator;
    private readonly Random random = new Random();
    private float zExaggeration = 5.0f; // Default Z exaggeration for side view
    
    public ScientificGalaxyVisualizer2(ScientificMilkyWayGenerator generator)
    {
        this.generator = generator;
    }
    
    /// <summary>
    /// Generate density-based star samples for visualization
    /// </summary>
    private List<ScientificMilkyWayGenerator.Star> GenerateDensityBasedStars(int targetCount)
    {
        Console.WriteLine($"Generating {targetCount:N0} stars using density-based sampling...");
        var stars = new List<ScientificMilkyWayGenerator.Star>();
        
        // Sample different regions with appropriate densities
        // Central regions (0-10000 ly): 50% of stars
        // Use acceptance-rejection for truly smooth density
        int centralStars = (int)(targetCount * 0.50);
        int accepted = 0;
        
        while (accepted < centralStars)
        {
            // Sample position in cylindrical coordinates
            var r = random.NextDouble() * 10000; // 0 to 10,000 ly
            var theta = random.NextDouble() * 2 * Math.PI;
            
            // Calculate density at this radius using smooth power law
            double density;
            
            // Multi-component model with smooth transitions
            // Core region - high density, steep falloff
            var coreDensity = Math.Pow(1 + r / 500, -3.0);
            
            // Bulge component - moderate density, less steep
            var bulgeDensity = 0.3 * Math.Pow(1 + r / 2000, -2.5);
            
            // Disk component - lower density, shallow falloff
            var diskDensity = 0.1 * Math.Pow(1 + r / 5000, -1.5) * Math.Exp(-r / 20000);
            
            // Smooth blending between components
            var coreWeight = Math.Exp(-r / 1500);
            var bulgeWeight = Math.Exp(-r / 5000) * (1 - coreWeight * 0.7);
            var diskWeight = 1 - Math.Exp(-r / 3000);
            
            // Combine with smooth weights
            density = coreDensity * coreWeight + 
                     bulgeDensity * bulgeWeight + 
                     diskDensity * diskWeight;
            
            // Normalize density to [0,1] range
            density = Math.Min(1.0, density);
            
            // Accept or reject based on density
            if (random.NextDouble() < density)
            {
                // Calculate z based on radius - smooth transition from sphere to disk
                double z;
                var flatteningFactor = 1.0 - Math.Exp(-r / 2000); // Smooth 0 to 1
                
                if (flatteningFactor < 0.5)
                {
                    // More spherical distribution but with proper scale height
                    var maxZ = Math.Min(r * 0.5, 1400); // Increased vertical extent for bulge
                    z = (2 * random.NextDouble() - 1) * maxZ * (1 - flatteningFactor);
                }
                else
                {
                    // Disk-like distribution
                    var scaleHeight = 300 * Math.Exp(-r / 8000) + 100;
                    z = random.NextGaussian() * scaleHeight;
                }
                
                var x = (float)(r * Math.Cos(theta));
                var y = (float)(r * Math.Sin(theta));
                
                var pos = new ScientificMilkyWayGenerator.Vector3(x, y, (float)z);
                var star = generator.GenerateStarAtPosition(pos);
                stars.Add(star);
                accepted++;
            }
        }
        
        // Disk and spiral arms (3000-50000 ly): 45% of stars
        int diskStars = (int)(targetCount * 0.45);
        int armStars = 0;
        int interArmStars = 0;
        
        for (int i = 0; i < diskStars; i++)
        {
            // Use exponential disk profile
            var u = random.NextDouble();
            var r = 3000 - 8300 * Math.Log(1 - u * 0.98);
            
            var theta = random.NextDouble() * 2 * Math.PI;
            
            // Calculate distance to nearest spiral arm
            var nearestArmPhase = GetNearestSpiralArmPhase(r, theta);
            var armDistance = Math.Abs(NormalizeAngle(theta - nearestArmPhase.angle));
            var armWidth = r * 0.12; // Arm width ~12% of radius
            
            // Smooth density gradient using error function approximation
            var normalizedDistance = armDistance / (armWidth / r);
            var armDensityFactor = 1.0 + 1.5 * Math.Exp(-normalizedDistance * normalizedDistance);
            
            // Use acceptance-rejection for smooth density variation
            if (random.NextDouble() < armDensityFactor / 2.5) // Max factor is 2.5
            {
                // Accept this position
                // Add slight drift toward arm center for stars near arms
                if (normalizedDistance < 2)
                {
                    var drift = (1 - normalizedDistance / 2) * 0.3;
                    theta = theta + Math.Sign(nearestArmPhase.angle - theta) * drift * armDistance;
                }
                
                if (normalizedDistance < 1) armStars++;
                else interArmStars++;
            }
            else
            {
                // Reject and try new position
                i--; // Retry this star
                continue;
            }
            
            // Smooth vertical distribution with scale height that varies with radius
            var scaleHeight = 300 + (r - 5000) * 0.01; // Flares outward
            var z = (float)(random.NextGaussian() * scaleHeight);
            
            var x = (float)(r * Math.Cos(theta));
            var y = (float)(r * Math.Sin(theta));
            
            var pos = new ScientificMilkyWayGenerator.Vector3(x, y, z);
            var star = generator.GenerateStarAtPosition(pos);
            stars.Add(star);
        }
        
        Console.WriteLine($"  Disk stars: {armStars} in arms, {interArmStars} between arms");
        
        // Outer disk/halo (50000-100000 ly): 5% of stars
        int haloStars = (int)(targetCount * 0.05);
        for (int i = 0; i < haloStars; i++)
        {
            var r = 50000 + random.NextDouble() * 50000;
            var theta = random.NextDouble() * 2 * Math.PI;
            var phi = random.NextDouble() * Math.PI - Math.PI / 2;
            
            var x = (float)(r * Math.Cos(theta) * Math.Cos(phi));
            var y = (float)(r * Math.Sin(theta) * Math.Cos(phi));
            var z = (float)(r * Math.Sin(phi));
            
            var pos = new ScientificMilkyWayGenerator.Vector3(x, y, z);
            var star = generator.GenerateStarAtPosition(pos);
            stars.Add(star);
        }
        
        Console.WriteLine($"Generated {stars.Count:N0} stars with realistic density distribution");
        return stars;
    }
    
    /// <summary>
    /// Get nearest spiral arm phase for given position
    /// </summary>
    private (double angle, int armIndex) GetNearestSpiralArmPhase(double r, double theta)
    {
        // Logarithmic spiral: theta = a * ln(r/r0)
        var pitchAngle = 12.0 * Math.PI / 180.0; // 12 degree pitch
        var r0 = 5000.0; // Reference radius
        
        var spiralBase = Math.Log(r / r0) / Math.Tan(pitchAngle);
        
        double minDistance = double.MaxValue;
        double nearestAngle = 0;
        int nearestArm = 0;
        
        // Check 4 major arms
        for (int i = 0; i < 4; i++)
        {
            var armAngle = spiralBase + i * Math.PI / 2;
            var distance = Math.Abs(NormalizeAngle(theta - armAngle));
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestAngle = armAngle;
                nearestArm = i;
            }
        }
        
        return (nearestAngle, nearestArm);
    }
    
    private double NormalizeAngle(double angle)
    {
        while (angle > Math.PI) angle -= 2 * Math.PI;
        while (angle < -Math.PI) angle += 2 * Math.PI;
        return angle;
    }
    
    public void GenerateAllViews(int width = 2048, int height = 2048, int starCount = 500000, float zExaggeration = 5.0f)
    {
        this.zExaggeration = zExaggeration;
        var stars = GenerateDensityBasedStars(starCount);
        
        GenerateTopView(stars, width, height);
        GenerateSideView(stars, width, height);
        GenerateAngledView(stars, width, height);
        GenerateCompositeView(stars, width * 2, height * 2);
    }
    
    private void GenerateTopView(List<ScientificMilkyWayGenerator.Star> stars, int width, int height)
    {
        Console.WriteLine("Generating top view...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            DrawStars(canvas, stars, width, height, ViewProjection.Top);
            DrawGalaxyInfo(canvas, "Top View (X-Y Plane)", width, height, stars.Count);
            
            SaveImage(surface, "MilkyWay_TopView_Density.png");
        }
    }
    
    private void GenerateSideView(List<ScientificMilkyWayGenerator.Star> stars, int width, int height)
    {
        Console.WriteLine("Generating side view...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            DrawStars(canvas, stars, width, height, ViewProjection.Side);
            DrawGalaxyInfo(canvas, "Side View (X-Z Plane)", width, height, stars.Count);
            
            SaveImage(surface, "MilkyWay_SideView_Density.png");
        }
    }
    
    private void GenerateAngledView(List<ScientificMilkyWayGenerator.Star> stars, int width, int height)
    {
        Console.WriteLine("Generating angled 3D view...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            DrawStars(canvas, stars, width, height, ViewProjection.Angled3D);
            DrawGalaxyInfo(canvas, "3D View (45° angle)", width, height, stars.Count);
            
            SaveImage(surface, "MilkyWay_3DView_Density.png");
        }
    }
    
    private void GenerateCompositeView(List<ScientificMilkyWayGenerator.Star> stars, int width, int height)
    {
        Console.WriteLine("Generating composite view...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            int subWidth = width / 2;
            int subHeight = height / 2;
            
            // Top-left: Top view
            canvas.Save();
            canvas.ClipRect(new SKRect(0, 0, subWidth, subHeight));
            DrawStars(canvas, stars, subWidth, subHeight, ViewProjection.Top);
            DrawGalaxyInfo(canvas, "Top View", subWidth, subHeight, stars.Count);
            canvas.Restore();
            
            // Top-right: Side view
            canvas.Save();
            canvas.ClipRect(new SKRect(subWidth, 0, width, subHeight));
            canvas.Translate(subWidth, 0);
            DrawStars(canvas, stars, subWidth, subHeight, ViewProjection.Side);
            DrawGalaxyInfo(canvas, "Side View", subWidth, subHeight, stars.Count);
            canvas.Restore();
            
            // Bottom-left: Angled view
            canvas.Save();
            canvas.ClipRect(new SKRect(0, subHeight, subWidth, height));
            canvas.Translate(0, subHeight);
            DrawStars(canvas, stars, subWidth, subHeight, ViewProjection.Angled3D);
            DrawGalaxyInfo(canvas, "3D View", subWidth, subHeight, stars.Count);
            canvas.Restore();
            
            // Bottom-right: Density map
            canvas.Save();
            canvas.ClipRect(new SKRect(subWidth, subHeight, width, height));
            canvas.Translate(subWidth, subHeight);
            DrawDensityMap(canvas, subWidth, subHeight);
            canvas.Restore();
            
            // Title
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.White;
                paint.TextSize = 60;
                paint.IsAntialias = true;
                paint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
                
                string title = "Scientific Milky Way - Density-Based Visualization";
                var bounds = new SKRect();
                paint.MeasureText(title, ref bounds);
                canvas.DrawText(title, (width - bounds.Width) / 2, 60, paint);
            }
            
            SaveImage(surface, "MilkyWay_Composite_Density.png");
        }
    }
    
    private void DrawStars(SKCanvas canvas, List<ScientificMilkyWayGenerator.Star> stars, 
        int width, int height, ViewProjection projection)
    {
        var scale = Math.Min(width, height) / 120000.0f; // 60,000 ly radius
        var centerX = width / 2f;
        var centerY = height / 2f;
        
        // Sort stars by distance for proper layering in 3D view
        if (projection == ViewProjection.Angled3D)
        {
            stars = stars.OrderBy(s => -ProjectPoint(s.Position, projection).Z).ToList();
        }
        
        using (var paint = new SKPaint())
        {
            paint.IsAntialias = true;
            paint.BlendMode = SKBlendMode.Plus;
            
            foreach (var star in stars)
            {
                var projected = ProjectPoint(star.Position, projection);
                var x = centerX + projected.X * scale;
                var y = centerY - projected.Y * scale;
                
                if (x < 0 || x >= width || y < 0 || y >= height) continue;
                
                // Fixed size for all stars - no mass scaling
                float size = 0.8f;
                
                // Adjust size slightly based on distance in 3D view
                if (projection == ViewProjection.Angled3D)
                {
                    var depthFactor = 1f - (projected.Z + 50000) / 100000f;
                    size *= (0.5f + 0.5f * depthFactor);
                }
                
                // Brightness based on temperature and luminosity
                var brightness = Math.Min(1, Math.Sqrt(star.Luminosity) * 0.5);
                var alpha = (byte)(brightness * 200);
                
                paint.Color = new SKColor(
                    (byte)(star.Color.X * 255),
                    (byte)(star.Color.Y * 255),
                    (byte)(star.Color.Z * 255),
                    alpha
                );
                
                canvas.DrawCircle(x, y, size, paint);
            }
        }
    }
    
    private void DrawDensityMap(SKCanvas canvas, int width, int height)
    {
        using (var paint = new SKPaint())
        {
            paint.Color = SKColors.White;
            paint.TextSize = 24;
            paint.IsAntialias = true;
            paint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
            
            canvas.DrawText("Stellar Density Map", 20, 40, paint);
            
            // Draw density gradient legend
            var gradientRect = new SKRect(40, 80, width - 40, 120);
            using (var shader = SKShader.CreateLinearGradient(
                new SKPoint(gradientRect.Left, 0),
                new SKPoint(gradientRect.Right, 0),
                new[] { SKColors.Black, SKColors.DarkBlue, SKColors.Blue, SKColors.Cyan, SKColors.Yellow, SKColors.White },
                null,
                SKShaderTileMode.Clamp))
            {
                paint.Shader = shader;
                canvas.DrawRect(gradientRect, paint);
            }
            
            paint.Shader = null;
            paint.TextSize = 16;
            paint.Color = SKColors.White;
            canvas.DrawText("Low", 40, 145, paint);
            canvas.DrawText("High", width - 80, 145, paint);
            canvas.DrawText("Stellar Density", width / 2 - 50, 145, paint);
            
            // Draw radial density profile
            paint.TextSize = 18;
            canvas.DrawText("Radial Density Profile", 20, 180, paint);
            
            paint.StrokeWidth = 2;
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = new SKColor(100, 100, 100);
            
            var graphRect = new SKRect(40, 200, width - 40, height - 60);
            canvas.DrawRect(graphRect, paint);
            
            // Draw density curve
            paint.Color = SKColors.Cyan;
            var path = new SKPath();
            
            for (int i = 0; i <= 100; i++)
            {
                var r = i * 600; // 0 to 60,000 ly
                var density = CalculateDensity(r);
                
                var x = graphRect.Left + (graphRect.Width * i / 100);
                var y = graphRect.Bottom - (float)(Math.Log10(density + 1) / 6 * graphRect.Height);
                
                if (i == 0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }
            
            canvas.DrawPath(path, paint);
            
            // Labels
            paint.Style = SKPaintStyle.Fill;
            paint.TextSize = 14;
            paint.Color = SKColors.White;
            canvas.DrawText("0", graphRect.Left - 20, graphRect.Bottom + 20, paint);
            canvas.DrawText("60k ly", graphRect.Right - 40, graphRect.Bottom + 20, paint);
            canvas.DrawText("Distance from Center", graphRect.MidX - 60, graphRect.Bottom + 40, paint);
        }
    }
    
    private double CalculateDensity(double r)
    {
        // Power law components matching the sampling
        var coreDensity = 1e5 * Math.Pow(1 + r / 500, -3.0);
        var bulgeDensity = 3e4 * Math.Pow(1 + r / 2000, -2.5);
        var diskDensity = 1e4 * Math.Pow(1 + r / 5000, -1.5) * Math.Exp(-r / 20000);
        
        // Smooth blending
        var coreWeight = Math.Exp(-r / 1500);
        var bulgeWeight = Math.Exp(-r / 5000) * (1 - coreWeight * 0.7);
        var diskWeight = 1 - Math.Exp(-r / 3000);
        
        return coreDensity * coreWeight + 
               bulgeDensity * bulgeWeight + 
               diskDensity * diskWeight;
    }
    
    private void DrawGalaxyInfo(SKCanvas canvas, string viewName, int width, int height, int starCount)
    {
        using (var paint = new SKPaint())
        {
            paint.Color = SKColors.White;
            paint.TextSize = 24;
            paint.IsAntialias = true;
            
            canvas.DrawText(viewName, 20, 40, paint);
            
            paint.TextSize = 16;
            paint.Color = new SKColor(200, 200, 200);
            canvas.DrawText($"Stars: {starCount:N0} (density-based sampling)", 20, 65, paint);
            canvas.DrawText("Scale: 1 pixel ≈ 60 light-years", 20, 85, paint);
            
            // Scale bar
            paint.StrokeWidth = 2;
            paint.Style = SKPaintStyle.Stroke;
            canvas.DrawLine(20, height - 40, 220, height - 40, paint);
            paint.Style = SKPaintStyle.Fill;
            canvas.DrawText("12,000 light-years", 60, height - 45, paint);
        }
    }
    
    private ScientificMilkyWayGenerator.Vector3 ProjectPoint(ScientificMilkyWayGenerator.Vector3 point, ViewProjection projection)
    {
        switch (projection)
        {
            case ViewProjection.Top:
                return new ScientificMilkyWayGenerator.Vector3(point.X, point.Y, 0);
                
            case ViewProjection.Side:
                return new ScientificMilkyWayGenerator.Vector3(point.X, point.Z * zExaggeration, 0); // Configurable Z exaggeration
                
            case ViewProjection.Angled3D:
                // Rotate around X axis by 45 degrees, then around Z by 30 degrees
                var cosX = Math.Cos(Math.PI / 4);
                var sinX = Math.Sin(Math.PI / 4);
                var y1 = point.Y * cosX - point.Z * sinX;
                var z1 = point.Y * sinX + point.Z * cosX;
                
                var cosZ = Math.Cos(Math.PI / 6);
                var sinZ = Math.Sin(Math.PI / 6);
                var x2 = point.X * cosZ - y1 * sinZ;
                var y2 = point.X * sinZ + y1 * cosZ;
                
                return new ScientificMilkyWayGenerator.Vector3((float)x2, (float)y2, (float)z1);
                
            default:
                return point;
        }
    }
    
    private void SaveImage(SKSurface surface, string filename)
    {
        using (var image = surface.Snapshot())
        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
        using (var stream = System.IO.File.OpenWrite(filename))
        {
            data.SaveTo(stream);
        }
        
        Console.WriteLine($"✓ Saved {filename}");
    }
    
    private enum ViewProjection
    {
        Top,
        Side,
        Angled3D
    }
}

// Extension for Gaussian distribution
public static class RandomExtensions
{
    public static double NextGaussian(this Random rand)
    {
        double u1 = 1.0 - rand.NextDouble();
        double u2 = 1.0 - rand.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}