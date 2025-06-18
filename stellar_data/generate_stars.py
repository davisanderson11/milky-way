#!/usr/bin/env python3
"""
Generate a comprehensive stars.csv file for Milky Way simulation
Creates ~1400 stars within 80 light years with realistic properties
"""

import csv
import math
import random

# Known nearby stars with accurate data
KNOWN_STARS = [
    # name, ra_hours, dec_degrees, distance_ly, stellar_class, mass, temperature, luminosity, absolute_magnitude
    {"name": "Sol", "x": 0.0, "y": 0.0, "z": 0.0, "stellar_class": "G2V", "mass": 1.0, "temperature": 5778, "luminosity": 1.0, "absolute_magnitude": 4.83},
    
    # Proxima Centauri system
    {"name": "Proxima Centauri", "ra": 14.495, "dec": -62.679, "dist": 4.24, "stellar_class": "M5.5Ve", "mass": 0.122, "temperature": 3042, "luminosity": 0.00005, "absolute_magnitude": 15.6},
    {"name": "Alpha Centauri A", "ra": 14.660, "dec": -60.835, "dist": 4.37, "stellar_class": "G2V", "mass": 1.1, "temperature": 5790, "luminosity": 1.519, "absolute_magnitude": 4.38},
    {"name": "Alpha Centauri B", "ra": 14.660, "dec": -60.835, "dist": 4.37, "stellar_class": "K1V", "mass": 0.907, "temperature": 5260, "luminosity": 0.5002, "absolute_magnitude": 5.71},
    
    # Barnard's Star
    {"name": "Barnard's Star", "ra": 17.963, "dec": 4.693, "dist": 5.96, "stellar_class": "M4Ve", "mass": 0.144, "temperature": 3134, "luminosity": 0.0004, "absolute_magnitude": 13.2},
    
    # Wolf 359
    {"name": "Wolf 359", "ra": 10.940, "dec": 7.014, "dist": 7.86, "stellar_class": "M6.5Ve", "mass": 0.09, "temperature": 2800, "luminosity": 0.00002, "absolute_magnitude": 16.7},
    
    # Lalande 21185
    {"name": "Lalande 21185", "ra": 11.055, "dec": 35.970, "dist": 8.31, "stellar_class": "M2V", "mass": 0.46, "temperature": 3828, "luminosity": 0.021, "absolute_magnitude": 10.5},
    
    # Sirius system
    {"name": "Sirius A", "ra": 6.752, "dec": -16.716, "dist": 8.66, "stellar_class": "A1V", "mass": 2.063, "temperature": 9940, "luminosity": 25.4, "absolute_magnitude": 1.42},
    {"name": "Sirius B", "ra": 6.752, "dec": -16.716, "dist": 8.66, "stellar_class": "DA2", "mass": 1.018, "temperature": 25200, "luminosity": 0.056, "absolute_magnitude": 11.2},
    
    # Luyten 726-8 (UV Ceti)
    {"name": "Luyten 726-8 A", "ra": 1.649, "dec": -17.950, "dist": 8.73, "stellar_class": "M5.5Ve", "mass": 0.102, "temperature": 2670, "luminosity": 0.00006, "absolute_magnitude": 15.3},
    {"name": "Luyten 726-8 B", "ra": 1.649, "dec": -17.950, "dist": 8.73, "stellar_class": "M6Ve", "mass": 0.1, "temperature": 2650, "luminosity": 0.00004, "absolute_magnitude": 16.0},
    
    # Ross 154
    {"name": "Ross 154", "ra": 18.829, "dec": -23.836, "dist": 9.69, "stellar_class": "M3.5Ve", "mass": 0.17, "temperature": 3340, "luminosity": 0.0038, "absolute_magnitude": 13.1},
    
    # Ross 248
    {"name": "Ross 248", "ra": 23.697, "dec": 44.179, "dist": 10.3, "stellar_class": "M5.5Ve", "mass": 0.136, "temperature": 2799, "luminosity": 0.0018, "absolute_magnitude": 14.8},
    
    # Epsilon Eridani
    {"name": "Epsilon Eridani", "ra": 3.549, "dec": -9.458, "dist": 10.5, "stellar_class": "K2V", "mass": 0.82, "temperature": 5084, "luminosity": 0.34, "absolute_magnitude": 6.2},
    
    # Lacaille 9352
    {"name": "Lacaille 9352", "ra": 23.102, "dec": -35.853, "dist": 10.7, "stellar_class": "M0.5V", "mass": 0.486, "temperature": 3688, "luminosity": 0.033, "absolute_magnitude": 9.8},
    
    # Ross 128
    {"name": "Ross 128", "ra": 11.793, "dec": 0.803, "dist": 11.0, "stellar_class": "M4V", "mass": 0.168, "temperature": 3180, "luminosity": 0.00362, "absolute_magnitude": 13.5},
    
    # EZ Aquarii system
    {"name": "EZ Aquarii A", "ra": 22.646, "dec": -15.268, "dist": 11.1, "stellar_class": "M5Ve", "mass": 0.11, "temperature": 2700, "luminosity": 0.00008, "absolute_magnitude": 15.0},
    {"name": "EZ Aquarii B", "ra": 22.646, "dec": -15.268, "dist": 11.1, "stellar_class": "M5Ve", "mass": 0.11, "temperature": 2700, "luminosity": 0.00008, "absolute_magnitude": 15.0},
    {"name": "EZ Aquarii C", "ra": 22.646, "dec": -15.268, "dist": 11.1, "stellar_class": "M7Ve", "mass": 0.08, "temperature": 2500, "luminosity": 0.00002, "absolute_magnitude": 16.5},
    
    # Procyon system
    {"name": "Procyon A", "ra": 7.655, "dec": 5.225, "dist": 11.5, "stellar_class": "F5IV-V", "mass": 1.499, "temperature": 6530, "luminosity": 6.93, "absolute_magnitude": 2.66},
    {"name": "Procyon B", "ra": 7.655, "dec": 5.225, "dist": 11.5, "stellar_class": "DQZ", "mass": 0.602, "temperature": 9700, "luminosity": 0.00049, "absolute_magnitude": 13.0},
    
    # 61 Cygni system
    {"name": "61 Cygni A", "ra": 21.115, "dec": 38.752, "dist": 11.4, "stellar_class": "K5V", "mass": 0.7, "temperature": 4526, "luminosity": 0.153, "absolute_magnitude": 7.5},
    {"name": "61 Cygni B", "ra": 21.115, "dec": 38.752, "dist": 11.4, "stellar_class": "K7V", "mass": 0.63, "temperature": 4077, "luminosity": 0.085, "absolute_magnitude": 8.3},
    
    # Struve 2398 system
    {"name": "Struve 2398 A", "ra": 18.727, "dec": 59.633, "dist": 11.5, "stellar_class": "M3V", "mass": 0.34, "temperature": 3600, "luminosity": 0.036, "absolute_magnitude": 11.2},
    {"name": "Struve 2398 B", "ra": 18.727, "dec": 59.633, "dist": 11.5, "stellar_class": "M3.5V", "mass": 0.25, "temperature": 3500, "luminosity": 0.013, "absolute_magnitude": 11.9},
    
    # Groombridge 34 system
    {"name": "Groombridge 34 A", "ra": 0.304, "dec": 44.024, "dist": 11.6, "stellar_class": "M1.5V", "mass": 0.38, "temperature": 3700, "luminosity": 0.0064, "absolute_magnitude": 10.3},
    {"name": "Groombridge 34 B", "ra": 0.304, "dec": 44.024, "dist": 11.6, "stellar_class": "M3.5V", "mass": 0.16, "temperature": 3300, "luminosity": 0.0004, "absolute_magnitude": 13.3},
    
    # Epsilon Indi system
    {"name": "Epsilon Indi A", "ra": 22.055, "dec": -56.786, "dist": 11.8, "stellar_class": "K5Ve", "mass": 0.766, "temperature": 4280, "luminosity": 0.15, "absolute_magnitude": 7.0},
    {"name": "Epsilon Indi Ba", "ra": 22.055, "dec": -56.786, "dist": 11.8, "stellar_class": "T1", "mass": 0.065, "temperature": 1350, "luminosity": 0.000002, "absolute_magnitude": 23.0},
    {"name": "Epsilon Indi Bb", "ra": 22.055, "dec": -56.786, "dist": 11.8, "stellar_class": "T6", "mass": 0.050, "temperature": 880, "luminosity": 0.0000002, "absolute_magnitude": 24.0},
    
    # DX Cancri
    {"name": "DX Cancri", "ra": 8.483, "dec": 26.780, "dist": 11.8, "stellar_class": "M6.5Ve", "mass": 0.087, "temperature": 2840, "luminosity": 0.00012, "absolute_magnitude": 16.5},
    
    # Tau Ceti
    {"name": "Tau Ceti", "ra": 1.735, "dec": -15.938, "dist": 11.9, "stellar_class": "G8V", "mass": 0.783, "temperature": 5344, "luminosity": 0.52, "absolute_magnitude": 5.7},
    
    # GJ 1061
    {"name": "GJ 1061", "ra": 3.604, "dec": -44.508, "dist": 12.0, "stellar_class": "M5.5V", "mass": 0.125, "temperature": 2977, "luminosity": 0.001, "absolute_magnitude": 15.3},
]

