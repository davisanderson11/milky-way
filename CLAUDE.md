# Scientific Milky Way Galaxy Simulator - Project Context

## Overview
This is a scientific galaxy simulation system that generates a realistic representation of the Milky Way galaxy using C#. The system uses a chunk-based approach for efficient memory usage and implements proper astronomical distributions for stellar populations.

## Key Components

### Core Systems
1. **ChunkBasedGalaxySystem.cs** - Main galaxy generation using chunk-based seeding
2. **UnifiedSystemGenerator.cs** - Hierarchical stellar and planetary system generation
3. **StellarTypeGenerator.cs** - Determines stellar types based on galactic position
4. **GalaxyGenerator.cs** - Core galaxy structure and density calculations
5. **RealStellarData.cs** - Loads and manages real star catalogs with planets and moons
6. **RoguePlanet.cs** - Rogue planet generation and management
7. **Star.cs** - Star object representation with real data support

### Visualization
1. **DensityVisualizer.cs** - Creates density heatmaps of the galaxy
2. **ChunkVisualizer.cs** - Visualizes chunk boundaries and contents
3. **ScientificMilkyWayConsole.cs** - Console interface for exploring the galaxy

### Data Files
1. **stellar_data/stars.csv** - 1,398 real stars within 80 ly with coordinates and properties
2. **stellar_data/planets.csv** - Known exoplanets and solar system planets
3. **stellar_data/moons.csv** - Moons of solar system planets

## Technical Details

### Coordinate Systems
- Cylindrical coordinates (r, theta, z) for galaxy structure
- Cartesian coordinates for distance calculations
- Chunk-based system: 100 light-year chunks
- RA/Dec coordinates for real stars converted to galactic XYZ

### Seed System
- Seeds encode: ChunkR_ChunkTheta_ChunkZ_ObjectIndex
- Special seeds for real stars (bit 35 set)
- Negative indices for rogue planets
- Companion stars don't get separate seeds (redirect to primary)

### Real Stellar Data Integration
- 1,398 real stars from comprehensive catalog
- Proper companion star grouping (B, C stars under primary A star)
- Planet/moon associations from CSV data
- Stars without known planets use procedural generation
- Proper name matching handles complex designations

### Binary/Multiple Star Systems
- Companions properly grouped under primary stars
- "Procyon B" searches redirect to "Procyon A" system
- Binary relationships preserved in tree display
- No duplicate systems for companion stars

## Usage Examples

### Investigating Objects
```
Enter seed: Sol                    # Shows real solar system with 8 planets
Enter seed: Procyon               # Shows Procyon A with B as companion
Enter seed: Alpha Centauri        # Shows full system with companions
Enter seed: 265_0_0_100          # Chunk coordinate format
Enter seed: 12345678-B-2-a       # Star B, planet 2, moon a
```

### Special Commands
```
list real                         # List all real stars
visualize density 2048 5          # Generate density heatmaps
```

## Building the Project

```bash
# Console version (lightweight):
./compile-console.bat
ScientificMilkyWayConsole.exe

# Visual version (with SkiaSharp):
./compile-visual.bat  
cd ScientificMilkyWayVisual
dotnet run
```

### Requirements
- .NET 8.0 SDK
- Windows/Linux/macOS
- SkiaSharp (auto-installed for visual version)

## Recent Updates (December 2024)
- Expanded real star catalog from ~300 to 1,398 stars
- Fixed star-planet-moon associations from CSV data
- Implemented proper companion star grouping (no duplicate systems)
- Added moon loading for solar system planets
- Fixed name matching for complex star designations
- Stars without planet data use procedural generation
- Companion star searches redirect to primary star system
- Rogue planet system with proper density distribution
- 4-panel density visualization with rogue planets

## IMPORTANT DEVELOPMENT RULES

### NEVER Modify Files in ScientificMilkyWayVisual Directory
- **DO NOT** edit files in /ScientificMilkyWayVisual/
- **ALWAYS** edit files in the root directory
- Let compile-visual.bat handle copying files
- This prevents version conflicts

### File Organization
- Core algorithms: GalaxyGenerator.cs
- Spatial system: ChunkBasedGalaxySystem.cs  
- Star properties: StellarTypeGenerator.cs
- Real data: RealStellarData.cs
- Visualization: DensityVisualizer.cs, ChunkVisualizer.cs