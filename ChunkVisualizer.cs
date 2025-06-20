using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

/// <summary>
/// Visualizes individual chunks as images from multiple viewpoints
/// </summary>
public class ChunkVisualizer
{
    private readonly ChunkBasedGalaxySystem galaxySystem;
    private readonly int imageSize;
    
    public ChunkVisualizer(int imageSize = 1024)
    {
        this.galaxySystem = new ChunkBasedGalaxySystem();
        this.imageSize = imageSize;
    }
    
    /// <summary>
    /// Generate visualization of a chunk from three views
    /// </summary>
    public void VisualizeChunk(string chunkId, string? outputPrefix = null, bool realStarsOnly = false)
    {
        Console.WriteLine($"\nVisualizing chunk {chunkId}...");
        if (realStarsOnly)
        {
            Console.WriteLine("Mode: REAL STARS ONLY");
        }
        
        // Parse chunk coordinate
        var chunk = new ChunkBasedGalaxySystem.ChunkCoordinate(chunkId);
        var bounds = chunk.GetBounds();
        
        // Get chunk info
        Console.WriteLine($"Chunk bounds:");
        Console.WriteLine($"  R: {bounds.rMin:F0} - {bounds.rMax:F0} ly");
        Console.WriteLine($"  Theta: {bounds.thetaMin * 180 / Math.PI:F1}° - {bounds.thetaMax * 180 / Math.PI:F1}°");
        Console.WriteLine($"  Z: {bounds.zMin:F0} - {bounds.zMax:F0} ly");
        
        // Generate stars
        var startTime = DateTime.Now;
        var stars = galaxySystem.GenerateChunkStars(chunkId);
        var elapsed = (DateTime.Now - startTime).TotalSeconds;
        
        Console.WriteLine($"Generated {stars.Count} stars in {elapsed:F2}s");
        
        if (realStarsOnly)
        {
            var realStarsCount = stars.Count(s => s.IsRealStar);
            Console.WriteLine($"Real stars: {realStarsCount} out of {stars.Count} total");
        }
        
        if (stars.Count == 0)
        {
            Console.WriteLine("No stars in this chunk!");
            return;
        }
        
        // Convert to Cartesian positions using helper class
        var positions = stars.Select(s => new StarPosition
        {
            Star = s,
            X = s.Position.X,
            Y = s.Position.Y,
            Z = s.Position.Z
        }).ToList();
        
        // Calculate chunk center in Cartesian
        var centerR = (bounds.rMin + bounds.rMax) / 2;
        var centerTheta = (bounds.thetaMin + bounds.thetaMax) / 2;
        var centerZ = (bounds.zMin + bounds.zMax) / 2;
        var centerX = centerR * Math.Cos(centerTheta);
        var centerY = centerR * Math.Sin(centerTheta);
        
        // Generate output prefix if not provided
        if (outputPrefix == null)
        {
            outputPrefix = $"chunk_{chunkId}";
        }
        
        // Add suffix for real stars only mode
        if (realStarsOnly)
        {
            outputPrefix += "_real_only";
        }
        
        // Generate three views
        GenerateTopView(positions, centerX, centerY, centerZ, $"{outputPrefix}_top.png", realStarsOnly);
        GenerateSideView(positions, centerX, centerY, centerZ, $"{outputPrefix}_side.png", realStarsOnly);
        GenerateFrontView(positions, centerX, centerY, centerZ, $"{outputPrefix}_front.png", realStarsOnly);
        
        // Generate composite view
        GenerateCompositeView(positions, centerX, centerY, centerZ, chunk, $"{outputPrefix}_composite.png", realStarsOnly);
        
        Console.WriteLine($"✓ Visualization complete!");
    }
    
