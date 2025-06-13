using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

/// <summary>
/// Cross-platform galaxy visualizer using SkiaSharp
/// Generates images of the galaxy from different viewpoints
/// </summary>
public class ScientificGalaxyVisualizer
{
    private readonly ScientificMilkyWayGenerator generator;
    private readonly List<ScientificMilkyWayGenerator.Star> stars;
    
    public ScientificGalaxyVisualizer(ScientificMilkyWayGenerator generator, int starCount = 100000)
    {
        this.generator = generator;
        Console.WriteLine($"Generating {starCount:N0} stars for visualization...");
        this.stars = generator.GenerateStars(starCount);
        Console.WriteLine("Stars generated!");
    }
    
    /// <summary>
    /// Generate all three views of the galaxy
    /// </summary>
    public void GenerateAllViews(int width = 2048, int height = 2048)
    {
        GenerateTopView(width, height);
        GenerateSideView(width, height);
        GenerateAngledView(width, height);
        GenerateCompositeView(width * 2, height * 2);
    }
    
    /// <summary>
    /// Generate top-down view (X-Y plane)
    /// </summary>
    public void GenerateTopView(int width = 2048, int height = 2048)
    {
        Console.WriteLine("Generating top view...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            // Draw galaxy with proper layering
            DrawGalaxyLayer(canvas, width, height, ViewProjection.Top, 0.3f); // Background stars
            DrawSpiralArms(canvas, width, height, ViewProjection.Top);
            DrawGalaxyLayer(canvas, width, height, ViewProjection.Top, 1.0f); // Foreground stars
            DrawGalaxyInfo(canvas, "Top View (X-Y Plane)", width, height);
            
            SaveImage(surface, "MilkyWay_TopView.png");
        }
    }
    
    /// <summary>
    /// Generate edge-on view (X-Z plane)
    /// </summary>
    public void GenerateSideView(int width = 2048, int height = 2048)
    {
        Console.WriteLine("Generating side view...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            DrawGalaxyLayer(canvas, width, height, ViewProjection.Side, 1.0f);
            DrawGalaxyInfo(canvas, "Side View (X-Z Plane)", width, height);
            
            SaveImage(surface, "MilkyWay_SideView.png");
        }
    }
    
    /// <summary>
    /// Generate angled 3D view
    /// </summary>
    public void GenerateAngledView(int width = 2048, int height = 2048)
    {
        Console.WriteLine("Generating angled 3D view...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            DrawGalaxyLayer(canvas, width, height, ViewProjection.Angled3D, 0.5f);
            DrawSpiralArms(canvas, width, height, ViewProjection.Angled3D);
            DrawGalaxyLayer(canvas, width, height, ViewProjection.Angled3D, 1.0f);
            DrawGalaxyInfo(canvas, "3D View (45° angle)", width, height);
            
            SaveImage(surface, "MilkyWay_3DView.png");
        }
    }
    
    /// <summary>
    /// Generate composite view with all three perspectives
    /// </summary>
    public void GenerateCompositeView(int width = 4096, int height = 4096)
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
            DrawGalaxyLayer(canvas, subWidth, subHeight, ViewProjection.Top, 0.3f);
            DrawSpiralArms(canvas, subWidth, subHeight, ViewProjection.Top);
            DrawGalaxyLayer(canvas, subWidth, subHeight, ViewProjection.Top, 1.0f);
            DrawGalaxyInfo(canvas, "Top View", subWidth, subHeight);
            canvas.Restore();
            
            // Top-right: Side view
            canvas.Save();
            canvas.ClipRect(new SKRect(subWidth, 0, width, subHeight));
            canvas.Translate(subWidth, 0);
            DrawGalaxyLayer(canvas, subWidth, subHeight, ViewProjection.Side, 1.0f);
            DrawGalaxyInfo(canvas, "Side View", subWidth, subHeight);
            canvas.Restore();
            
