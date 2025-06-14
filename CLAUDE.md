# Milky Way Galaxy Simulation Project

## Overview
This is a scientifically accurate Milky Way galaxy generator that creates a simulation of 100 billion unique stars. The project uses the chunk-based system exclusively and features realistic stellar classifications based on the Harvard spectral system.

## Recent Major Changes (June 2025)
- **Removed old system**: Now uses only the chunk-based generation system
- **Updated stellar types**: All stars now use proper astronomical notation (O5V, G2V, K0III, etc.)
- **Enhanced companion stars**: Companions now have varied stellar types based on mass
- **Improved star finder**: Supports both underscore (260_45_0_100_A) and dash (12345678-A) formats
- **Cleaned up codebase**: Removed consistency tests and legacy code
- **NEW: Companion star planetary systems**: Each companion star can now have its own planetary system with stable orbit calculations
- **Project Cleanup (Latest)**: Removed unused file GalaxyChunkSystem.cs and unused methods from SpecialGalacticObjects.cs and ScientificMilkyWayGenerator.cs
- **Visualization Update**: Removed ScientificGalaxyVisualizer.cs in favor of the superior ScientificGalaxyVisualizer2.cs density-based implementation
- **Code Consolidation**: Combined companion star files into MultipleStarSystems.cs and analytics files into GalacticAnalytics.cs
- **Galaxy Structure Improvements**: 
  - Fixed bulge parameters for more realistic peanut/boxy shape
  - Adjusted vertical scale heights (bulge: 1400 ly, thick disk: 900 ly, thin disk: 300 ly)
  - Fixed density singularity issues in central regions
  - Added configurable Z-axis exaggeration for side view (1x for realistic, up to 10x for visibility)
  - Eliminated visual artifacts (spikes near core, donut effect)
  - Implemented smooth power-law density profiles (1/r² to 1/r³) for realistic falloff
  - Fixed stellar type frequencies to match real-world observations:
    - Black holes: ~0.001% (1 million total)
    - Neutron stars: ~0.002% (2 million total)
    - White dwarfs: 0.5-1% (varies by population)
    - Main sequence follows Kroupa IMF
- **Statistics Improvements**:
  - Combined regular and advanced statistics into comprehensive viewer
  - Added interesting astronomical facts and context
  - Improved performance with optional sampling sizes
  - Added extreme star tracking (most massive, most luminous)

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
- **MultipleStarSystems.cs**: Comprehensive binary/multiple star system management with planetary support
- **GalacticAnalytics.cs**: Special objects (Sagittarius A*) and statistical analysis tools

### Visualization Components

- **ScientificGalaxyVisualizer2.cs**: Density-based rendering with SkiaSharp
- Uses importance sampling to accurately represent galaxy density
- Supports top-down, side, and 3D perspective views
- Generates PNG images and heatmaps
- Configurable Z-axis exaggeration for side view (1-10x, default 5x)
- Side view can use realistic 1:1 proportions or exaggerated vertical scale

## Key Features

### Scientific Accuracy
- Modified Hernquist profile for galactic bulge (with core to avoid singularity)
- Bulge radius: 5,000 ly with vertical scale height of 1,400 ly (peanut/boxy shape)
- Exponential disk model with proper scale heights:
  - Thin disk: 300 ly scale height (85% of disk stars)
  - Thick disk: 900 ly scale height (15% of disk stars)
- Power-law density profiles with smooth transitions:
  - Core: ~1/r³ falloff
  - Bulge: ~1/r²·⁵ falloff
  - Disk: ~1/r¹·⁵ falloff
- Logarithmic spiral arms with realistic pitch angles (~12.5°)
- Central bar at 25° angle, 10,000 ly long, 3,000 ly wide
- Solar position at 26,000 ly from center
- Density of ~0.14 stars/ly³ at Sun's location
- Realistic stellar type frequencies based on Kroupa IMF and population ages

### Stellar Classification System
Uses proper Harvard spectral classification with luminosity classes:
- **Main Sequence (V)**: O5V, B0V, B5V, A0V, A5V, F0V, F5V, G0V, G5V, K0V, K5V, M0V, M5V, M8V
- **Giants (III)**: K0III, K5III, M0III (red giants), B0III (blue giant)
- **Supergiants (I)**: M2I (red supergiant), B0I (blue supergiant)
- **Compact Objects**: DA (white dwarf), NS (neutron star), BH (black hole), SMBH (Sgr A*)

### Star Properties
Each star includes:
- 3D position (x, y, z in light years)
- Spectral type with proper classification (e.g., G2V, K0III, M5V)
- Mass, temperature, luminosity
- Age and metallicity
- Population type
- Habitable zone boundaries
- Companion star information with varied stellar types

### Planetary Systems
- Generated based on stellar properties
- Realistic orbital distances
- Planet types (terrestrial, gas giant, ice giant)
- Moon systems for larger planets
- Habitable zone considerations
- **NEW: Companion stars have their own planetary systems**
  - Each companion star can host planets
  - Stable orbit calculations for multi-star systems
  - S-type orbits (planets orbit one star) with stability constraints
  - Orbital limits based on companion star separations
  - Access companion planets with formats like `260_45_0_100_A_1` or `12345678-A-1`