    private void GenerateTopView(List<StarPosition> stars, double centerX, double centerY, double centerZ, string filename, bool realStarsOnly = false)
    {
        Console.WriteLine($"Generating top view (X-Y plane)...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(imageSize, imageSize)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            // Calculate bounds for this view
            var minX = stars.Min(s => s.X);
            var maxX = stars.Max(s => s.X);
            var minY = stars.Min(s => s.Y);
            var maxY = stars.Max(s => s.Y);
            
            // Add padding
            var rangeX = maxX - minX;
            var rangeY = maxY - minY;
            var range = Math.Max(rangeX, rangeY) * 1.2; // 20% padding
            
            var scale = imageSize / range;
            var offsetX = (minX + maxX) / 2;
            var offsetY = (minY + maxY) / 2;
            
            DrawStars(canvas, stars, s => s.X, s => s.Y, scale, offsetX, offsetY, realStarsOnly);
            DrawChunkBorder(canvas, "Top View (X-Y)", stars.Count);
            
            SaveImage(surface, filename);
        }
    }
    
    private void GenerateSideView(List<StarPosition> stars, double centerX, double centerY, double centerZ, string filename, bool realStarsOnly = false)
    {
        Console.WriteLine($"Generating side view (X-Z plane)...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(imageSize, imageSize)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            // Calculate bounds for this view
            var minX = stars.Min(s => s.X);
            var maxX = stars.Max(s => s.X);
            var minZ = stars.Min(s => s.Z);
            var maxZ = stars.Max(s => s.Z);
            
            // Add padding
            var rangeX = maxX - minX;
            var rangeZ = maxZ - minZ;
            var range = Math.Max(rangeX, rangeZ) * 1.2;
            
            var scale = imageSize / range;
            var offsetX = (minX + maxX) / 2;
            var offsetZ = (minZ + maxZ) / 2;
            
            DrawStars(canvas, stars, s => s.X, s => s.Z, scale, offsetX, offsetZ, realStarsOnly);
            DrawChunkBorder(canvas, "Side View (X-Z)", stars.Count);
            
            SaveImage(surface, filename);
        }
    }
    
    private void GenerateFrontView(List<StarPosition> stars, double centerX, double centerY, double centerZ, string filename, bool realStarsOnly = false)
    {
        Console.WriteLine($"Generating front view (Y-Z plane)...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(imageSize, imageSize)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            // Calculate bounds for this view
            var minY = stars.Min(s => s.Y);
            var maxY = stars.Max(s => s.Y);
            var minZ = stars.Min(s => s.Z);
            var maxZ = stars.Max(s => s.Z);
            
            // Add padding
            var rangeY = maxY - minY;
            var rangeZ = maxZ - minZ;
            var range = Math.Max(rangeY, rangeZ) * 1.2;
            
            var scale = imageSize / range;
            var offsetY = (minY + maxY) / 2;
            var offsetZ = (minZ + maxZ) / 2;
            
            DrawStars(canvas, stars, s => s.Y, s => s.Z, scale, offsetY, offsetZ, realStarsOnly);
            DrawChunkBorder(canvas, "Front View (Y-Z)", stars.Count);
            
            SaveImage(surface, filename);
        }
    }
    
    private void GenerateCompositeView(List<StarPosition> stars, double centerX, double centerY, double centerZ, 
        ChunkBasedGalaxySystem.ChunkCoordinate chunk, string filename, bool realStarsOnly = false)
    {
        Console.WriteLine($"Generating composite view...");
        
        using (var surface = SKSurface.Create(new SKImageInfo(imageSize * 2, imageSize * 2)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            var subSize = imageSize;
            
            // Top-left: Top view
            canvas.Save();
            canvas.ClipRect(new SKRect(0, 0, subSize, subSize));
            DrawSubView(canvas, stars, s => s.X, s => s.Y, 0, 0, subSize, "Top (X-Y)", realStarsOnly);
            canvas.Restore();
            
            // Top-right: Side view
            canvas.Save();
            canvas.ClipRect(new SKRect(subSize, 0, subSize * 2, subSize));
            canvas.Translate(subSize, 0);
            DrawSubView(canvas, stars, s => s.X, s => s.Z, 0, 0, subSize, "Side (X-Z)", realStarsOnly);
            canvas.Restore();
            
            // Bottom-left: Front view
            canvas.Save();
            canvas.ClipRect(new SKRect(0, subSize, subSize, subSize * 2));
            canvas.Translate(0, subSize);
            DrawSubView(canvas, stars, s => s.Y, s => s.Z, 0, 0, subSize, "Front (Y-Z)", realStarsOnly);
            canvas.Restore();
            
            // Bottom-right: Info panel
            canvas.Save();
            canvas.ClipRect(new SKRect(subSize, subSize, subSize * 2, subSize * 2));
            canvas.Translate(subSize, subSize);
            DrawInfoPanel(canvas, chunk, stars, subSize);
            canvas.Restore();
            
            // Main title
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.White;
                paint.TextSize = 48;
                paint.IsAntialias = true;
                paint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
                
                var title = $"Chunk {chunk} Visualization";
                var bounds = new SKRect();
                paint.MeasureText(title, ref bounds);
                canvas.DrawText(title, (imageSize * 2 - bounds.Width) / 2, 60, paint);
            }
            
            SaveImage(surface, filename);
        }
    }
    
