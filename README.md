# Milky Way Galaxy Simulation

A scientifically accurate simulation of the Milky Way galaxy with 100 billion unique stars, featuring realistic stellar populations, spiral arms, and astronomical properties.

## Features

- **100 Billion Unique Stars**: Deterministic generation system ensures consistent properties for each star
- **Realistic Stellar Types**: Uses Harvard spectral classification (O5V, B0V, A0V, F0V, G5V, K5V, M5V, etc.)
- **Multiple Stellar Populations**: PopIII, PopII, Thin Disk, Thick Disk, Halo, and Bulge populations
- **Spiral Arm Structure**: Four logarithmic spiral arms with realistic density distributions
- **Binary/Multiple Star Systems**: Statistically accurate companion star generation
- **Planetary Systems**: Realistic planet generation based on stellar properties
- **Special Objects**: Includes Sagittarius A* (supermassive black hole)

## Quick Start

### Building the Project

**Console Version** (lightweight, no graphics):
```bash
./compile-console.bat
cd ScientificMilkyWayConsole
dotnet run
```

**Visual Version** (includes image generation):
```bash
./compile-visual.bat
cd ScientificMilkyWayVisual
dotnet run
```

### Requirements

- .NET 8.0 SDK
- Windows/Linux/macOS
- SkiaSharp (auto-installed for visual version)

## Usage

The console application provides a menu-driven interface with options to:

1. Export stars for Unity (JSON format)
2. Find stars by seed using the chunk-based system
3. Generate galaxy statistics
4. Generate galaxy visualization images
5. Analyze stellar distributions
6. Investigate specific galaxy chunks

### Finding Stars

The new chunk-based system supports multiple formats:
- **Encoded seed**: `12345678`
- **Chunk coordinates**: `260_45_0_100` (r_theta_z_index)
- **Companion stars**: `12345678-A` or `260_45_0_100_A`
- **Planets**: `12345678-1` or `260_45_0_100_1`
- **Moons**: `12345678-1-a` or `260_45_0_100_1_a`

## Architecture

### Core Components

- **ScientificMilkyWayGenerator.cs**: Main star generation engine
- **ChunkBasedGalaxySystem.cs**: Efficient spatial indexing system
- **CompanionStarDatabase.cs**: Binary/multiple star system management
- **PlanetarySystemGenerator.cs**: Planetary system generation
- **SpecialGalacticObjects.cs**: Special astronomical objects

### Stellar Classification

Stars use proper astronomical notation:
- Main sequence: O5V, B0V, B5V, A0V, A5V, F0V, F5V, G0V, G5V, K0V, K5V, M0V, M5V, M8V
- Giants: K0III, K5III, M0III (red giants), B0III (blue giant)
- Supergiants: M2I (red supergiant), B0I (blue supergiant)
- Compact objects: DA (white dwarf), NS (neutron star), BH (black hole), SMBH (supermassive black hole)

## Scientific Accuracy

The simulation implements:
- Hernquist profile for galactic bulge
- Exponential disk model with proper scale heights
- Logarithmic spiral arms with correct pitch angles
- Realistic stellar initial mass function
- Proper stellar density (~0.14 stars/ly³ at Sun's position)
- Central bar at 25° angle, 10,000 ly long

## License

This project is open source. Feel free to use and modify for your own projects.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.