## Usage Examples

### Console Interface Menu
1. Export stars for Unity (JSON)
2. Find star by seed (chunk-based system)
3. Generate galaxy statistics
4. Generate galaxy images
5. Advanced analytical statistics
6. Investigate galaxy chunk
7. Exit

The system now exclusively uses the chunk-based approach with no option to switch back to the old system.

### Code Usage
```csharp
// Use chunk-based system (now the only system)
var chunkSystem = new ChunkBasedGalaxySystem();

// Find a star by encoded seed
var star = chunkSystem.GetStarBySeed(12345678);

// Generate stars in a chunk at r=1000ly, theta=45°, z=0
var stars = chunkSystem.GenerateChunkStars(10, 45, 0);

// Find star with specific chunk coordinates
long seed = ChunkBasedGalaxySystem.EncodeSeed(260, 45, 0, 100);
var star2 = chunkSystem.GetStarBySeed(seed);

// Generate visualization
var generator = new ScientificMilkyWayGenerator();
var visualizer = new ScientificGalaxyVisualizer2(generator);
visualizer.GenerateAllViews(4096, 4096, 500000); // Default 5x Z exaggeration
// Or with custom Z exaggeration:
visualizer.GenerateAllViews(4096, 4096, 500000, 1.0f); // Realistic 1:1 proportions
```

### Finding Stars - Format Examples
```
# Encoded seed format
12345678            # Direct seed lookup
12345678-A          # Companion star A
12345678-1          # Planet 1
12345678-1-a        # Moon a of planet 1
12345678-A-2        # Planet 2 of companion A
12345678-A-2-b      # Moon b of planet 2 of companion A

# Chunk coordinate format
260_45_0_100        # Chunk r=260, theta=45, z=0, star index 100
260_45_0_100_A      # Companion star A
260_45_0_100_1      # Planet 1
260_45_0_100_1_a    # Moon a of planet 1
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

### Missing Files in Visual Project
The compile-visual.bat script now automatically copies all required files including ChunkBasedGalaxySystem.cs.

### Build Errors
Ensure .NET 8.0 SDK is installed:
```bash
dotnet --version
```

### Stellar Type Errors
All stellar types now use the new classification system. If you see errors about old types (like "WhiteDwarf" or "RedGiant"), ensure you're using the latest version of all files.

### Memory Issues with Large Exports
Use chunk-based generation and process in batches. The chunk system allows you to generate specific regions without loading the entire galaxy.

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

## Converting to Unity

### Approach 1: Direct Integration
1. **Copy Core Classes**: Take these essential files to Unity:
   - `ScientificMilkyWayGenerator.cs` (core generation logic)
   - `ChunkBasedGalaxySystem.cs` (spatial indexing)
   - `CompanionStarDatabase.cs` (if you want binary systems)
   - `PlanetarySystemGenerator.cs` (if you want planets)

2. **Remove System Dependencies**:
   - Replace `Console.WriteLine` with `Debug.Log`
   - The Vector3 struct can be replaced with Unity's Vector3
   - Remove file I/O code or adapt to Unity's file system

3. **Create Unity Components**:
   ```csharp
   public class GalaxyManager : MonoBehaviour
   {
       private ChunkBasedGalaxySystem galaxySystem;
       
       void Start()
       {
           galaxySystem = new ChunkBasedGalaxySystem();
       }
   }
   ```

### Approach 2: Dynamic LOD System
1. **Chunk-Based Loading**: Use the chunk system for level-of-detail:
   - Load nearby chunks in detail
   - Use lower detail for distant chunks
   - Unload chunks outside view distance

2. **Star Rendering**:
   - Use GPU instancing for performance
   - Create star prefabs for different types
   - Use particle systems for distant stars

### Approach 3: Precomputed Data
1. **Export from Console App**: Generate star data files
2. **Import to Unity**: Load JSON/binary files as needed
3. **Streaming System**: Load chunks on demand

### Key Considerations
- **Memory**: 100 billion stars = ~4TB if all loaded. Use chunking!
- **Precision**: Unity uses floats, may need origin shifting for large scales
- **Performance**: Generate stars on background threads
- **Visuals**: Use LOD system - dots for distant, models for nearby

### Example Unity Integration
```csharp
// Simple star spawner
public class StarSpawner : MonoBehaviour
{
    public GameObject starPrefab;
    private ChunkBasedGalaxySystem galaxy;
    
    void SpawnLocalStars(Vector3 playerPos)
    {
        // Convert Unity position to galaxy coordinates
        int chunkR = (int)(playerPos.magnitude / 100);
        int chunkTheta = (int)(Mathf.Atan2(playerPos.z, playerPos.x) * Mathf.Rad2Deg);
        int chunkZ = (int)(playerPos.y / 100);
        
        // Generate stars for this chunk
        var stars = galaxy.GenerateChunkStars(chunkR, chunkTheta, chunkZ);
        
        foreach (var star in stars)
        {
            // Spawn star GameObject
            var go = Instantiate(starPrefab);
            go.transform.position = new Vector3(star.Position.X, star.Position.Z, star.Position.Y);
            // Set color, size based on star.Type
        }
    }
}
```

## Credits
Created as a scientifically accurate galaxy simulation for games and visualizations.