   private void DrawStars(SKCanvas canvas, List<StarPosition> stars,
    Func<StarPosition, double> getX, Func<StarPosition, double> getY,
    double scale, double offsetX, double offsetY, bool realStarsOnly = false)
{
    using (var paint = new SKPaint())
    {
        paint.IsAntialias = true;
        paint.BlendMode   = SKBlendMode.Plus;

        foreach (var sp in stars)
        {
            var star = sp.Star;

            // 1) If requested, skip any procedural stars
            if (realStarsOnly && !star.IsRealStar)
                continue;

            // 2) Skip companion stars: only draw those where RawName == SystemName
            if (sp.RawName != star.SystemName)
                continue;

            // 3) Compute on-screen position
            var x = (float)((getX(sp) - offsetX) * scale + imageSize / 2);
            var y = (float)((getY(sp) - offsetY) * scale + imageSize / 2);
            if (x < 0 || x >= imageSize || y < 0 || y >= imageSize)
                continue;

            // 4) Draw the star dot
            const float size = 1.0f;
            var c = star.Color;
            paint.Color = new SKColor(
                (byte)(c.X * 255),
                (byte)(c.Y * 255),
                (byte)(c.Z * 255),
                255
            );
            canvas.DrawCircle(x, y, size, paint);
        }
    }
}


    
    private void DrawSubView(SKCanvas canvas, List<StarPosition> stars,
        Func<StarPosition, double> getX, Func<StarPosition, double> getY,
        float offsetX, float offsetY, int size, string label, bool realStarsOnly = false)
    {
        // Calculate bounds
        var minX = stars.Min(getX);
        var maxX = stars.Max(getX);
        var minY = stars.Min(getY);
        var maxY = stars.Max(getY);
        
        var rangeX = maxX - minX;
        var rangeY = maxY - minY;
        var range = Math.Max(rangeX, rangeY) * 1.2;
        
        var scale = size / range;
        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;
        
        // Draw stars
        using (var paint = new SKPaint())
        {
            paint.IsAntialias = true;
            paint.BlendMode = SKBlendMode.Plus;
            
            foreach (var star in stars)
            {
                // Skip non-real stars if realStarsOnly is true
                if (realStarsOnly && !star.Star.IsRealStar) continue;
                
                var x = (float)((getX(star) - centerX) * scale + size / 2);
                var y = (float)((getY(star) - centerY) * scale + size / 2);
                
                if (x < 0 || x >= size || y < 0 || y >= size) continue;
                
                // UNIFORM SIZE AND BRIGHTNESS FOR ALL STARS
                var starSize = 0.8f; // Same size for all stars in composite view
                
                var starColor = star.Star.Color;
                var alpha = (byte)(255); // Full brightness for all stars
                
                paint.Color = new SKColor(
                    (byte)(starColor.X * 255),
                    (byte)(starColor.Y * 255),
                    (byte)(starColor.Z * 255),
                    alpha
                );
                
                canvas.DrawCircle(x, y, starSize, paint);
            }
            
            // Draw label
            paint.Color = SKColors.White;
            paint.TextSize = 20;
            paint.BlendMode = SKBlendMode.SrcOver;
            canvas.DrawText(label, 10, 30, paint);
        }
    }
    
