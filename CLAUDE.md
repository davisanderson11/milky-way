# Milky Way Galaxy Simulation Project

## Overview
This is a scientifically accurate Milky Way galaxy generator that creates a simulation of 100 billion unique stars. The project provides both console and visual interfaces for generating, analyzing, and visualizing galactic data.

## Quick Start

### Building the Project
```bash
# Console version (lightweight, no graphics):
./compile-console.bat
cd ScientificMilkyWayConsole
dotnet run

# Visual version (includes image generation):
./compile-visual.bat
cd ScientificMilkyWayVisual
dotnet run
```

### Requirements
- .NET 8.0 SDK
- Windows/Linux/macOS
- SkiaSharp (auto-installed for visual version)

## Project Architecture

### Core Components

#### 1. ScientificMilkyWayGenerator.cs
The heart of the simulation system. Key features:
- Generates 100 billion unique stars deterministically
- 14 stellar types (O, B, A, F, G, K, M main sequence + evolved stars)
- 6 stellar populations (PopIII, PopII, Thin/Thick Disk, Halo, Bulge)
- Realistic spiral arm structure
- Density-based positioning using astrophysical models

#### 2. Generation Systems

**Original System (Seeds 0-99,999,999,999)**
- Direct seed-to-star mapping
- Sequential generation
- Good for exports and specific star lookups

**Chunk-Based System (ChunkBasedGalaxySystem.cs)**
- Galaxy divided into 100×100×100 ly chunks
- Cylindrical coordinates (r, theta, z)
- Seeds encode: ChunkR_ChunkTheta_ChunkZ_StarIndex
- Instant spatial queries
- Better for "stars near position" operations

#### 3. Supporting Systems

- **PlanetarySystemGenerator.cs**: Creates realistic planetary systems
- **CompanionStarDatabase.cs**: Manages binary/multiple star systems
- **SpecialGalacticObjects.cs**: Handles unique objects (Sagittarius A*, etc.)
- **AdvancedGalaxyStatistics.cs**: Analysis and verification tools

### Visualization Components

- **ScientificGalaxyVisualizer.cs**: Basic visualization with SkiaSharp
- **ScientificGalaxyVisualizer2.cs**: Advanced density-based rendering
- Supports top-down, side, and 3D perspective views
- Generates PNG images and heatmaps

## Key Features

### Scientific Accuracy
- Hernquist profile for galactic bulge
- Exponential disk model with proper scale heights
- Logarithmic spiral arms with realistic pitch angles
- Central bar at 25° angle, 10,000 ly long
- Solar position at 26,000 ly from center
- Density of ~0.14 stars/ly³ at Sun's location

### Star Properties
Each star includes:
- 3D position (x, y, z in light years)
- Spectral type and luminosity class
- Mass, temperature, luminosity
- Age and metallicity
- Population type
- Habitable zone boundaries
- Companion star information

### Planetary Systems
- Generated based on stellar properties
- Realistic orbital distances
- Planet types (terrestrial, gas giant, ice giant)
- Moon systems for larger planets
- Habitable zone considerations

## Usage Examples

### Console Interface Menu
1. Generate a test sample of N stars
2. Find a star by seed
3. Generate full galaxy export (JSON)
4. Generate Unity-compatible export
5. Investigate a specific chunk
6. View nearby stars
7. Companion star statistics
8. Generate planetary system
9. Find habitable planets

### Code Usage
```csharp
// Generate a single star
var generator = new ScientificMilkyWayGenerator();
var star = generator.GenerateStar(12345);

// Use chunk-based system
var chunkSystem = new ChunkBasedGalaxySystem();
var stars = chunkSystem.GenerateChunkStars(10, 45, 0); // r=1000ly, theta=45°, z=0

// Generate visualization
var visualizer = new ScientificGalaxyVisualizer();
visualizer.GenerateVisualization(stars, 4096, 4096, GalaxyView.Top);
```

## File Formats

### JSON Export
```json
{
  "seed": 12345,
  "position": { "x": 1500.5, "y": -200.3, "z": 50.1 },
  "type": "G2V",
  "mass": 1.0,
  "temperature": 5778,
  "luminosity": 1.0,
  "age": 4.6,
  "metallicity": 0.0,
  "population": "ThinDisk",
  "habitableZoneInner": 0.95,
  "habitableZoneOuter": 1.37
}
```

### Unity Export
Optimized format with position, color, and size for efficient rendering.

## Performance Considerations

- Original system: ~1 million stars/second generation
- Chunk system: Instant generation of any spatial region
- Memory efficient: Stars generated on-demand
- Deterministic: Same seed always produces same star

## Testing

Run consistency tests:
```bash
./test_consistency.bat
```

This verifies that:
- Star generation is deterministic
- Chunk system matches original system
- Density calculations are correct

## Common Issues and Solutions

### Missing ChunkBasedGalaxySystem Error
Copy the file to the Visual project:
```bash
cp ChunkBasedGalaxySystem.cs ScientificMilkyWayVisual/
```

### Build Errors
Ensure .NET 8.0 SDK is installed:
```bash
dotnet --version
```

### Memory Issues with Large Exports
Use chunk-based generation and process in batches.

## Future Enhancements

1. Black hole distribution modeling
2. Nebula and star cluster generation
3. Time evolution simulation
4. Gravitational interaction modeling
5. More detailed planetary atmospheres
6. Asteroid belt generation

## Technical Details

### Coordinate System
- Origin at galactic center
- X-axis points toward Sun's initial position
- Y-axis perpendicular in galactic plane
- Z-axis perpendicular to galactic plane
- Units in light years

### Seed Encoding (Chunk System)
- Bits 0-16: Star index within chunk (0-100,000)
- Bits 17-23: Z chunk coordinate (0-100)
- Bits 24-32: Theta chunk coordinate (0-359)
- Bits 33-42: R chunk coordinate (0-599)

### Performance Optimizations
- Importance sampling for density calculations
- Cached trigonometric calculations
- Bit manipulation for seed encoding
- Parallel generation support

## Credits
Created as a scientifically accurate galaxy simulation for games and visualizations.