def ra_dec_to_xyz(ra_hours, dec_degrees, distance_ly):
    """Convert RA/Dec/Distance to Cartesian coordinates"""
    ra_radians = (ra_hours * 15.0) * math.pi / 180.0
    dec_radians = dec_degrees * math.pi / 180.0
    
    x = distance_ly * math.cos(dec_radians) * math.cos(ra_radians)
    y = distance_ly * math.cos(dec_radians) * math.sin(ra_radians)
    z = distance_ly * math.sin(dec_radians)
    
    return x, y, z

def generate_stellar_properties(stellar_class):
    """Generate realistic stellar properties based on spectral class"""
    
    # Main sequence data with variations
    spectral_data = {
        'O': {'mass': (15, 90), 'temp': (30000, 50000), 'lum': (30000, 1000000), 'abs_mag': (-6.5, -4.0)},
        'B': {'mass': (2.1, 16), 'temp': (10000, 30000), 'lum': (25, 30000), 'abs_mag': (-4.0, 1.0)},
        'A': {'mass': (1.4, 2.1), 'temp': (7500, 10000), 'lum': (5, 25), 'abs_mag': (1.0, 2.5)},
        'F': {'mass': (1.04, 1.4), 'temp': (6000, 7500), 'lum': (1.5, 5), 'abs_mag': (2.5, 4.5)},
        'G': {'mass': (0.8, 1.04), 'temp': (5200, 6000), 'lum': (0.6, 1.5), 'abs_mag': (4.5, 6.0)},
        'K': {'mass': (0.45, 0.8), 'temp': (3700, 5200), 'lum': (0.08, 0.6), 'abs_mag': (6.0, 9.0)},
        'M': {'mass': (0.08, 0.45), 'temp': (2400, 3700), 'lum': (0.0001, 0.08), 'abs_mag': (9.0, 17.0)},
        'D': {'mass': (0.5, 1.4), 'temp': (8000, 40000), 'lum': (0.0001, 0.01), 'abs_mag': (11.0, 16.0)},
    }
    
    spectral_type = stellar_class[0].upper() if stellar_class else 'M'
    
    if spectral_type not in spectral_data:
        spectral_type = 'M'  # Default to red dwarf
    
    data = spectral_data[spectral_type]
    
    # Generate values within ranges
    mass = random.uniform(*data['mass'])
    temperature = int(random.uniform(*data['temp']))
    luminosity = random.uniform(*data['lum'])
    absolute_magnitude = random.uniform(*data['abs_mag'])
    
    # Apply subclass modifications
    if len(stellar_class) > 1 and stellar_class[1].isdigit():
        subclass = int(stellar_class[1])
        # Interpolate within the spectral class
        factor = (9 - subclass) / 9.0
        
        mass = data['mass'][0] + (data['mass'][1] - data['mass'][0]) * factor
        temperature = int(data['temp'][0] + (data['temp'][1] - data['temp'][0]) * factor)
        luminosity = data['lum'][0] * math.pow(data['lum'][1] / data['lum'][0], factor)
        absolute_magnitude = data['abs_mag'][1] + (data['abs_mag'][0] - data['abs_mag'][1]) * factor
    
    return mass, temperature, luminosity, absolute_magnitude

