#!/usr/bin/env python3
import csv
import re
import math
import random

def clean_text(text):
    """Clean text encoding issues"""
    if not text:
        return text
    
    # Replace encoding artifacts
    text = text.replace('�', ' ')
    text = text.replace('°', '')
    text = text.replace('±', '+/-')
    text = text.replace('–', '-')
    text = text.replace('—', '-')
    text = text.replace('$', '')
    text = text.replace('&', '')
    text = text.replace('"', '')
    text = text.replace('  ', ' ')
    text = re.sub(r'\s+', ' ', text)
    return text.strip()

def parse_distance(distance_str):
    """Extract distance in light years - be more liberal"""
    if not distance_str or distance_str == 'N/A' or distance_str == '':
        return None
    
    # Clean the string
    distance_str = clean_text(distance_str)
    
    # Extract first number
    match = re.search(r'([\d.]+)', distance_str)
    if not match:
        return None
    
    distance = float(match.group(1))
    
    # Check context to determine if parsecs or light years
    # The original catalog lists everything in the format "X.XX � Y.YY"
    # Numbers under ~25 are definitely parsecs (since nearest star is 4.24 ly)
    # Numbers 25-80 could be either, but likely parsecs given the format
    # Numbers over 80 are likely parsecs too since we want stars within 80 ly
    
    # Convert all to light years (the catalog appears to be in parsecs)
    if distance <= 25:  # Definitely parsecs
        distance = distance * 3.26156
    else:
        # Still likely parsecs based on catalog format
        distance_ly = distance * 3.26156
        if distance_ly <= 260:  # Within ~80 parsecs = 260 ly
            distance = distance_ly
        else:
            # Might already be in light years
            pass
    
    return distance

def generate_random_position(distance):
    """Generate random position at given distance"""
    theta = random.uniform(0, 2 * math.pi)
    phi = math.acos(1 - 2 * random.random())
    
    x = distance * math.sin(phi) * math.cos(theta)
    y = distance * math.sin(phi) * math.sin(theta)
    z = distance * math.cos(phi)
    
    return x, y, z

def infer_stellar_class(name, stellar_class):
    """Infer stellar class for well-known stars if missing"""
    if stellar_class and stellar_class.strip() and stellar_class != '?':
        return stellar_class
    
    # Common stellar class for unnamed
    # Most stars within 80 ly are M dwarfs
    return 'M4V'

def generate_stellar_properties(stellar_class):
    """Generate realistic stellar properties based on spectral class"""
    if not stellar_class or stellar_class == '?':
        stellar_class = 'M4V'
    
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
        'W': {'mass': (0.5, 1.4), 'temp': (8000, 40000), 'lum': (0.0001, 0.01), 'abs_mag': (11.0, 16.0)},  # White dwarf
    }
    
    # Find spectral type
    spectral_type = 'M'
    stellar_class_upper = stellar_class.upper()
    for char in stellar_class_upper:
        if char in spectral_data:
            spectral_type = char
            break
    
    # Handle special cases
    if 'WD' in stellar_class_upper or stellar_class_upper.startswith('D'):
        spectral_type = 'D'
    
    data = spectral_data[spectral_type]
    
    # Generate base values
    mass = random.uniform(*data['mass'])
    temperature = int(random.uniform(*data['temp']))
    luminosity = random.uniform(*data['lum'])
    absolute_magnitude = random.uniform(*data['abs_mag'])
    
    # Apply subclass
    subclass_match = re.search(r'(\d+\.?\d*)', stellar_class)
    if subclass_match:
        try:
            subclass = float(subclass_match.group(1))
            if subclass <= 9:
                factor = (9 - subclass) / 9.0
                mass = data['mass'][0] + (data['mass'][1] - data['mass'][0]) * factor
                temperature = int(data['temp'][0] + (data['temp'][1] - data['temp'][0]) * factor)
                luminosity = data['lum'][0] * math.pow(data['lum'][1] / data['lum'][0], factor)
                absolute_magnitude = data['abs_mag'][1] + (data['abs_mag'][0] - data['abs_mag'][1]) * factor
        except:
            pass
    
    return mass, temperature, luminosity, absolute_magnitude

print("Parsing ALL 1421 stars from star-ref.csv...")

# Read file
with open('/mnt/c/users/davis/milky-way/stellar_data/star-ref.csv', 'rb') as f:
    content = f.read()
    content = content.replace(b'\r\n', b'\n').replace(b'\r', b'\n')
    text = content.decode('latin-1', errors='replace')

lines = text.split('\n')
print(f"Total lines: {len(lines)}")

stars = []
companion_mapping = {}
star_count = 0

# System tracking
current_system = None
current_system_distance = None
primary_star_name = None
system_stars = []

