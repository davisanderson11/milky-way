# Milky Way Galaxy Simulation Project

## Overview
This is a scientifically accurate Milky Way galaxy generator that creates a simulation of ~225 billion unique stars using a unified generation system where position determines everything.

## Project Architecture

### Core Generation System - GalaxyGenerator.cs
The master algorithm that defines the galaxy's structure:
- **Unified Generation**: seed = f(x,y,z) - position determines star properties
- **Realistic Density**: Smooth transitions from galactic center to halo
- **Logarithmic Spiral Arms**: Proper pitch angles (15-20°) creating ~2 full rotations
- **Scale Heights**: Bulge transitions smoothly to thin disk
- **Extended Range**: Chunks out to 150,000 ly for galactic halo

### Key Components

1. **GalaxyGenerator.cs**: Master density and structure algorithms
   - CalculateTotalDensity(): Returns normalized density (0-1)
   - GetExpectedStarDensity(): Returns actual stars/ly³
   - CalculateSpiralArmMultiplier(): Logarithmic spiral arms
   - Smooth transitions between all regions

2. **ChunkBasedGalaxySystem.cs**: Spatial organization
   - 100×100×100 ly chunks in cylindrical coordinates
   - Extended to 1500 radius chunks (150,000 ly)
   - No star count limits - density determines count
   - Fast chunk investigation and CSV export

3. **ScientificMilkyWayGenerator.cs**: Star property generation
   - 24 stellar types with Harvard classification
   - Realistic mass, temperature, luminosity relationships
   - 6 stellar populations based on location
   - Companion systems and planetary generation

4. **ScientificGalaxyVisualizer2.cs**: Advanced visualization
   - Rejection sampling with volume compensation
   - Pure formula-based density heatmaps
   - Logarithmic scaling for wide density ranges
   - Adjustable vertical scale for side views
   - Updated color scheme (black only for density < 0.0000015)
   - Purple transition between grey and blue colors

5. **ChunkVisualizer.cs**: Individual chunk visualization
   - Generate images of any chunk showing actual star colors
   - Three views: top (X-Y), side (X-Z), front (Y-Z)
   - Composite view with all three plus statistics
   - Star colors based on temperature (blue O/B to red M stars)

### Supporting Systems
- **MultipleStarSystems.cs**: Binary/multiple star systems
- **PlanetarySystemGenerator.cs**: Realistic planetary systems
- **GalacticAnalytics.cs**: Special objects (Sgr A*) and statistics

## Recent Major Updates (December 2024)

### Unified Galaxy Generation
- Single algorithm where position determines density
- Removed separate bulge/disk/halo calculations
- Smooth transitions everywhere - no discontinuities

### Scientifically Accurate Stellar Densities
- Piecewise function matching 14 observational data points exactly
- Galactic center (0 ly): 288 stars/ly³
- 100 ly: 100 stars/ly³
- 500 ly: 5 stars/ly³
- 1000 ly: 1 star/ly³
- 2500 ly: 0.2 stars/ly³
- 5000 ly: 0.04 stars/ly³
- Solar neighborhood (26,000 ly): 0.004 stars/ly³
- Outer disk (40,000 ly): 0.001 stars/ly³
- Far halo (80,000 ly): 0.00005 stars/ly³
- Smooth exponential/power-law interpolation between all points

### Enhanced Spiral Arms
- 6 spiral arms total for better coverage:
  - 2 major arms (strongest, 15° pitch angle)
  - 2 medium arms (moderate, 18° pitch angle)  
  - 2 minor arms (weakest, 22° pitch angle)
- All arms are wider for more realistic appearance
- Minor arms repositioned to avoid overlap with major arms
- Logarithmic spirals create ~2 full galactic rotations

### Visualization Improvements
- Density heatmaps now include distance rulers on side/front views
- Rulers show scale in light years with smart tick intervals
- Axis labels for clarity ("Distance (ly)" and "Height (ly)")
- Better color scaling for new density values

### Removed Features
- Sky view generation (option 9) - removed for performance
- Galaxy statistics generation (option 3) - removed as redundant
- SkyGenerator.cs file deleted

### Bit Encoding Fix
- Expanded star index from 15 to 36 bits
- Supports up to ~68 billion stars per chunk
- Fixed overflow bug that corrupted chunk coordinates

### Performance Optimizations
- Lazy loading with seed ranges for dense regions
- Hierarchical spatial indexing suggested
- Progressive chunk loading for navigation