def generate_random_star(index, max_distance=80):
    """Generate a random star with realistic properties"""
    
    # Star class distribution (approximate for solar neighborhood)
    class_weights = {
        'M': 0.765,  # M dwarfs dominate
        'K': 0.121,
        'G': 0.076,
        'F': 0.030,
        'A': 0.006,
        'D': 0.001,  # White dwarfs
        'B': 0.001,
    }
    
    # Select spectral class
    spectral_type = random.choices(
        list(class_weights.keys()), 
        weights=list(class_weights.values())
    )[0]
    
    # Generate subclass
    subclass = random.randint(0, 9)
    luminosity_class = 'V' if spectral_type != 'D' else ''
    
    stellar_class = f"{spectral_type}{subclass}{luminosity_class}"
    
    # Generate position
    # Use a realistic distribution - more stars closer to us
    distance = random.random() ** 0.33 * max_distance  # Cube root for volume distribution
    
    # Random direction
    theta = random.uniform(0, 2 * math.pi)  # Azimuth
    phi = math.acos(1 - 2 * random.random())  # Polar angle (uniform on sphere)
    
    x = distance * math.sin(phi) * math.cos(theta)
    y = distance * math.sin(phi) * math.sin(theta)
    z = distance * math.cos(phi)
    
    # Generate properties
    mass, temperature, luminosity, absolute_magnitude = generate_stellar_properties(stellar_class)
    
    # Generate name
    name = f"Star-{index:04d}"
    
    return {
        "name": name,
        "x": round(x, 6),
        "y": round(y, 6),
        "z": round(z, 6),
        "stellar_class": stellar_class,
        "mass": round(mass, 3),
        "temperature": temperature,
        "luminosity": round(luminosity, 6),
        "absolute_magnitude": round(absolute_magnitude, 2)
    }