# Process every line
for i, line in enumerate(lines):
    if i == 0 or i == 1:  # Skip headers
        continue
    
    if not line.strip():
        continue
    
    # Split by comma - handle quotes properly
    parts = []
    current_part = []
    in_quotes = False
    
    for char in line:
        if char == '"':
            in_quotes = not in_quotes
        elif char == ',' and not in_quotes:
            parts.append(''.join(current_part).strip())
            current_part = []
        else:
            current_part.append(char)
    
    if current_part:
        parts.append(''.join(current_part).strip())
    
    # Need at least 2 fields (name might be in second field)
    if len(parts) < 2:
        continue
    
    # Extract fields
    system = clean_text(parts[0]) if parts[0] else ""
    name = clean_text(parts[1]) if parts[1] else ""
    distance_str = parts[2] if len(parts) > 2 else ""
    coord_str = parts[3] if len(parts) > 3 else ""
    stellar_class = clean_text(parts[4]) if len(parts) > 4 else ""
    
    # Handle new system
    if system:
        # Process previous system
        if system_stars and primary_star_name:
            for star in system_stars:
                if star['name'] != primary_star_name:
                    companion_mapping[star['name']] = primary_star_name
        
        # Start new system
        current_system = system
        current_system_distance = parse_distance(distance_str)
        system_stars = []
        primary_star_name = None
    
    # Skip if no name
    if not name:
        continue
    
    # Skip the "10 parsecs" marker
    if "10 parsecs" in name:
        continue
    
    # Handle companion naming
    original_name = name
    is_companion = False
    
    # If name is just a letter, it's a companion
    if name in ['A', 'B', 'C', 'D', 'Ba', 'Bb', 'Ab', 'Ca', 'Cb', 'Da', 'Db']:
        if current_system:
            name = f"{current_system} {name}"
            is_companion = (original_name != 'A')
    
    # Get distance
    if distance_str and distance_str.strip() and distance_str != ',':
        distance = parse_distance(distance_str)
    else:
        distance = current_system_distance
    
    # Skip if no distance or too far
    if distance is None:
        continue
    
    # Be more liberal with distance - include everything up to 260 ly
    # (80 parsecs converted to ly)
    if distance > 260:
        continue
    
    # Generate position
    x, y, z = generate_random_position(distance)
    
    # Infer stellar class
    stellar_class = infer_stellar_class(name, stellar_class)
    
    # Generate properties
    mass, temperature, luminosity, absolute_magnitude = generate_stellar_properties(stellar_class)
    
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
    
    stars.append(star_data)
    system_stars.append(star_data)
    star_count += 1
    
    # Track primary
    if not primary_star_name or original_name == 'A' or name.endswith(' A'):
        primary_star_name = name

# Process last system
if system_stars and primary_star_name:
    for star in system_stars:
        if star['name'] != primary_star_name:
            companion_mapping[star['name']] = primary_star_name

# Special companion mappings
for star in stars:
    if "Proxima" in star['name']:
        star['name'] = "Proxima Centauri"
        companion_mapping["Proxima Centauri"] = "Alpha Centauri A"
    elif "Rigil Kentaurus" in star['name']:
        star['name'] = "Alpha Centauri A"
    elif "Toliman" in star['name']:
        star['name'] = "Alpha Centauri B"
        companion_mapping["Alpha Centauri B"] = "Alpha Centauri A"

# Add Sol if missing
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

print(f"\nTotal stars extracted: {len(stars)}")
print(f"Companion mappings: {len(companion_mapping)}")

# Sort by distance
stars.sort(key=lambda s: math.sqrt(s["x"]**2 + s["y"]**2 + s["z"]**2))

# Limit to closest 1421 stars
if len(stars) > 1421:
    stars = stars[:1421]
    print(f"Limited to closest 1421 stars")

# Save files
output_path = '/home/davis/final_1421_stars.csv'
mapping_path = '/home/davis/final_companion_mappings.csv'

with open(output_path, 'w', newline='') as f:
    fieldnames = ['name', 'x', 'y', 'z', 'stellar_class', 'mass', 'temperature', 'luminosity', 'absolute_magnitude']
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(stars)

with open(mapping_path, 'w', newline='') as f:
    writer = csv.writer(f)
    writer.writerow(['companion_name', 'primary_star_name'])
    # Only include mappings for stars we kept
    star_names = {s['name'] for s in stars}
    for companion, primary in sorted(companion_mapping.items()):
        if companion in star_names and primary in star_names:
            writer.writerow([companion, primary])

print(f"\nCreated {output_path} with {len(stars)} stars")
print(f"Created {mapping_path}")

# Show summary
print(f"\nFirst 40 stars:")
for i, star in enumerate(stars[:40]):
    dist = math.sqrt(star['x']**2 + star['y']**2 + star['z']**2)
    comp = f" -> {companion_mapping[star['name']]}" if star['name'] in companion_mapping else ""
    print(f"{i+1:4d}. {star['name']}{comp} - {dist:.2f} ly - {star['stellar_class']}")

print(f"\nTotal unique star systems (excluding companions): ~{len(stars) - len(companion_mapping)}")
print(f"Total companions mapped: {len(companion_mapping)}")