            // Bottom-left: Angled view
            canvas.Save();
            canvas.ClipRect(new SKRect(0, subHeight, subWidth, height));
            canvas.Translate(0, subHeight);
            DrawGalaxyLayer(canvas, subWidth, subHeight, ViewProjection.Angled3D, 0.5f);
            DrawSpiralArms(canvas, subWidth, subHeight, ViewProjection.Angled3D);
            DrawGalaxyLayer(canvas, subWidth, subHeight, ViewProjection.Angled3D, 1.0f);
            DrawGalaxyInfo(canvas, "3D View", subWidth, subHeight);
            canvas.Restore();
            
            // Bottom-right: Statistics
            canvas.Save();
            canvas.ClipRect(new SKRect(subWidth, subHeight, width, height));
            canvas.Translate(subWidth, subHeight);
            DrawStatistics(canvas, subWidth, subHeight);
            canvas.Restore();
            
            // Draw title
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.White;
                paint.TextSize = 60;
                paint.IsAntialias = true;
                paint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
                
                string title = "Scientific Milky Way Galaxy Model";
                var bounds = new SKRect();
                paint.MeasureText(title, ref bounds);
                canvas.DrawText(title, (width - bounds.Width) / 2, 60, paint);
            }
            
            SaveImage(surface, "MilkyWay_Composite.png");
        }
    }
    
    private void DrawGalaxyLayer(SKCanvas canvas, int width, int height, ViewProjection projection, float brightness)
    {
        var scale = Math.Min(width, height) / 120000.0f; // Scale to fit 60,000 ly radius
        var centerX = width / 2f;
        var centerY = height / 2f;
        
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
                
                // Size based on luminosity and distance
                var size = (float)Math.Sqrt(star.Luminosity) * 0.5f;
                if (projection == ViewProjection.Angled3D)
                {
                    size *= (1f - projected.Z / 100000f); // Depth scaling
                }
                
                // Color from star
                var alpha = (byte)(brightness * 255 * Math.Min(1, star.Luminosity / 10));
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
    
    private void DrawSpiralArms(SKCanvas canvas, int width, int height, ViewProjection projection)
    {
        var scale = Math.Min(width, height) / 120000.0f;
        var centerX = width / 2f;
        var centerY = height / 2f;
        
        using (var paint = new SKPaint())
        {
            paint.IsAntialias = true;
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 20;
            paint.Color = new SKColor(100, 150, 255, 50);
            paint.BlendMode = SKBlendMode.Plus;
            
            // Draw major spiral arms
            DrawSpiralArm(canvas, centerX, centerY, scale, projection, 0, 2.5f, paint); // Perseus
            DrawSpiralArm(canvas, centerX, centerY, scale, projection, Math.PI, 2.5f, paint); // Scutum-Centaurus
            
            paint.StrokeWidth = 15;
            paint.Color = new SKColor(150, 180, 255, 40);
            DrawSpiralArm(canvas, centerX, centerY, scale, projection, Math.PI/2, 2.2f, paint); // Sagittarius
            DrawSpiralArm(canvas, centerX, centerY, scale, projection, 3*Math.PI/2, 2.2f, paint); // Norma
            
            // Local arm
            paint.StrokeWidth = 10;
            paint.Color = new SKColor(255, 255, 150, 60);
            DrawSpiralArm(canvas, centerX, centerY, scale, projection, 0.7, 1.8f, paint); // Local/Orion Spur
        }
    }
    
    private void DrawSpiralArm(SKCanvas canvas, float centerX, float centerY, float scale, 
        ViewProjection projection, double startAngle, float windingTightness, SKPaint paint)
    {
        var path = new SKPath();
        bool first = true;
        
        for (double t = 0; t < 4 * Math.PI; t += 0.1)
        {
            var r = 5000 * Math.Exp(t / windingTightness);
            if (r > 50000) break;
            
            var angle = startAngle + t;
            var x = (float)(r * Math.Cos(angle));
            var y = (float)(r * Math.Sin(angle));
            var z = 0f;
            
            var pos = new ScientificMilkyWayGenerator.Vector3(x, y, z);
            var projected = ProjectPoint(pos, projection);
            
            var screenX = centerX + projected.X * scale;
            var screenY = centerY - projected.Y * scale;
            
            if (first)
            {
                path.MoveTo(screenX, screenY);
                first = false;
            }
            else
            {
                path.LineTo(screenX, screenY);
            }
        }
        
        canvas.DrawPath(path, paint);
    }
    
    private void DrawGalaxyInfo(SKCanvas canvas, string viewName, int width, int height)
    {
        using (var paint = new SKPaint())
        {
            paint.Color = SKColors.White;
            paint.TextSize = 24;
            paint.IsAntialias = true;
            
            canvas.DrawText(viewName, 20, 40, paint);
            
            paint.TextSize = 16;
            paint.Color = new SKColor(200, 200, 200);
            canvas.DrawText($"Stars: {stars.Count:N0}", 20, 65, paint);
            canvas.DrawText("Scale: 1 pixel ≈ 60 light-years", 20, 85, paint);
            
            // Draw scale bar
            paint.StrokeWidth = 2;
            paint.Style = SKPaintStyle.Stroke;
            canvas.DrawLine(20, height - 40, 220, height - 40, paint);
            paint.Style = SKPaintStyle.Fill;
            canvas.DrawText("12,000 light-years", 60, height - 45, paint);
        }
    }
    
    private void DrawStatistics(SKCanvas canvas, int width, int height)
    {
        using (var paint = new SKPaint())
        {
            paint.Color = SKColors.White;
            paint.TextSize = 28;
            paint.IsAntialias = true;
            paint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
            
            canvas.DrawText("Galaxy Statistics", 40, 60, paint);
            
            paint.TextSize = 18;
            paint.Typeface = SKTypeface.FromFamilyName("Arial");
            
            var y = 120f;
            var lineHeight = 30f;
            
            // Star types
            paint.Color = new SKColor(150, 200, 255);
            canvas.DrawText("Stellar Population:", 40, y, paint);
            y += lineHeight;
            
            paint.Color = new SKColor(200, 200, 200);
            var typeGroups = stars.GroupBy(s => s.Type).OrderByDescending(g => g.Count()).Take(5);
            foreach (var group in typeGroups)
            {
                canvas.DrawText($"  {group.Key}: {group.Count():N0} ({group.Count() * 100.0 / stars.Count:F1}%)", 
                    60, y, paint);
                y += lineHeight;
            }
            
            y += 20;
            
            // Regions
            paint.Color = new SKColor(150, 200, 255);
            canvas.DrawText("Galactic Regions:", 40, y, paint);
            y += lineHeight;
            
            paint.Color = new SKColor(200, 200, 200);
            var regionGroups = stars.GroupBy(s => s.Region).OrderByDescending(g => g.Count()).Take(5);
            foreach (var group in regionGroups)
            {
                canvas.DrawText($"  {group.Key}: {group.Count():N0} ({group.Count() * 100.0 / stars.Count:F1}%)", 
                    60, y, paint);
                y += lineHeight;
            }
            
            y += 20;
            
            // Model parameters
            paint.Color = new SKColor(150, 200, 255);
            canvas.DrawText("Model Parameters:", 40, y, paint);
            y += lineHeight;
            
            paint.Color = new SKColor(200, 200, 200);
            canvas.DrawText("  Total stars: 100-400 billion", 60, y, paint);
            y += lineHeight;
            canvas.DrawText("  Disk radius: 50,000 ly", 60, y, paint);
            y += lineHeight;
            canvas.DrawText("  Disk thickness: 1,000 ly", 60, y, paint);
            y += lineHeight;
            canvas.DrawText("  Central bar: 10,000 ly", 60, y, paint);
            y += lineHeight;
            canvas.DrawText("  Spiral arms: 4 major + Local Spur", 60, y, paint);
            y += lineHeight;
            
            y += 20;
            paint.Color = new SKColor(255, 200, 100);
            canvas.DrawText("Based on Gaia DR3 & latest research (2024)", 40, y, paint);
        }
    }
    
    private ScientificMilkyWayGenerator.Vector3 ProjectPoint(ScientificMilkyWayGenerator.Vector3 point, ViewProjection projection)
    {
        switch (projection)
        {
            case ViewProjection.Top:
                return new ScientificMilkyWayGenerator.Vector3(point.X, point.Y, 0);
                
            case ViewProjection.Side:
                return new ScientificMilkyWayGenerator.Vector3(point.X, point.Z * 10, 0); // Exaggerate Z for visibility
                
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