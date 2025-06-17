using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

/// <summary>
/// Pure mathematical density visualizer that uses GalaxyGenerator formulas
/// Creates beautiful heatmaps showing stellar and rogue planet density
/// </summary>
public class DensityVisualizer
{
    private readonly Random random = new Random();
    
    /// <summary>
    /// Generate pure formula-based density heatmaps without any star sampling
    /// </summary>
    public void GenerateDensityHeatmaps(int width, int height, float verticalScale = 5.0f)
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
            
            DrawHeatmapInfo(canvas, "Density Heatmap - Side View (Y=0) [Log Scale]", width, height);
            DrawDistanceRulers(canvas, width, height, scaleX, scaleZ, "side", verticalScale);
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
                        // Since rogues are ~1/100 of stars, add offset to make visible
                        var logDensity = (float)(Math.Log10(rogueDensity + 0.00001) + 5) / 5f;
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
            
            DrawRoguePlanetInfo(canvas, "Rogue Planet Density Map [Log Scale]", width, height);
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
                        
                        // Apply logarithmic scaling
                        var logDensity = (float)(Math.Log10(rogueDensity + 0.00001) + 5) / 5f;
                        logDensity = Math.Max(0, Math.Min(1, logDensity));
                        
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
            string label = viewType switch
            {
                "top" => "Top View",
                "side" => "Side View",
                "arms" => "Spiral Arms",
                "rogue" => "Rogue Planets",
                _ => ""
            };
            canvas.DrawText(label, 10, 30, paint);
            
            // Add distance rulers for side view
            if (viewType == "side")
            {
                DrawDistanceRulers(canvas, width, height, scale, scaleZ, viewType, verticalScale);
            }
        }
    }
    
    private SKColor GetHeatmapColor(float value)
    {
        // Clamp value to [0, 1]
        value = Math.Max(0, Math.Min(1, value));
        
        // Special handling for very low density - hard cutoff from black
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
    
    private void DrawRoguePlanetInfo(SKCanvas canvas, string title, int width, int height)
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
            canvas.DrawText("Rogues/ly³:", legendX, legendY - 5, paint);
            
            // Draw gradient
            for (int x = 0; x < legendWidth; x++)
            {
                var value = (float)x / legendWidth;
                paint.Color = GetRoguePlanetColor(value);
                canvas.DrawLine(legendX + x, legendY, legendX + x, legendY + 20, paint);
            }
            
            // Show estimated rogues per cubic light year (much lower than stars)
            paint.Color = SKColors.White;
            paint.TextSize = 12;
            canvas.DrawText("10⁻⁵", legendX - 20, legendY + 35, paint);
            canvas.DrawText("10⁻⁴", legendX + legendWidth * 0.33f - 15, legendY + 35, paint);
            canvas.DrawText("10⁻³", legendX + legendWidth * 0.67f - 15, legendY + 35, paint);
            canvas.DrawText("10⁻²", legendX + legendWidth - 15, legendY + 35, paint);
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
}