## Console Interface Menu
1. Export stars for Unity (JSON)
2. Find star by seed
3. (Removed - was galaxy statistics)
4. Generate galaxy images (point cloud)
5. Generate density heatmaps (pure formulas)
6. Investigate galaxy chunk
7. Visualize chunk (generate images)
8. Estimate total galaxy star count
9. (Removed - was sky view)
10. Exit

## Building the Project

```bash
# Console version (lightweight):
./compile-console.bat
cd ScientificMilkyWayConsole
dotnet run

# Visual version (with SkiaSharp):
./compile-visual.bat  
cd ScientificMilkyWayVisual
dotnet run
```

### Requirements
- .NET 8.0 SDK
- Windows/Linux/macOS
- SkiaSharp (auto-installed for visual version)

## Recent Changes

### Unified System Generator V2
- Improved hierarchical stellar and planetary systems with clearer relationships
- Clear distinction between binary companions (close) and satellite stars (distant)
- Supports triple and quadruple star systems with proper hierarchy
- Binary planets and binary moons with proper labeling
- Clean naming scheme: Stars use A, B, C, D; Planets use 1, 2, 3; Moons use a, b, c
- SEED-SUFFIX investigation format (e.g., 12345678-A, 12345678-1, 12345678-1-a)

## Key Features

### Scientific Accuracy
- Realistic density profiles calibrated to astrophysical data
- Smooth bulge-to-disk transition at ~6000 ly
- Exponential disk with 3500 ly scale length
- NFW profile for stellar halo
- Proper vertical scale heights

### Stellar Classification
Harvard spectral classification with luminosity classes:
- **Main Sequence (V)**: O5V, B0V, B5V, A0V, F0V, G0V, K0V, M0V, M5V
- **Giants (III)**: K0III, M0III, B0III
- **Supergiants (I)**: M2I, B0I
- **Compact Objects**: DA, NS, BH, SMBH

### Navigation Performance Solutions
For dense regions with millions of stars per chunk:
- **Metadata Only**: Store star count, density, gravity fields
- **Streaming**: Generate stars on-demand using seeds
- **Spatial Queries**: Check positions without creating objects
- **Progressive Loading**: Load in stages based on needs

## Finding Stars

```
# Direct seed
12345678         # Primary star system overview
12345678-A       # Star A (primary)
12345678-B       # Star B (binary or satellite)
12345678-1       # Planet 1 of primary star
12345678-B-1     # Planet 1 of star B
12345678-1-a     # Moon a of planet 1

# Chunk coordinates  
260_0_0          # Solar neighborhood chunk
260_0_0_100      # Star index 100 in chunk
```

## Visualizing Chunks

Use option 7 to generate images of any chunk:
- Enter chunk ID (e.g., "260_0_0")
- Generates 4 PNG files showing stars with real colors
- Top, side, front views plus composite
- Shows star type statistics

## IMPORTANT DEVELOPMENT RULES

### NEVER Modify Files in ScientificMilkyWayVisual Directory
- **DO NOT** edit files in /ScientificMilkyWayVisual/
- **ALWAYS** edit files in the root directory
- Let compile-visual.bat handle copying files
- This prevents version conflicts

### File Organization
- Core algorithms: GalaxyGenerator.cs
- Spatial system: ChunkBasedGalaxySystem.cs  
- Star properties: ScientificMilkyWayGenerator.cs
- Visualization: ScientificGalaxyVisualizer2.cs, ChunkVisualizer.cs

## Unity Integration

### Memory-Efficient Approach
```csharp
// Don't load all stars - use metadata
ChunkMetadata meta = GetChunkMetadata(chunkId);
GravityField field = GetChunkGravityField(chunkId);

// Stream only visible stars
foreach (var star in StreamVisibleStars(chunkId, playerPos))
{
    RenderStar(star);
}
```

### Key Principles
1. Never load all stars at once
2. Use pre-computed navigation data
3. Stream stars as needed
4. Progressive loading by distance
5. Spatial indexing for queries

## Technical Details

### Coordinate System
- Origin at galactic center
- X-axis toward initial Sun position  
- Y-axis perpendicular in plane
- Z-axis perpendicular to plane
- Units in light years

### Density Calculation Flow
1. Position → GalaxyGenerator.CalculateTotalDensity() → normalized density (0-1)
2. Includes disk profile, spiral arms, halo
3. ConvertToStellarDensity() → actual stars/ly³
4. Different scaling per region for realism

### Chunk System
- Cylindrical coordinates (r, theta, z)
- 100×100×100 ly chunks
- Seeds encode: ChunkR_ChunkTheta_ChunkZ_StarIndex
- Deterministic generation from position

## Credits
Created as a scientifically accurate galaxy simulation for games and visualizations.