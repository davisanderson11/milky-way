namespace MilkyWay.Legacy
{
  public static class HeatmapConsole
  {
    public static void Run()
    {
        Console.WriteLine("\n=== Generate Density Heatmaps ===");
        Console.WriteLine("Create beautiful visualization of star and rogue planet density");
        Console.WriteLine("Using pure mathematical formulas from GalaxyGenerator");
        
        Console.Write("\nImage size (512-4096, default 2048): ");
        var sizeInput = Console.ReadLine();
        int imageSize = 2048;
        
        if (!string.IsNullOrWhiteSpace(sizeInput) && int.TryParse(sizeInput, out var size))
        {
            imageSize = Math.Max(512, Math.Min(4096, size));
        }
        
        Console.Write("\nVertical scale for side view (1.0-10.0, default 5.0): ");
        var scaleInput = Console.ReadLine();
        float verticalScale = 5.0f;
        
        if (!string.IsNullOrWhiteSpace(scaleInput) && float.TryParse(scaleInput, out var scale))
        {
            verticalScale = Math.Max(1.0f, Math.Min(10.0f, scale));
        }
        
        try
        {
            var visualizer = new DensityVisualizer();
            visualizer.GenerateDensityHeatmaps(imageSize, imageSize, verticalScale);
            visualizer.GenerateStellarDensityHeatmaps(imageSize, imageSize, verticalScale);
            
            Console.WriteLine("\n✓ Density heatmaps generated successfully!");
            Console.WriteLine("\nGenerated files:");
            Console.WriteLine("  - MilkyWay_DensityHeatmap_Top.png (top-down view)");
            Console.WriteLine("  - MilkyWay_DensityHeatmap_Side.png (side view)");
            Console.WriteLine("  - MilkyWay_DensityHeatmap_Arms.png (spiral arm enhancement)");
            Console.WriteLine("  - MilkyWay_DensityHeatmap_Rogues.png (rogue planet density)");
            Console.WriteLine("  - MilkyWay_DensityHeatmap_Composite.png (all views combined)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating heatmaps: {ex.Message}");
        }
    }
  }
}
