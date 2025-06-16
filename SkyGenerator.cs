using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

/// <summary>
/// Generates realistic sky views from any position in the galaxy
/// </summary>
public class SkyGenerator
{
    private readonly ChunkBasedGalaxySystem chunkSystem;
    private readonly ScientificMilkyWayGenerator generator;
    
    // Constants for rendering
    private const double NEARBY_CHUNK_DISTANCE = 1000; // Sample individual stars within 1000 ly
    private const double MIN_DENSITY_THRESHOLD = 0.00001; // Skip chunks with density below this
    private const double FIELD_OF_VIEW = 90.0; // 90 degree FOV
    private const double BRIGHT_STAR_MAG_LIMIT = 6.0; // Show individual stars brighter than mag 6
    private const double DIM_STAR_MAG_LIMIT = 10.0; // Show dimmer nearby stars up to mag 10
    
    public SkyGenerator(ChunkBasedGalaxySystem chunkSystem, ScientificMilkyWayGenerator generator)
    {
        this.chunkSystem = chunkSystem;
        this.generator = generator;
    }
    
    public void GenerateSkyView(double obsX, double obsY, double obsZ, int imageSize)
    {
        using (var surface = SKSurface.Create(new SKImageInfo(imageSize, imageSize)))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            
            // Calculate distance to galactic center
            double distanceToCenter = Math.Sqrt(obsX * obsX + obsY * obsY + obsZ * obsZ);
            
            // Direction to galactic center (normalized)
            // Special case: if at galactic center, look towards +X direction
            double dirX, dirY, dirZ;
            if (distanceToCenter < 0.001) // Effectively at galactic center
            {
                dirX = 1;  // Look towards positive X
                dirY = 0;
                dirZ = 0;
            }
            else
            {
                dirX = -obsX / distanceToCenter;
                dirY = -obsY / distanceToCenter;
                dirZ = -obsZ / distanceToCenter;
            }
            
            // Create perpendicular vectors for the view plane
            // First, find a temporary up vector that's not parallel to our view direction
            double tempUpX = 0, tempUpY = 0, tempUpZ = 1;
            if (Math.Abs(dirZ) > 0.9) // If looking nearly straight up/down, use X axis as temp up
            {
                tempUpX = 1;
                tempUpY = 0;
                tempUpZ = 0;
            }
            
            // Calculate right vector (cross product of direction and temp up)
            double rightX = dirY * tempUpZ - dirZ * tempUpY;
            double rightY = dirZ * tempUpX - dirX * tempUpZ;
            double rightZ = dirX * tempUpY - dirY * tempUpX;
            
            // Normalize right vector
            double rightLength = Math.Sqrt(rightX * rightX + rightY * rightY + rightZ * rightZ);
            if (rightLength > 1e-10)
            {
                rightX /= rightLength;
                rightY /= rightLength;
                rightZ /= rightLength;
            }
            else
            {
                // Fallback if cross product failed (view direction parallel to temp up)
                rightX = 1;
                rightY = 0;
                rightZ = 0;
            }
            
            // Calculate actual up vector (cross product of right and direction)
            double upX = rightY * dirZ - rightZ * dirY;
            double upY = rightZ * dirX - rightX * dirZ;
            double upZ = rightX * dirY - rightY * dirX;
            
            // Normalize up vector
            double upLength = Math.Sqrt(upX * upX + upY * upY + upZ * upZ);
            if (upLength > 1e-10)
            {
                upX /= upLength;
                upY /= upLength;
                upZ /= upLength;
            }
            else
            {
                // Fallback if cross product failed
                upX = 0;
                upY = 0;
                upZ = 1;
            }
            
            // New approach: Generate point stars based on chunks
            RenderGalaxyAsPoints(canvas, imageSize, obsX, obsY, obsZ, 
                dirX, dirY, dirZ, rightX, rightY, rightZ, upX, upY, upZ);
            
            // Render nearby individual stars
            RenderNearbyStars(canvas, imageSize, obsX, obsY, obsZ, 
                dirX, dirY, dirZ, rightX, rightY, rightZ, upX, upY, upZ);
            
            // Save the image
            SaveImage(surface, obsX, obsY, obsZ);
        }
    }
    
    private void RenderDistantChunks(SKCanvas canvas, int imageSize, 
        double obsX, double obsY, double obsZ,
        double dirX, double dirY, double dirZ, 
        double rightX, double rightY, double rightZ,
        double upX, double upY, double upZ)
    {
        double halfSize = imageSize / 2.0;
        double scale = Math.Tan(FIELD_OF_VIEW * Math.PI / 360.0); // Convert FOV to radians and get tan of half angle
        
        // Sample the sky in a grid pattern
        int samples = 200; // 200x200 sampling grid
        double[,] luminosityMap = new double[samples, samples];
        double[,] colorTempMap = new double[samples, samples];
        double maxLuminosity = 0;
        
        for (int sy = 0; sy < samples; sy++)
        {
            for (int sx = 0; sx < samples; sx++)
            {
                // Convert screen coordinates to ray direction
                // Add small offset to avoid exact center singularity
                double u = (sx + 0.5 - samples/2.0) / (samples/2.0) * scale;
                double v = (sy + 0.5 - samples/2.0) / (samples/2.0) * scale;
                
                // Ray direction in 3D (view direction + horizontal offset + vertical offset)
                double rayX = dirX + u * rightX + v * upX;
                double rayY = dirY + u * rightY + v * upY;
                double rayZ = dirZ + u * rightZ + v * upZ;
                
                // Normalize ray direction
                double rayLength = Math.Sqrt(rayX * rayX + rayY * rayY + rayZ * rayZ);
                if (rayLength < 1e-10) // Prevent division by zero
                {
                    // Skip this pixel if we have a degenerate ray
                    continue;
                }
                rayX /= rayLength;
                rayY /= rayLength;
                rayZ /= rayLength;
                
                // Accumulate light along this ray
                double totalLuminosity = 0;
                double weightedColorTemp = 0;
                double totalWeight = 0;
                
                // March along the ray in steps
                double stepSize = 100; // 100 ly steps
                double maxDistance = 100000; // Look up to 100k ly away
                
                // Start from a small offset to avoid numerical issues at observer position
                for (double dist = 0.1; dist < maxDistance; dist += stepSize)
                {
                    // Position along ray
                    double px = obsX + rayX * dist;
                    double py = obsY + rayY * dist;
                    double pz = obsZ + rayZ * dist;
                    
                    // Get density at this position
                    var pos = new GalaxyGenerator.Vector3((float)px, (float)py, (float)pz);
                    double stellarDensity = GalaxyGenerator.GetExpectedStarDensity(pos);
                    
                    if (stellarDensity < MIN_DENSITY_THRESHOLD) continue;
                    
                    // Calculate volume of space sampled at this distance
                    // The ray samples a cone that expands with distance
                    // At distance d, the cross-sectional area is proportional to (d * tan(fov/samples))²
                    double pixelAngle = FIELD_OF_VIEW * Math.PI / 180.0 / samples; // Angle per pixel in radians
                    double crossSection = dist * Math.Tan(pixelAngle);
                    crossSection = crossSection * crossSection; // Square for area
                    double chunkVolume = crossSection * stepSize; // Volume = area * length
                    
                    // Estimate number of stars in this volume
                    double starCount = stellarDensity * chunkVolume;
                    
                    // Determine stellar population for color
                    double r = Math.Sqrt(px * px + py * py);
                    double colorTemp = EstimateChunkColorTemperature(r, pz);
                    
                    // Light falls off with distance squared
                    double distanceFactor = 1.0 / (dist * dist);
                    double chunkLuminosity = starCount * distanceFactor;
                    
                    // Apply stronger tone mapping to prevent overflow in dense regions
                    // This is especially important near the galactic center
                    chunkLuminosity = chunkLuminosity / (1.0 + chunkLuminosity * 0.1);
                    
                    totalLuminosity += chunkLuminosity;
                    weightedColorTemp += colorTemp * chunkLuminosity;
                    totalWeight += chunkLuminosity;
                }
                
                luminosityMap[sx, sy] = totalLuminosity;
                colorTempMap[sx, sy] = totalWeight > 0 ? weightedColorTemp / totalWeight : 5000;
                maxLuminosity = Math.Max(maxLuminosity, totalLuminosity);
            }
        }
        
        // Render the accumulated light map
        using (var paint = new SKPaint())
        {
            paint.IsAntialias = true;
            
            for (int sy = 0; sy < samples; sy++)
            {
                for (int sx = 0; sx < samples; sx++)
                {
                    double luminosity = luminosityMap[sx, sy];
                    if (luminosity <= 0) continue;
                    
                    // Map to screen coordinates
                    float screenX = (float)(sx * imageSize / (double)samples);
                    float screenY = (float)(sy * imageSize / (double)samples);
                    float cellSize = (float)(imageSize / (double)samples);
                    
                    // Calculate brightness (logarithmic scale)
                    double brightness = Math.Log10(1 + luminosity * 1000) / Math.Log10(1 + maxLuminosity * 1000);
                    brightness = Math.Pow(brightness, 0.5); // Gamma correction
                    brightness = Math.Max(0, Math.Min(1, brightness)); // Clamp to [0,1]
                    
                    // Get color based on temperature
                    double colorTemp = colorTempMap[sx, sy];
                    var color = TemperatureToColor(colorTemp, brightness);
                    
                    paint.Color = color;
                    canvas.DrawRect(screenX, screenY, cellSize + 1, cellSize + 1, paint);
                }
            }
        }
    }
    
    private void RenderGalaxyAsPoints(SKCanvas canvas, int imageSize, 
        double obsX, double obsY, double obsZ,
        double dirX, double dirY, double dirZ, 
        double rightX, double rightY, double rightZ,
        double upX, double upY, double upZ)
    {
        double halfSize = imageSize / 2.0;
        double scale = Math.Tan(FIELD_OF_VIEW * Math.PI / 360.0);
        
        var stars = new List<(double x, double y, double brightness, SKColor color)>();
        Random rand = new Random(42); // Consistent randomness
        
        Console.WriteLine("\nGenerating sky view...");
        
        // Simple approach: sample rays through the view and place stars based on density
        int numRays = 100000; // Number of rays to cast for good coverage
        double maxDistance = 100000; // Maximum viewing distance
        
        for (int i = 0; i < numRays; i++)
        {
            if (i % 1000 == 0)
            {
                int percent = (int)((i / (double)numRays) * 100);
                Console.Write($"\rCasting rays: {percent}% complete");
            }
            
            // Random position on screen (within FOV)
            double u = (rand.NextDouble() - 0.5) * 2 * scale;
            double v = (rand.NextDouble() - 0.5) * 2 * scale;
            
            // Calculate ray direction
            double rayX = dirX + u * rightX + v * upX;
            double rayY = dirY + u * rightY + v * upY;
            double rayZ = dirZ + u * rightZ + v * upZ;
            
            // Normalize ray
            double rayLength = Math.Sqrt(rayX * rayX + rayY * rayY + rayZ * rayZ);
            if (rayLength < 1e-10) continue;
            rayX /= rayLength;
            rayY /= rayLength;
            rayZ /= rayLength;
            
            // March along the ray and accumulate stars based on density
            double stepSize = 50; // Step size in light years
            double currentDistance = 10; // Start 10 ly from observer
            
            while (currentDistance < maxDistance)
            {
                // Position along ray
                double px = obsX + rayX * currentDistance;
                double py = obsY + rayY * currentDistance;
                double pz = obsZ + rayZ * currentDistance;
                
                // Get density at this position
                var pos = new GalaxyGenerator.Vector3((float)px, (float)py, (float)pz);
                double density = GalaxyGenerator.GetExpectedStarDensity(pos);
                
                if (density > MIN_DENSITY_THRESHOLD)
                {
                    // Probability of a star in this segment
                    double segmentVolume = stepSize * (currentDistance * scale / imageSize) * (currentDistance * scale / imageSize);
                    double starProbability = density * segmentVolume * 0.1; // Scaling factor
                    
                    if (rand.NextDouble() < starProbability)
                    {
                        // Project this 3D position to screen
                        // We need to recalculate screen position for each star's actual 3D location
                        double dx = px - obsX;
                        double dy = py - obsY;
                        double dz = pz - obsZ;
                        
                        double dotProduct = dx * dirX + dy * dirY + dz * dirZ;
                        if (dotProduct <= 0) continue; // Behind observer
                        
                        double projX = (dx * rightX + dy * rightY + dz * rightZ) / dotProduct;
                        double projY = (dx * upX + dy * upY + dz * upZ) / dotProduct;
                        
                        double screenX = halfSize + projX * halfSize / scale;
                        double screenY = halfSize - projY * halfSize / scale;
                        
                        // Brightness based on distance AND density (logarithmic)
                        double distanceFalloff = 1.0 / (1 + currentDistance / 20000);
                        double densityBrightness = Math.Log10(1 + density * 10000) / 4.0; // Log scale
                        double brightness = distanceFalloff * densityBrightness;
                        brightness *= (0.7 + rand.NextDouble() * 0.3);
                        brightness = Math.Min(1.0, brightness);
                        
                        // Color based on density - denser = bluer/hotter
                        double r = Math.Sqrt(px * px + py * py);
                        double baseTemp = EstimateChunkColorTemperature(r, pz);
                        
                        // Adjust temperature based on density
                        // Low density: 4000-5000K (red-yellow)
                        // Medium density: 5000-6000K (yellow-white) 
                        // High density: 6000-10000K (white-blue)
                        double densityFactor = Math.Log10(1 + density * 1000) / 3.0;
                        densityFactor = Math.Min(1.0, densityFactor);
                        
                        double colorTemp = baseTemp;
                        if (density > 0.1) // Dense regions (center, arms)
                        {
                            colorTemp = 6000 + densityFactor * 4000; // 6000-10000K
                        }
                        else if (density > 0.001) // Medium density (disc)
                        {
                            colorTemp = 5000 + densityFactor * 1000; // 5000-6000K
                        }
                        else // Low density (halo)
                        {
                            colorTemp = 4000 + densityFactor * 1000; // 4000-5000K
                        }
                        
                        var color = TemperatureToColor(colorTemp, brightness);
                        
                        stars.Add((screenX, screenY, brightness, color));
                    }
                }
                
                // Adaptive step size - smaller steps in denser regions
                stepSize = Math.Max(20, Math.Min(500, 1000 / (1 + density)));
                currentDistance += stepSize;
            }
        }
        
        Console.WriteLine($"\rCasting rays: 100% complete - {stars.Count:N0} stars generated");
        
        // Sort by brightness and render
        Console.WriteLine($"Sorting {stars.Count:N0} stars by brightness...");
        stars.Sort((a, b) => a.brightness.CompareTo(b.brightness));
        
        Console.WriteLine("Rendering stars to image...");
        
        using (var paint = new SKPaint())
        {
            paint.IsAntialias = true;
            paint.StrokeWidth = 1;
            
            foreach (var star in stars)
            {
                paint.Color = star.color;
                canvas.DrawPoint((float)star.x, (float)star.y, paint);
            }
        }
        
        Console.WriteLine($"Rendered {stars.Count:N0} stars");
    }
    
    private void RenderNearbyStars(SKCanvas canvas, int imageSize,
        double obsX, double obsY, double obsZ,
        double dirX, double dirY, double dirZ,
        double rightX, double rightY, double rightZ,
        double upX, double upY, double upZ)
    {
        double halfSize = imageSize / 2.0;
        double scale = Math.Tan(FIELD_OF_VIEW * Math.PI / 360.0);
        
        // Get observer's chunk
        var obsChunk = chunkSystem.GetChunkForPosition((float)obsX, (float)obsY, (float)obsZ);
        
        // Check nearby chunks (within NEARBY_CHUNK_DISTANCE)
        int searchRadius = (int)(NEARBY_CHUNK_DISTANCE / 100) + 1; // Convert to chunk units
        
        var nearbyStars = new List<(ScientificMilkyWayGenerator.Star star, double distance, double screenX, double screenY, double apparentMag)>();
        
        // Search surrounding chunks
        for (int dr = -searchRadius; dr <= searchRadius; dr++)
        {
            for (int dtheta = -searchRadius; dtheta <= searchRadius; dtheta++)
            {
                for (int dz = -searchRadius; dz <= searchRadius; dz++)
                {
                    int chunkR = obsChunk.R + dr;
                    int chunkTheta = (obsChunk.Theta + dtheta + 360) % 360;
                    int chunkZ = obsChunk.Z + dz;
                    
                    // Skip invalid chunks
                    if (chunkR < 0 || chunkR >= 1500 || chunkZ < -100 || chunkZ > 100) continue;
                    
                    // Calculate chunk center position
                    double chunkCenterR = (chunkR + 0.5) * 100; // CHUNK_SIZE = 100
                    double chunkCenterTheta = (chunkTheta + 0.5) * Math.PI / 180.0;
                    double chunkCenterX = chunkCenterR * Math.Cos(chunkCenterTheta);
                    double chunkCenterY = chunkCenterR * Math.Sin(chunkCenterTheta);
                    double chunkCenterZ = (chunkZ + 0.5) * 100;
                    
                    double chunkDist = Math.Sqrt(
                        Math.Pow(chunkCenterX - obsX, 2) +
                        Math.Pow(chunkCenterY - obsY, 2) +
                        Math.Pow(chunkCenterZ - obsZ, 2));
                    
                    // Skip chunks too far away
                    if (chunkDist > NEARBY_CHUNK_DISTANCE + 150) continue; // 150 is diagonal of chunk
                    
                    // Check chunk density before generating stars
                    var chunkPos = new GalaxyGenerator.Vector3((float)chunkCenterX, (float)chunkCenterY, (float)chunkCenterZ);
                    double stellarDensity = GalaxyGenerator.GetExpectedStarDensity(chunkPos);
                    
                    // Skip very sparse chunks unless very close
                    if (stellarDensity < MIN_DENSITY_THRESHOLD && chunkDist > 200) continue;
                    
                    // Get stars in this chunk
                    string chunkId = $"{chunkR}_{chunkTheta}_{chunkZ}";
                    
                    // Generate stars for this chunk
                    var chunkStars = chunkSystem.GenerateChunkStars(chunkId);
                    
                    // Only process chunks with reasonable star counts
                    if (chunkStars.Count > 100000) continue; // Skip extremely dense chunks
                    
                    // Sample a subset of stars if chunk is dense
                    int sampleRate = chunkStars.Count > 10000 ? chunkStars.Count / 10000 : 1;
                    
                    for (int i = 0; i < chunkStars.Count; i += sampleRate)
                    {
                        var star = chunkStars[i];
                        // Star is a struct, so can't be null
                        
                        // Calculate distance to observer
                        double dx = star.Position.X - obsX;
                        double dy = star.Position.Y - obsY;
                        double starDz = star.Position.Z - obsZ;
                        double distance = Math.Sqrt(dx * dx + dy * dy + starDz * starDz);
                        
                        if (distance > NEARBY_CHUNK_DISTANCE || distance < 0.1) continue;
                        
                        // Calculate apparent magnitude from luminosity
                        double absoluteMag = -2.5 * Math.Log10(star.Luminosity) + 4.74; // Convert luminosity to absolute magnitude
                        double apparentMag = absoluteMag + 5 * Math.Log10(distance / 10.0);
                        
                        // Skip stars too dim to see
                        if (apparentMag > DIM_STAR_MAG_LIMIT) continue;
                        
                        // Project star position onto view plane
                        double dotProduct = dx * dirX + dy * dirY + starDz * dirZ;
                        if (dotProduct <= 0) continue; // Behind observer
                        
                        // Calculate screen position using proper 3D projection
                        double projX = (dx * rightX + dy * rightY + starDz * rightZ) / dotProduct;
                        double projY = (dx * upX + dy * upY + starDz * upZ) / dotProduct;
                        
                        // Check if within FOV
                        if (Math.Abs(projX) > scale || Math.Abs(projY) > scale) continue;
                        
                        double screenX = halfSize + projX * halfSize / scale;
                        double screenY = halfSize - projY * halfSize / scale;
                        
                        nearbyStars.Add((star, distance, screenX, screenY, apparentMag));
                    }
                }
            }
        }
        
        // Sort stars by apparent magnitude (brightest first)
        nearbyStars.Sort((a, b) => a.apparentMag.CompareTo(b.apparentMag));
        
        // Render stars
        using (var paint = new SKPaint())
        {
            paint.IsAntialias = true;
            
            foreach (var (star, distance, screenX, screenY, apparentMag) in nearbyStars)
            {
                // Calculate star size based on apparent magnitude
                float starSize = (float)(10 * Math.Pow(2.512, -apparentMag / 2.5));
                starSize = Math.Max(0.5f, Math.Min(20f, starSize));
                
                // Get star color
                var starColor = GetStarColor(star.Type.ToString());
                
                // Adjust brightness based on apparent magnitude
                float brightness = (float)Math.Pow(2.512, (DIM_STAR_MAG_LIMIT - apparentMag) / 5.0);
                brightness = Math.Min(1f, brightness);
                
                byte alpha = (byte)(255 * brightness);
                starColor = starColor.WithAlpha(alpha);
                
                paint.Color = starColor;
                paint.StrokeWidth = 1;
                
                // Draw all stars as single pixels
                canvas.DrawPoint((float)screenX, (float)screenY, paint);
            }
        }
    }
    
    private double EstimateChunkColorTemperature(double r, double z)
    {
        // Base color temperature based on galactic region
        double scaleHeight = Math.Abs(z) / 1000.0;
        
        if (r < 1000) // Galactic center - very hot/blue
        {
            return 7000; // Blue-white
        }
        else if (r < 3000) // Inner bulge
        {
            return 6000; // White with slight blue
        }
        else if (r < 20000 && scaleHeight < 0.5) // Thin disk
        {
            return 5000; // Yellow (like our Sun)
        }
        else if (scaleHeight < 2) // Thick disk
        {
            return 4500; // Yellow-orange
        }
        else // Halo
        {
            return 5500; // Old but metal-poor stars appear bluer
        }
    }
    
    private SKColor TemperatureToColor(double temperature, double brightness)
    {
        // Convert temperature to RGB using blackbody radiation approximation
        double temp = temperature / 100.0;
        double red, green, blue;
        
        // Red
        if (temp <= 66)
        {
            red = 255;
        }
        else
        {
            red = temp - 60;
            red = 329.698727446 * Math.Pow(red, -0.1332047592);
            red = Math.Max(0, Math.Min(255, red));
        }
        
        // Green
        if (temp <= 66)
        {
            green = temp;
            green = 99.4708025861 * Math.Log(green) - 161.1195681661;
        }
        else
        {
            green = temp - 60;
            green = 288.1221695283 * Math.Pow(green, -0.0755148492);
        }
        green = Math.Max(0, Math.Min(255, green));
        
        // Blue
        if (temp >= 66)
        {
            blue = 255;
        }
        else if (temp <= 19)
        {
            blue = 0;
        }
        else
        {
            blue = temp - 10;
            blue = 138.5177312231 * Math.Log(blue) - 305.0447927307;
            blue = Math.Max(0, Math.Min(255, blue));
        }
        
        // Apply brightness and clamp to valid range
        red *= brightness;
        green *= brightness;
        blue *= brightness;
        
        // Clamp values to 0-255 range to prevent overflow
        red = Math.Max(0, Math.Min(255, red));
        green = Math.Max(0, Math.Min(255, green));
        blue = Math.Max(0, Math.Min(255, blue));
        
        return new SKColor((byte)red, (byte)green, (byte)blue);
    }
    
    private SKColor GetStarColor(string stellarType)
    {
        // Map stellar types to colors
        if (stellarType.StartsWith("O")) return new SKColor(155, 176, 255);
        if (stellarType.StartsWith("B")) return new SKColor(170, 191, 255);
        if (stellarType.StartsWith("A")) return new SKColor(202, 215, 255);
        if (stellarType.StartsWith("F")) return new SKColor(248, 247, 255);
        if (stellarType.StartsWith("G")) return new SKColor(255, 244, 234);
        if (stellarType.StartsWith("K")) return new SKColor(255, 210, 161);
        if (stellarType.StartsWith("M")) return new SKColor(255, 155, 84);
        
        // Special objects
        if (stellarType == "DA") return new SKColor(200, 200, 255); // White dwarf
        if (stellarType == "NS") return new SKColor(150, 150, 255); // Neutron star
        if (stellarType == "BH") return new SKColor(50, 50, 50); // Black hole (dim)
        
        return new SKColor(255, 255, 255); // Default white
    }
    
    private void SaveImage(SKSurface surface, double obsX, double obsY, double obsZ)
    {
        using (var image = surface.Snapshot())
        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
        {
            string filename = $"GalaxySkyView_{obsX:F0}_{obsY:F0}_{obsZ:F0}.png";
            using (var stream = System.IO.File.OpenWrite(filename))
            {
                data.SaveTo(stream);
            }
            Console.WriteLine($"Saved: {filename}");
        }
    }
}