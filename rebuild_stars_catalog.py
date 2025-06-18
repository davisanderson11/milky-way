#!/usr/bin/env python3
import csv
import re
import math
import random

def ra_dec_to_xyz(ra_hours, dec_degrees, distance_ly):
    """Convert RA/Dec/Distance to Cartesian coordinates"""
    ra_radians = (ra_hours * 15.0) * math.pi / 180.0
    dec_radians = dec_degrees * math.pi / 180.0
    
    x = distance_ly * math.cos(dec_radians) * math.cos(ra_radians)
    y = distance_ly * math.cos(dec_radians) * math.sin(ra_radians)
    z = distance_ly * math.sin(dec_radians)
    
    return x, y, z

def parse_coordinates(coord_str):
    """Parse RA/Dec from coordinate string"""
    if not coord_str or coord_str == 'N/A':
        return None, None
    
    # Try to extract RA and Dec from various formats
    # Format: "HHh MMm SS.Ss ±DD° MM′ SS″" or similar
    ra_match = re.search(r'(\d+)h\s*(\d+)m\s*([\d.]+)s', coord_str)
    dec_match = re.search(r'([+-]?\d+)[°�]\s*(\d+)[′\']\s*([\d.]+)[″"]?', coord_str)
    
    if ra_match and dec_match:
        ra_h = float(ra_match.group(1))
        ra_m = float(ra_match.group(2))
        ra_s = float(ra_match.group(3))
        ra_hours = ra_h + ra_m/60 + ra_s/3600
        
        dec_d = float(dec_match.group(1))
        dec_m = float(dec_match.group(2))
        dec_s = float(dec_match.group(3))
        dec_sign = -1 if dec_d < 0 else 1
        dec_degrees = dec_d + dec_sign * (dec_m/60 + dec_s/3600)
        
        return ra_hours, dec_degrees
    
    return None, None

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
        'L': {'mass': (0.06, 0.08), 'temp': (1300, 2400), 'lum': (0.00001, 0.0001), 'abs_mag': (17.0, 20.0)},
        'T': {'mass': (0.02, 0.06), 'temp': (600, 1300), 'lum': (0.000001, 0.00001), 'abs_mag': (20.0, 25.0)},
        'Y': {'mass': (0.01, 0.02), 'temp': (300, 600), 'lum': (0.0000001, 0.000001), 'abs_mag': (25.0, 30.0)},
        'D': {'mass': (0.5, 1.4), 'temp': (8000, 40000), 'lum': (0.0001, 0.01), 'abs_mag': (11.0, 16.0)},
    }
    
    if not stellar_class:
        stellar_class = 'M5V'
    
    spectral_type = stellar_class[0].upper()
    
    if spectral_type not in spectral_data:
        # Try to extract from complex classifications
        for char in stellar_class:
            if char.upper() in spectral_data:
                spectral_type = char.upper()
                break
        else:
            spectral_type = 'M'  # Default to red dwarf
    
    data = spectral_data[spectral_type]
    
    # Generate base values
    mass = random.uniform(*data['mass'])
    temperature = int(random.uniform(*data['temp']))
    luminosity = random.uniform(*data['lum'])
    absolute_magnitude = random.uniform(*data['abs_mag'])
    
    # Apply subclass modifications if present
    subclass_match = re.search(r'(\d+\.?\d*)', stellar_class)
    if subclass_match:
        subclass = float(subclass_match.group(1))
        if subclass <= 9:
            factor = (9 - subclass) / 9.0
            
            mass = data['mass'][0] + (data['mass'][1] - data['mass'][0]) * factor
            temperature = int(data['temp'][0] + (data['temp'][1] - data['temp'][0]) * factor)
            luminosity = data['lum'][0] * math.pow(data['lum'][1] / data['lum'][0], factor)
            absolute_magnitude = data['abs_mag'][1] + (data['abs_mag'][0] - data['abs_mag'][1]) * factor
    
    return mass, temperature, luminosity, absolute_magnitude

print("Parsing star-ref.csv to extract all stars...")

stars = []
companion_mapping = {}  # Maps companion names to primary star names
current_system = None
primary_star_name = None