    private void DrawInfoPanel(SKCanvas canvas, ChunkBasedGalaxySystem.ChunkCoordinate chunk, 
        List<StarPosition> stars, int size)
    {
        using (var paint = new SKPaint())
        {
            paint.Color = SKColors.White;
            paint.TextSize = 24;
            paint.IsAntialias = true;
            
            var y = 40;
            var lineHeight = 30;
            
            canvas.DrawText("Chunk Information", 20, y, paint);
            y += lineHeight * 2;
            
            paint.TextSize = 18;
            var bounds = chunk.GetBounds();
            
            canvas.DrawText($"Coordinates: {chunk}", 20, y, paint);
            y += lineHeight;
            
            canvas.DrawText($"Radial: {bounds.rMin:F0} - {bounds.rMax:F0} ly", 20, y, paint);
            y += lineHeight;
            
            canvas.DrawText($"Angular: {bounds.thetaMin * 180 / Math.PI:F1}° - {bounds.thetaMax * 180 / Math.PI:F1}°", 20, y, paint);
            y += lineHeight;
            
            canvas.DrawText($"Vertical: {bounds.zMin:F0} - {bounds.zMax:F0} ly", 20, y, paint);
            y += lineHeight;
            
            canvas.DrawText($"Total Stars: {stars.Count:N0}", 20, y, paint);
            y += lineHeight * 2;
            
            // Star type breakdown
            var typeGroups = stars.GroupBy(s => s.Star.Type)
                .OrderByDescending(g => g.Count())
                .Take(10);
            
            canvas.DrawText("Star Types:", 20, y, paint);
            y += lineHeight;
            
            paint.TextSize = 16;
            foreach (var group in typeGroups)
            {
                var percentage = group.Count() * 100.0 / stars.Count;
                canvas.DrawText($"  {group.Key}: {group.Count()} ({percentage:F1}%)", 20, y, paint);
                y += (int)(lineHeight * 0.8f);
            }
            
            // Color legend
            y = size - 200;
            paint.TextSize = 18;
            canvas.DrawText("Star Colors:", 20, y, paint);
            y += lineHeight;
            
            var colorExamples = new[]
            {
                ("O/B", new SKColor(155, 176, 255)),
                ("A", new SKColor(202, 215, 255)),
                ("F", new SKColor(248, 247, 255)),
                ("G", new SKColor(255, 244, 234)),
                ("K", new SKColor(255, 210, 161)),
                ("M", new SKColor(255, 155, 130))
            };
            
            paint.TextSize = 16;
            foreach (var (type, color) in colorExamples)
            {
                paint.Color = color;
                canvas.DrawCircle(30, y - 5, 6, paint);
                paint.Color = SKColors.White;
                canvas.DrawText(type, 50, y, paint);
                y += (int)(lineHeight * 0.8f);
            }
        }
    }
    
    private void DrawChunkBorder(SKCanvas canvas, string viewName, int starCount)
    {
        using (var paint = new SKPaint())
        {
            // Draw border
            paint.Color = SKColors.Gray;
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 2;
            canvas.DrawRect(1, 1, imageSize - 2, imageSize - 2, paint);
            
            // Draw title
            paint.Style = SKPaintStyle.Fill;
            paint.Color = SKColors.White;
            paint.TextSize = 24;
            paint.IsAntialias = true;
            
            canvas.DrawText(viewName, 20, 40, paint);
            
            paint.TextSize = 18;
            canvas.DrawText($"{starCount:N0} stars", 20, 65, paint);
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
        
        Console.WriteLine($"  ✓ Saved {filename}");
    }
    
    // Helper class to avoid dynamic type issues
    private class StarPosition
    {
        public Star Star { get; set; } = null!;

        public string RawName { get; set; } = null!;
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}