def main():
    """Generate the complete star catalog"""
    
    stars = []
    
    # Add known stars
    for star_data in KNOWN_STARS:
        if "ra" in star_data:
            # Convert RA/Dec to XYZ
            x, y, z = ra_dec_to_xyz(star_data["ra"], star_data["dec"], star_data["dist"])
            star = {
                "name": star_data["name"],
                "x": round(x, 6),
                "y": round(y, 6),
                "z": round(z, 6),
                "stellar_class": star_data["stellar_class"],
                "mass": star_data["mass"],
                "temperature": star_data["temperature"],
                "luminosity": star_data["luminosity"],
                "absolute_magnitude": star_data["absolute_magnitude"]
            }
        else:
            # Already has XYZ coordinates (Sol)
            star = star_data.copy()
        
        stars.append(star)
    
    print(f"Added {len(stars)} known stars")
    
    # Generate additional random stars to reach ~1400 total
    num_random = 1400 - len(stars)
    
    for i in range(num_random):
        stars.append(generate_random_star(i + 1000))
    
    print(f"Generated {num_random} additional stars")
    
    # Sort by distance from Sol
    stars.sort(key=lambda s: math.sqrt(s["x"]**2 + s["y"]**2 + s["z"]**2))
    
    # Write to CSV
    output_file = "stars.csv"
    with open(output_file, 'w', newline='', encoding='utf-8') as f:
        fieldnames = ['name', 'x', 'y', 'z', 'stellar_class', 'mass', 'temperature', 'luminosity', 'absolute_magnitude']
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(stars)
    
    print(f"\nCreated {output_file} with {len(stars)} stars")
    
    # Print some statistics
    print("\nStatistics:")
    print(f"- Total stars: {len(stars)}")
    
    # Count by spectral type
    spectral_counts = {}
    for star in stars:
        spec_type = star["stellar_class"][0] if star["stellar_class"] else "?"
        spectral_counts[spec_type] = spectral_counts.get(spec_type, 0) + 1
    
    print("\nStars by spectral type:")
    for spec_type in sorted(spectral_counts.keys()):
        percentage = (spectral_counts[spec_type] / len(stars)) * 100
        print(f"  {spec_type}: {spectral_counts[spec_type]} ({percentage:.1f}%)")
    
    # Distance statistics
    distances = [math.sqrt(s["x"]**2 + s["y"]**2 + s["z"]**2) for s in stars if s["name"] != "Sol"]
    print(f"\nDistance range: {min(distances):.2f} - {max(distances):.2f} light years")
    print(f"Average distance: {sum(distances)/len(distances):.2f} light years")
    
    # Check for specific stars
    print("\nKey stars included:")
    key_names = ["Sol", "Proxima Centauri", "Alpha Centauri A", "Alpha Centauri B", 
                 "Sirius A", "Sirius B", "Procyon A", "Procyon B"]
    for name in key_names:
        found = any(s["name"] == name for s in stars)
        print(f"  {name}: {'✓' if found else '✗'}")

if __name__ == "__main__":
    main()