with open('/mnt/c/users/davis/milky-way/stellar_data/star-ref.csv', 'r', encoding='latin-1') as f:
    lines = f.readlines()
    
    for i, line in enumerate(lines[1:], 1):  # Skip header
        if not line.strip():
            continue
        
        # Split by comma but be careful with embedded commas
        parts = line.split(',')
        if len(parts) < 5:
            continue
        
        system = parts[0].strip()
        name = parts[1].strip()
        distance_str = parts[2].strip()
        coord_str = parts[3].strip() if len(parts) > 3 else ""
        stellar_class = parts[4].strip() if len(parts) > 4 else ""
        
        # Update current system
        if system:
            current_system = system
            primary_star_name = None  # Reset primary star for new system
        
        # Skip empty names
        if not name:
            continue
        
        # Clean up name
        name = name.replace('�', ' ').replace('¤', '').replace('à', ' ')
        name = re.sub(r'\s+', ' ', name).strip()
        name = name.replace('$', '')  # Remove $ markers
        
        # Check if this is a companion star
        is_companion = False
        companion_letter = None
        
        # Check for companion patterns
        if len(name) <= 2 and name in ['A', 'B', 'C', 'D', 'Ba', 'Bb', 'Ab', 'Bc', 'Bd']:
            is_companion = True
            companion_letter = name
            # Use the system name with companion letter
            if current_system:
                name = f"{current_system} {companion_letter}"
        elif current_system and name != current_system:
            # Check if name ends with companion letter
            for suffix in [' B', ' C', ' D', ' Ba', ' Bb', ' Ab']:
                if name.endswith(suffix):
                    is_companion = True
                    companion_letter = suffix.strip()
                    break
        
        # Special case for Proxima Centauri (it's Alpha Centauri C)
        if 'Proxima' in name and 'Centauri' in name:
            is_companion = True
            companion_letter = 'C'
            primary_star_name = "Alpha Centauri A"
            companion_mapping["Proxima Centauri"] = "Alpha Centauri A"
        
        # Extract distance
        if not distance_str or distance_str == 'N/A':
            continue
        
        match = re.search(r'([\d.]+)', distance_str)
        if not match:
            continue
        
        distance = float(match.group(1))
        
        # Check if it's already in light years or needs conversion from parsecs
        # If the number is small or explicitly mentions parsecs, convert
        if distance < 100 or 'pc' in distance_str or 'parsec' in distance_str:
            distance = distance * 3.26156  # Convert to ly
        
        # Skip stars beyond 80 ly
        if distance > 80:
            continue
        
        # Parse coordinates if available
        ra, dec = parse_coordinates(coord_str)
        
        # Generate x, y, z coordinates
        if ra is not None and dec is not None:
            x, y, z = ra_dec_to_xyz(ra, dec, distance)
        else:
            # Generate random position at the given distance
            theta = random.uniform(0, 2 * math.pi)
            phi = math.acos(1 - 2 * random.random())
            
            x = distance * math.sin(phi) * math.cos(theta)
            y = distance * math.sin(phi) * math.sin(theta)
            z = distance * math.cos(phi)
        
        # Generate stellar properties
        if stellar_class:
            mass, temperature, luminosity, absolute_magnitude = generate_stellar_properties(stellar_class)
        else:
            # Default properties for unknown class
            mass, temperature, luminosity, absolute_magnitude = 0.15, 3000, 0.001, 14.0
            stellar_class = "M5V"
        
        star_data = {
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
        
        # Track primary star
        if not is_companion and not primary_star_name:
            primary_star_name = name
        
        # Map companions to primary star
        if is_companion and primary_star_name:
            companion_mapping[name] = primary_star_name
            print(f"  Companion: {name} -> Primary: {primary_star_name}")
        
        stars.append(star_data)

# Add Sol if not already present
if not any(s['name'] == 'Sol' for s in stars):
    stars.insert(0, {
        "name": "Sol",
        "x": 0.0,
        "y": 0.0,
        "z": 0.0,
        "stellar_class": "G2V",
        "mass": 1.0,
        "temperature": 5778,
        "luminosity": 1.0,
        "absolute_magnitude": 4.83
    })

print(f"\nExtracted {len(stars)} stars from star-ref.csv")
print(f"Identified {len(companion_mapping)} companion stars")

# Sort by distance from Sol
stars.sort(key=lambda s: math.sqrt(s["x"]**2 + s["y"]**2 + s["z"]**2))

# Write to CSV
with open('/mnt/c/users/davis/milky-way/stellar_data/stars.csv', 'w', newline='') as f:
    fieldnames = ['name', 'x', 'y', 'z', 'stellar_class', 'mass', 'temperature', 'luminosity', 'absolute_magnitude']
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(stars)

print(f"\nCreated stars.csv with {len(stars)} stars")

# Write companion mapping for reference
with open('/mnt/c/users/davis/milky-way/stellar_data/companion_mapping.csv', 'w', newline='') as f:
    writer = csv.writer(f)
    writer.writerow(['companion_name', 'primary_star_name'])
    for companion, primary in sorted(companion_mapping.items()):
        writer.writerow([companion, primary])

print(f"Created companion_mapping.csv with {len(companion_mapping)} mappings")

# Print statistics
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

# Check key systems
print("\nKey star systems:")
key_systems = ["Sol", "Alpha Centauri", "Sirius", "Procyon", "61 Cygni", "Epsilon Eridani", "Tau Ceti"]
for system in key_systems:
    system_stars = [s for s in stars if system in s['name']]
    if system_stars:
        print(f"  {system}: {len(system_stars)} stars")
        for star in system_stars:
            comp_status = " (companion)" if star['name'] in companion_mapping else ""
            print(f"    - {star['name']}{comp_status}")