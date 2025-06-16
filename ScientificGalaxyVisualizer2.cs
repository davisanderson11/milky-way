using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

/// <summary>
/// Improved galaxy visualizer that uses the unified GalaxyGenerator with efficient importance sampling
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
    /// Generate density-based star samples for visualization using rejection sampling
    /// </summary>
    private List<ScientificMilkyWayGenerator.Star> GenerateDensityBasedStars(int targetCount)
    {
        Console.WriteLine($"Generating {targetCount:N0} stars using rejection sampling...");
        var stars = new List<ScientificMilkyWayGenerator.Star>();
        
        // Use rejection sampling with bias towards outer regions
        // This compensates for the logarithmic nature of density calculations
        
        int generated = 0;
        int attempts = 0;
        
        while (generated < targetCount)
        {
            // Use square root distribution for radius to bias towards outer regions
            // This gives more samples in areas where volume is larger
            var u = random.NextDouble();
            var r = Math.Sqrt(u) * 60000; // Square root biases towards larger radii
            var theta = random.NextDouble() * 2 * Math.PI;
            
            // Calculate z based on radius with appropriate scale height
            // Match the scale heights from GalaxyGenerator.CalculateDiskDensity
            double z;
            double scaleHeight;
            
            if (r < 1000)
            {
                // Central region - nearly spherical
                scaleHeight = 800;
            }
            else if (r < 6000)
            {
                // Bulge region - smooth transition
                var t = (r - 1000) / 5000.0;
                t = t * t * (3 - 2 * t); // Smoothstep
                scaleHeight = 800 * (1 - t) + 300 * t;
            }
            else
            {
                // Disk region with slight flaring
                scaleHeight = 300 + (r / 100000.0) * 300;
            }
            
            // Use Gaussian distribution for all regions
            // This matches the exp(-z²/2σ²) profile in the density formula
            z = random.NextGaussian() * scaleHeight;
            
            var xPos = (float)(r * Math.Cos(theta));
            var yPos = (float)(r * Math.Sin(theta));
            
            var testPos = new GalaxyGenerator.Vector3(xPos, yPos, (float)z);
            
            // Use unified generator's density calculation
            var density = GalaxyGenerator.CalculateTotalDensity(testPos);
            
            // Rejection sampling with volume compensation
            // The volume at radius r is proportional to r, so we compensate
            var volumeFactor = r / 60000.0f; // Normalize by max radius
            var adjustedDensity = density * Math.Pow(volumeFactor, 0.5); // Square root to not overcompensate
            
            // Accept or reject based on density
            if (random.NextDouble() < adjustedDensity)
            {
                var pos = new ScientificMilkyWayGenerator.Vector3(xPos, yPos, (float)z);
                var star = generator.GenerateStarAtPosition(pos);
                stars.Add(star);
                generated++;
            }
            
            attempts++;
            
            // Progress reporting every million attempts
            if (attempts % 1000000 == 0)
            {
                Console.WriteLine($"  Generated {generated:N0}/{targetCount:N0} stars ({(float)generated/targetCount*100:F1}%)...");
            }
        }
        
        Console.WriteLine($"Generated {stars.Count:N0} stars in {attempts:N0} attempts");
        Console.WriteLine($"Acceptance rate: {(float)generated/attempts*100:F2}%");
        return stars;
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
            
            SaveImage(surface, "MilkyWay_TopView_Unified.png");
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
            DrawGalaxyInfo(canvas, $"Side View (X-Z Plane, {zExaggeration:F1}x Z)", width, height, stars.Count);
            
            SaveImage(surface, "MilkyWay_SideView_Unified.png");
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
            
            SaveImage(surface, "MilkyWay_3DView_Unified.png");
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
                
                string title = "Scientific Milky Way - Unified Generator";
                var bounds = new SKRect();
                paint.MeasureText(title, ref bounds);
                canvas.DrawText(title, (width - bounds.Width) / 2, 60, paint);
            }
            
            SaveImage(surface, "MilkyWay_Composite_Unified.png");
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
            
            // Draw radial density profile using unified generator
            paint.TextSize = 18;
            canvas.DrawText("Radial Density Profile (Unified)", 20, 180, paint);
            
            paint.StrokeWidth = 2;
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = new SKColor(100, 100, 100);
            
            var graphRect = new SKRect(40, 200, width - 40, height - 60);
            canvas.DrawRect(graphRect, paint);
            
            // Draw density curve from unified generator
            paint.Color = SKColors.Cyan;
            var path = new SKPath();
            
            for (int i = 0; i <= 100; i++)
            {
                var r = i * 600; // 0 to 60,000 ly
                var pos = new GalaxyGenerator.Vector3((float)r, 0, 0);
                var density = GalaxyGenerator.CalculateTotalDensity(pos);
                
                var x = graphRect.Left + (graphRect.Width * i / 100);
                var y = graphRect.Bottom - (float)(density * graphRect.Height);
                
                if (i == 0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }
            
            canvas.DrawPath(path, paint);
            
            // Draw spiral arm positions
            paint.Color = new SKColor(255, 200, 100, 100);
            paint.StrokeWidth = 1;
            for (int arm = 0; arm < 4; arm++)
            {
                var armPath = new SKPath();
                for (int i = 10; i <= 80; i++)
                {
                    var r = i * 600;
                    var theta = arm * Math.PI / 2 + (r / 60000f) * 2.094f; // 120 degree curl
                    var pos = new GalaxyGenerator.Vector3(
                        (float)(r * Math.Cos(theta)),
                        (float)(r * Math.Sin(theta)),
                        0
                    );
                    var spiralDensity = GalaxyGenerator.CalculateSpiralArmMultiplier(pos);
                    
                    if (spiralDensity > 1.1f)
                    {
                        var x = graphRect.Left + (graphRect.Width * i / 100);
                        var y = graphRect.Bottom - 10;
                        canvas.DrawCircle(x, y, 2, paint);
                    }
                }
            }
            
            // Labels
            paint.Style = SKPaintStyle.Fill;
            paint.TextSize = 14;
            paint.Color = SKColors.White;
            canvas.DrawText("0", graphRect.Left - 20, graphRect.Bottom + 20, paint);
            canvas.DrawText("60k ly", graphRect.Right - 40, graphRect.Bottom + 20, paint);
            canvas.DrawText("Distance from Center", graphRect.MidX - 60, graphRect.Bottom + 40, paint);
        }
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
            canvas.DrawText($"Stars: {starCount:N0} (unified generator)", 20, 65, paint);
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
    
    /// <summary>
    /// Generate pure formula-based density heatmaps without any star sampling
    /// </summary>
    public void GenerateDensityHeatmaps(int width, int height, float verticalScale = 1.0f)
    {
        Console.WriteLine($"\nGenerating pure mathematical density heatmaps (vertical scale: {verticalScale}x)...");
        
        // Generate individual views
        GenerateDensityHeatmapTopView(width, height);
        GenerateDensityHeatmapSideView(width, height, verticalScale);
        GenerateDensityHeatmapArmView(width, height);
        GenerateDensityHeatmapRogueView(width, height);
        GenerateDensityHeatmapComposite(width * 2, height * 2, verticalScale);
    }
    
    private void GenerateDensityHeatmapTopView(int width, int height)
    {
        Console.WriteLine("Generating top-down density heatmap...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            // Calculate density at each pixel
            var scale = 120000.0f / Math.Min(width, height); // 60k ly radius
            
            using (var paint = new SKPaint())
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Convert pixel to galaxy coordinates
                        var galX = (x - width / 2f) * scale;
                        var galY = (height / 2f - y) * scale; // Flip Y
                        
                        // Calculate density at this point (z=0 for top view)
                        var pos = new GalaxyGenerator.Vector3(galX, galY, 0);
                        var density = GalaxyGenerator.CalculateTotalDensity(pos);
                        
                        // Apply logarithmic scaling to see full structure
                        // log10(density + 0.001) maps [0,1] to roughly [-3, 0]
                        // Then normalize to [0,1] range
                        var logDensity = (float)(Math.Log10(density + 0.001) + 3) / 3f;
                        logDensity = Math.Max(0, Math.Min(1, logDensity));
                        
                        // Convert density to color using a heat color map
                        var color = GetHeatmapColor(logDensity);
                        paint.Color = color;
                        canvas.DrawPoint(x, y, paint);
                    }
                    
                    // Progress update
                    if (y % 100 == 0)
                    {
                        Console.WriteLine($"  Progress: {(float)y/height*100:F1}%");
                    }
                }
            }
            
            // Add labels and scale
            DrawHeatmapInfo(canvas, "Density Heatmap - Top View (Z=0) [Log Scale]", width, height);
            SaveImage(surface, "MilkyWay_DensityHeatmap_Top.png");
        }
    }
    
    private void GenerateDensityHeatmapSideView(int width, int height, float verticalScale = 1.0f)
    {
        Console.WriteLine($"Generating side density heatmap (vertical scale: {verticalScale}x)...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            // For side view, we'll show X-Z plane
            var scaleX = 120000.0f / width;
            // At scale 1.0, show same range as X (120k ly) for realistic proportions
            // At scale 5.0, show 1/5th of that range (24k ly) to stretch vertically
            var zRange = 120000.0f / verticalScale;
            var scaleZ = zRange / height;
            
            using (var paint = new SKPaint())
            {
                for (int y = 0; y < height; y += 2) // Skip every other pixel for performance
                {
                    for (int x = 0; x < width; x += 2)
                    {
                        var galX = (x - width / 2f) * scaleX;
                        var galZ = (height / 2f - y) * scaleZ;
                        
                        // For side view, sample at Y=0 (simplify instead of integrating)
                        var pos = new GalaxyGenerator.Vector3(galX, 0, galZ);
                        var density = GalaxyGenerator.CalculateTotalDensity(pos);
                        
                        // Apply logarithmic scaling
                        var logDensity = (float)(Math.Log10(density + 0.001) + 3) / 3f;
                        logDensity = Math.Max(0, Math.Min(1, logDensity));
                        
                        var color = GetHeatmapColor(logDensity);
                        paint.Color = color;
                        canvas.DrawRect(x, y, 2, 2, paint);
                    }
                    
                    // Progress update
                    if (y % 100 == 0)
                    {
                        Console.WriteLine($"  Progress: {(float)y/height*100:F1}%");
                    }
                }
            }
            
            DrawHeatmapInfo(canvas, "Density Heatmap - Side View (Y integrated) [Log Scale]", width, height);
            SaveImage(surface, "MilkyWay_DensityHeatmap_Side.png");
        }
    }
    
    private void GenerateDensityHeatmapArmView(int width, int height)
    {
        Console.WriteLine("Generating spiral arm density heatmap...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            // Show only spiral arm enhancement
            var scale = 120000.0f / Math.Min(width, height);
            
            using (var paint = new SKPaint())
            {
                for (int y = 0; y < height; y += 2)
                {
                    for (int x = 0; x < width; x += 2)
                    {
                        var galX = (x - width / 2f) * scale;
                        var galY = (height / 2f - y) * scale;
                        
                        var pos = new GalaxyGenerator.Vector3(galX, galY, 0);
                        
                        // Get only the spiral arm density enhancement
                        var spiralDensity = GalaxyGenerator.CalculateSpiralArmMultiplier(pos);
                        
                        // Show enhancement above background (spiralDensity ranges from 1.0 to ~3.0)
                        var enhancement = (spiralDensity - 1.0f) / 2.0f; // Normalize to 0-1 range
                        enhancement = Math.Max(0, Math.Min(1, enhancement));
                        
                        // Use a different color scheme for arms
                        var color = GetArmEnhancementColor(enhancement);
                        paint.Color = color;
                        canvas.DrawRect(x, y, 2, 2, paint);
                    }
                    
                    if (y % 100 == 0)
                    {
                        Console.WriteLine($"  Progress: {(float)y/height*100:F1}%");
                    }
                }
            }
            
            DrawHeatmapInfo(canvas, "Spiral Arm Enhancement Map [Linear Scale]", width, height);
            SaveImage(surface, "MilkyWay_DensityHeatmap_Arms.png");
        }
    }
    
    private void GenerateDensityHeatmapRogueView(int width, int height)
    {
        Console.WriteLine("Generating rogue planet density heatmap...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            // Show rogue planet density
            var scale = 120000.0f / Math.Min(width, height);
            
            using (var paint = new SKPaint())
            {
                for (int y = 0; y < height; y += 2)
                {
                    for (int x = 0; x < width; x += 2)
                    {
                        var galX = (x - width / 2f) * scale;
                        var galY = (height / 2f - y) * scale;
                        
                        var pos = new GalaxyGenerator.Vector3(galX, galY, 0);
                        
                        // Get rogue planet density
                        var rogueDensity = GalaxyGenerator.CalculateRoguePlanetDensity(pos);
                        
                        // Apply logarithmic scaling similar to stars
                        // Since rogues are ~1/10000 of stars, add offset to make visible
                        var logDensity = (float)(Math.Log10(rogueDensity + 0.0000001) + 7) / 7f;
                        logDensity = Math.Max(0, Math.Min(1, logDensity));
                        
                        // Use purple color scheme for rogues
                        var color = GetRoguePlanetColor(logDensity);
                        paint.Color = color;
                        canvas.DrawRect(x, y, 2, 2, paint);
                    }
                    
                    if (y % 100 == 0)
                    {
                        Console.WriteLine($"  Progress: {(float)y/height*100:F1}%");
                    }
                }
            }
            
            DrawHeatmapInfo(canvas, "Rogue Planet Density Map [Log Scale]", width, height);
            SaveImage(surface, "MilkyWay_DensityHeatmap_Rogues.png");
        }
    }
    
    private void GenerateDensityHeatmapComposite(int width, int height, float verticalScale = 1.0f)
    {
        Console.WriteLine($"Generating composite density heatmap (vertical scale: {verticalScale}x)...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            int subWidth = width / 2;
            int subHeight = height / 2;
            
            // Top-left: Top view
            canvas.Save();
            canvas.ClipRect(new SKRect(0, 0, subWidth, subHeight));
            DrawDensityHeatmapQuadrant(canvas, subWidth, subHeight, "top");
            canvas.Restore();
            
            // Top-right: Side view
            canvas.Save();
            canvas.ClipRect(new SKRect(subWidth, 0, width, subHeight));
            canvas.Translate(subWidth, 0);
            DrawDensityHeatmapQuadrant(canvas, subWidth, subHeight, "side", verticalScale);
            canvas.Restore();
            
            // Bottom-left: Arm view
            canvas.Save();
            canvas.ClipRect(new SKRect(0, subHeight, subWidth, height));
            canvas.Translate(0, subHeight);
            DrawDensityHeatmapQuadrant(canvas, subWidth, subHeight, "arms");
            canvas.Restore();
            
            // Bottom-right: Rogue planet density
            canvas.Save();
            canvas.ClipRect(new SKRect(subWidth, subHeight, width, height));
            canvas.Translate(subWidth, subHeight);
            DrawDensityHeatmapQuadrant(canvas, subWidth, subHeight, "rogue");
            canvas.Restore();
            
            // Title
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.White;
                paint.TextSize = 60;
                paint.IsAntialias = true;
                paint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
                
                string title = "Milky Way Density Maps - Stars & Rogue Planets";
                var bounds = new SKRect();
                paint.MeasureText(title, ref bounds);
                canvas.DrawText(title, (width - bounds.Width) / 2, 60, paint);
            }
            
            SaveImage(surface, "MilkyWay_DensityHeatmap_Composite.png");
        }
    }
    
    private void DrawDensityHeatmapQuadrant(SKCanvas canvas, int width, int height, string viewType, float verticalScale = 1.0f)
    {
        var scale = viewType == "side" ? 120000.0f / width : 120000.0f / Math.Min(width, height);
        // At scale 1.0, show same range as X for realistic proportions
        var zRange = viewType == "side" ? 120000.0f / verticalScale : 120000.0f;
        var scaleZ = zRange / height;
        
        using (var paint = new SKPaint())
        {
            for (int y = 0; y < height; y += 2)
            {
                for (int x = 0; x < width; x += 2)
                {
                    float density = 0;
                    SKColor color;
                    
                    if (viewType == "top")
                    {
                        var galX = (x - width / 2f) * scale;
                        var galY = (height / 2f - y) * scale;
                        var pos = new GalaxyGenerator.Vector3(galX, galY, 0);
                        density = GalaxyGenerator.CalculateTotalDensity(pos);
                        
                        var logDensity = (float)(Math.Log10(density + 0.001) + 3) / 3f;
                        logDensity = Math.Max(0, Math.Min(1, logDensity));
                        color = GetHeatmapColor(logDensity);
                    }
                    else if (viewType == "side")
                    {
                        var galX = (x - width / 2f) * scale;
                        var galZ = (height / 2f - y) * scaleZ;
                        var pos = new GalaxyGenerator.Vector3(galX, 0, galZ);
                        density = GalaxyGenerator.CalculateTotalDensity(pos);
                        
                        var logDensity = (float)(Math.Log10(density + 0.001) + 3) / 3f;
                        logDensity = Math.Max(0, Math.Min(1, logDensity));
                        color = GetHeatmapColor(logDensity);
                    }
                    else if (viewType == "arms")
                    {
                        var galX = (x - width / 2f) * scale;
                        var galY = (height / 2f - y) * scale;
                        var pos = new GalaxyGenerator.Vector3(galX, galY, 0);
                        
                        var spiralDensity = GalaxyGenerator.CalculateSpiralArmMultiplier(pos);
                        var enhancement = (spiralDensity - 1.0f) / 2.0f;
                        enhancement = Math.Max(0, Math.Min(1, enhancement));
                        color = GetArmEnhancementColor(enhancement);
                    }
                    else if (viewType == "rogue")
                    {
                        var galX = (x - width / 2f) * scale;
                        var galY = (height / 2f - y) * scale;
                        var pos = new GalaxyGenerator.Vector3(galX, galY, 0);
                        
                        // Get rogue planet density
                        var rogueDensity = GalaxyGenerator.CalculateRoguePlanetDensity(pos);
                        
                        // Apply logarithmic scaling similar to stars
                        // Since rogues are ~1/10000 of stars, add offset to make visible
                        var logDensity = (float)(Math.Log10(rogueDensity + 0.0000001) + 7) / 7f;
                        logDensity = Math.Max(0, Math.Min(1, logDensity));
                        
                        // Use purple color scheme for rogues
                        color = GetRoguePlanetColor(logDensity);
                    }
                    else
                    {
                        color = SKColors.Black;
                    }
                    
                    paint.Color = color;
                    canvas.DrawRect(x, y, 2, 2, paint);
                }
            }
            
            // Add labels
            paint.Color = SKColors.White;
            paint.TextSize = 20;
            string label = viewType == "top" ? "Top View" : viewType == "side" ? "Side View" : "Spiral Arms";
            canvas.DrawText(label, 10, 30, paint);
            
            // Add distance rulers for side and front views
            if (viewType == "side" || viewType == "front")
            {
                DrawDistanceRulers(canvas, width, height, scale, scaleZ, viewType, verticalScale);
            }
        }
    }
    
    private void DrawRawDensityFormula(SKCanvas canvas, int width, int height)
    {
        var scale = 120000.0f / Math.Min(width, height);
        
        using (var paint = new SKPaint())
        {
            // Draw the raw density field
            for (int y = 0; y < height; y += 2) // Skip every other pixel for performance
            {
                for (int x = 0; x < width; x += 2)
                {
                    var galX = (x - width / 2f) * scale;
                    var galY = (height / 2f - y) * scale;
                    
                    var pos = new GalaxyGenerator.Vector3(galX, galY, 0);
                    var density = GalaxyGenerator.CalculateTotalDensity(pos);
                    
                    // Apply logarithmic scaling
                    var logDensity = (float)(Math.Log10(density + 0.001) + 3) / 3f;
                    logDensity = Math.Max(0, Math.Min(1, logDensity));
                    
                    var color = GetHeatmapColor(logDensity);
                    paint.Color = color;
                    canvas.DrawRect(x, y, 2, 2, paint);
                }
            }
            
            paint.Color = SKColors.White;
            paint.TextSize = 20;
            canvas.DrawText("Total Density Field", 10, 30, paint);
        }
    }
    
    private void DrawDensityComponents(SKCanvas canvas, int width, int height)
    {
        using (var paint = new SKPaint())
        {
            paint.TextSize = 20;
            paint.Color = SKColors.White;
            canvas.DrawText("Density Components", 10, 30, paint);
            
            // Show individual components
            var components = new[] { "Bulge", "Bar", "Disk", "Halo", "Spiral" };
            var componentHeight = (height - 50) / components.Length;
            
            for (int c = 0; c < components.Length; c++)
            {
                var y = 50 + c * componentHeight;
                paint.TextSize = 16;
                canvas.DrawText(components[c], 10, y + 20, paint);
                
                // Draw density gradient for this component
                for (int x = 60; x < width - 10; x++)
                {
                    var r = (x - 60) * 60000.0f / (width - 70);
                    float componentDensity = 0;
                    
                    var pos = new GalaxyGenerator.Vector3(r, 0, 0);
                    switch (c)
                    {
                        case 0: componentDensity = r < 5000 ? GalaxyGenerator.CalculateDiskDensity(r, 0) : 0; break; // Bulge region
                        case 1: componentDensity = 0; break; // No separate bar
                        case 2: componentDensity = GalaxyGenerator.CalculateDiskDensity(r, 0); break;
                        case 3: componentDensity = GalaxyGenerator.CalculateHaloDensity(r); break;
                        case 4: componentDensity = GalaxyGenerator.CalculateSpiralArmMultiplier(pos) - 1; break;
                    }
                    
                    // Apply logarithmic scaling for components
                    var logDensity = (float)(Math.Log10(componentDensity + 0.001) + 3) / 3f;
                    logDensity = Math.Max(0, Math.Min(1, logDensity));
                    var color = GetHeatmapColor(logDensity);
                    paint.Color = color;
                    canvas.DrawLine(x, y + 30, x, y + componentHeight - 10, paint);
                }
            }
        }
    }
    
    private void DrawSpiralArmStructure(SKCanvas canvas, int width, int height)
    {
        var scale = 100000.0f / Math.Min(width, height);
        
        using (var paint = new SKPaint())
        {
            // Draw spiral arm density only
            for (int y = 0; y < height; y += 2)
            {
                for (int x = 0; x < width; x += 2)
                {
                    var galX = (x - width / 2f) * scale;
                    var galY = (height / 2f - y) * scale;
                    
                    var pos = new GalaxyGenerator.Vector3(galX, galY, 0);
                    var spiralDensity = GalaxyGenerator.CalculateSpiralArmMultiplier(pos);
                    
                    // Show only the spiral enhancement with log scaling
                    var enhancement = (spiralDensity - 1); // Spiral density ranges from 1 to ~2.5
                    var logEnhancement = (float)(Math.Log10(enhancement + 0.1) + 1) / 1.5f; // Log scale [0.1, 1.5] to [0, 1]
                    logEnhancement = Math.Max(0, Math.Min(1, logEnhancement));
                    var color = GetHeatmapColor(logEnhancement);
                    paint.Color = color;
                    canvas.DrawRect(x, y, 2, 2, paint);
                }
            }
            
            paint.Color = SKColors.White;
            paint.TextSize = 20;
            canvas.DrawText("Spiral Arm Enhancement", 10, 30, paint);
        }
    }
    
    private void DrawCrossSectionProfiles(SKCanvas canvas, int width, int height)
    {
        using (var paint = new SKPaint())
        {
            paint.Color = SKColors.White;
            paint.TextSize = 20;
            canvas.DrawText("Density Cross-Sections", 10, 30, paint);
            
            // Draw radial profile
            paint.StrokeWidth = 2;
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = SKColors.Cyan;
            
            var path = new SKPath();
            for (int x = 10; x < width - 10; x++)
            {
                var r = x * 60000.0f / width;
                var pos = new GalaxyGenerator.Vector3(r, 0, 0);
                var density = GalaxyGenerator.CalculateTotalDensity(pos);
                
                // Apply logarithmic scaling for better visibility
                var logDensity = (float)(Math.Log10(density + 0.001) + 3) / 3f;
                logDensity = Math.Max(0, Math.Min(1, logDensity));
                
                var y = height / 2 - logDensity * (height / 2 - 40);
                if (x == 10)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }
            canvas.DrawPath(path, paint);
            
            // Draw vertical profile at solar radius
            paint.Color = SKColors.Yellow;
            path = new SKPath();
            for (int y = height / 2; y < height - 10; y++)
            {
                var z = (y - height / 2) * 5000.0f / (height / 2);
                var pos = new GalaxyGenerator.Vector3(26000, 0, z);
                var density = GalaxyGenerator.CalculateTotalDensity(pos);
                
                // Apply logarithmic scaling
                var logDensity = (float)(Math.Log10(density + 0.001) + 3) / 3f;
                logDensity = Math.Max(0, Math.Min(1, logDensity));
                
                var x = 10 + logDensity * (width - 20);
                if (y == height / 2)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }
            canvas.DrawPath(path, paint);
            
            paint.Style = SKPaintStyle.Fill;
            paint.TextSize = 14;
            paint.Color = SKColors.Cyan;
            canvas.DrawText("Radial (Z=0)", 10, height / 2 - 10, paint);
            paint.Color = SKColors.Yellow;
            canvas.DrawText("Vertical (R=26kly)", 10, height - 20, paint);
        }
    }
    
    private SKColor GetHeatmapColor(float value)
    {
        // Clamp value to [0, 1]
        value = Math.Max(0, Math.Min(1, value));
        
        // Special handling for very low density - now 100x lower threshold
        // Since we use log scale with (Log10(density + 0.001) + 3) / 3, 
        // density of 0.0000015 maps to (Log10(0.0010015) + 3) / 3 = (-3.0 + 3) / 3 = 0.0
        // We'll use a threshold of 0.0002 which corresponds to density ~0.0000015
        if (value < 0.0002f)  // This catches densities below ~0.0000015
        {
            return SKColors.Black;
        }
        
        // Rescale remaining range to start from gray
        value = (value - 0.0002f) / 0.9998f;
        
        // Create a heat color map: gray -> purple -> blue -> cyan -> green -> yellow -> red -> white
        if (value < 0.05f)
        {
            // Gray to dark purple
            var t = value / 0.05f;
            var gray = 64;  // Start from dark gray
            return new SKColor((byte)(gray * (1 - t) + 64 * t), (byte)(gray * (1 - t)), (byte)(gray * (1 - t) + 128 * t));
        }
        else if (value < 0.1f)
        {
            // Dark purple to dark blue
            var t = (value - 0.05f) / 0.05f;
            return new SKColor((byte)(64 * (1 - t)), 0, (byte)(128));
        }
        else if (value < 0.25f)
        {
            // Dark blue to blue
            var t = (value - 0.1f) / 0.15f;
            return new SKColor(0, 0, (byte)(128 + t * 127));
        }
        else if (value < 0.4f)
        {
            // Blue to cyan
            var t = (value - 0.25f) / 0.15f;
            return new SKColor(0, (byte)(t * 255), 255);
        }
        else if (value < 0.55f)
        {
            // Cyan to green
            var t = (value - 0.4f) / 0.15f;
            return new SKColor(0, 255, (byte)((1 - t) * 255));
        }
        else if (value < 0.7f)
        {
            // Green to yellow
            var t = (value - 0.55f) / 0.15f;
            return new SKColor((byte)(t * 255), 255, 0);
        }
        else if (value < 0.85f)
        {
            // Yellow to red
            var t = (value - 0.7f) / 0.15f;
            return new SKColor(255, (byte)((1 - t) * 255), 0);
        }
        else
        {
            // Red to white
            var t = (value - 0.85f) / 0.15f;
            return new SKColor(255, (byte)(t * 255), (byte)(t * 255));
        }
    }
    
    private SKColor GetArmEnhancementColor(float value)
    {
        // Clamp value to [0, 1]
        value = Math.Max(0, Math.Min(1, value));
        
        // Special color map for spiral arms: dark purple -> blue -> cyan -> bright white
        if (value < 0.25f)
        {
            // Black to dark purple
            var t = value / 0.25f;
            return new SKColor((byte)(t * 64), 0, (byte)(t * 128));
        }
        else if (value < 0.5f)
        {
            // Dark purple to blue
            var t = (value - 0.25f) / 0.25f;
            return new SKColor((byte)(64 - t * 64), 0, (byte)(128 + t * 127));
        }
        else if (value < 0.75f)
        {
            // Blue to cyan
            var t = (value - 0.5f) / 0.25f;
            return new SKColor(0, (byte)(t * 255), 255);
        }
        else
        {
            // Cyan to white
            var t = (value - 0.75f) / 0.25f;
            return new SKColor((byte)(t * 255), 255, 255);
        }
    }
    
    private SKColor GetRoguePlanetColor(float value)
    {
        // Clamp value to [0, 1]
        value = Math.Max(0, Math.Min(1, value));
        
        // Purple/magenta color scheme for rogue planets
        if (value < 0.33f)
        {
            // Black to deep purple
            var t = value / 0.33f;
            return new SKColor((byte)(50 * t), 0, (byte)(100 * t));
        }
        else if (value < 0.66f)
        {
            // Deep purple to magenta
            var t = (value - 0.33f) / 0.33f;
            return new SKColor((byte)(50 + 155 * t), (byte)(50 * t), (byte)(100 + 55 * t));
        }
        else
        {
            // Magenta to bright pink
            var t = (value - 0.66f) / 0.34f;
            return new SKColor((byte)(205 + 50 * t), (byte)(50 + 100 * t), (byte)(155 + 100 * t));
        }
    }
    
    private void DrawHeatmapInfo(SKCanvas canvas, string title, int width, int height)
    {
        using (var paint = new SKPaint())
        {
            paint.Color = SKColors.White;
            paint.TextSize = 24;
            paint.IsAntialias = true;
            
            canvas.DrawText(title, 20, 40, paint);
            
            // Draw color scale legend
            var legendY = height - 60;
            var legendWidth = 300;
            var legendX = width - legendWidth - 20;
            
            paint.TextSize = 14;
            canvas.DrawText("Stars/ly³:", legendX, legendY - 5, paint);
            
            // Draw gradient
            for (int x = 0; x < legendWidth; x++)
            {
                var value = (float)x / legendWidth;
                paint.Color = GetHeatmapColor(value);
                canvas.DrawLine(legendX + x, legendY, legendX + x, legendY + 20, paint);
            }
            
            // Show estimated stars per cubic light year
            // Logarithmic scale from 10^-3 to 10^3 stars/ly³
            // This covers halo (0.0001) to galactic center (1000)
            paint.Color = SKColors.White;
            paint.TextSize = 12;
            canvas.DrawText("10⁻³", legendX - 20, legendY + 35, paint);
            canvas.DrawText("10⁻¹", legendX + legendWidth * 0.33f - 15, legendY + 35, paint);
            canvas.DrawText("10¹", legendX + legendWidth * 0.67f - 15, legendY + 35, paint);
            canvas.DrawText("10³", legendX + legendWidth - 15, legendY + 35, paint);
        }
    }
    
    /// <summary>
    /// Draw distance rulers on the heatmap views
    /// </summary>
    private void DrawDistanceRulers(SKCanvas canvas, int width, int height, float scaleX, float scaleZ, string viewType, float verticalScale)
    {
        using (var paint = new SKPaint())
        {
            paint.Color = SKColors.White;
            paint.StrokeWidth = 1;
            paint.IsAntialias = true;
            
            // Draw horizontal ruler (X-axis)
            float rulerY = height - 30;
            canvas.DrawLine(0, rulerY, width, rulerY, paint);
            
            // Calculate tick interval - aim for ticks every ~100 pixels
            float maxDistanceX = width * scaleX / 2;
            float tickIntervalX = 10000; // Start with 10,000 ly
            if (maxDistanceX > 50000) tickIntervalX = 20000;
            if (maxDistanceX < 20000) tickIntervalX = 5000;
            if (maxDistanceX < 10000) tickIntervalX = 2000;
            if (maxDistanceX < 5000) tickIntervalX = 1000;
            
            // Draw X-axis ticks and labels
            paint.TextSize = 12;
            for (float distance = -maxDistanceX; distance <= maxDistanceX; distance += tickIntervalX)
            {
                float x = width / 2f + distance / scaleX;
                if (x >= 0 && x <= width)
                {
                    canvas.DrawLine(x, rulerY - 5, x, rulerY + 5, paint);
                    
                    string label = $"{distance / 1000:0}k";
                    if (distance == 0) label = "0";
                    var textBounds = new SKRect();
                    paint.MeasureText(label, ref textBounds);
                    canvas.DrawText(label, x - textBounds.Width / 2, rulerY + 20, paint);
                }
            }
            
            // Draw vertical ruler (Z-axis for side view)
            float rulerX = 30;
            canvas.DrawLine(rulerX, 0, rulerX, height, paint);
            
            // Calculate tick interval for Z-axis
            float maxDistanceZ = height * scaleZ / 2;
            float tickIntervalZ = 1000; // Start with 1,000 ly for Z
            if (maxDistanceZ > 5000) tickIntervalZ = 2000;
            if (maxDistanceZ < 2000) tickIntervalZ = 500;
            if (maxDistanceZ < 1000) tickIntervalZ = 200;
            
            // Draw Z-axis ticks and labels
            for (float distance = -maxDistanceZ; distance <= maxDistanceZ; distance += tickIntervalZ)
            {
                float y = height / 2f - distance / scaleZ;
                if (y >= 0 && y <= height)
                {
                    canvas.DrawLine(rulerX - 5, y, rulerX + 5, y, paint);
                    
                    string label = $"{distance:0}";
                    if (Math.Abs(distance) >= 1000) label = $"{distance / 1000:0}k";
                    canvas.DrawText(label, rulerX + 10, y + 5, paint);
                }
            }
            
            // Add axis labels
            paint.TextSize = 14;
            paint.Color = SKColors.LightGray;
            
            // X-axis label
            canvas.DrawText("Distance (ly)", width / 2f - 40, height - 5, paint);
            
            // Z-axis label
            canvas.Save();
            canvas.RotateDegrees(-90, 15, height / 2f);
            canvas.DrawText("Height (ly)", 15 - 35, height / 2f + 5, paint);
            canvas.Restore();
        }
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