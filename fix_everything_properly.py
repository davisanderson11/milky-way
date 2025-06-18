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
    replacements = {
        '�': ' ',
        '°': '',
        '±': '+/-',
        '–': '-',
        '—': '-',
        '$': '',
        '&': '',
        '"': '',
        '  ': ' ',
    }
    
    for old, new in replacements.items():
        text = text.replace(old, new)
    
    text = re.sub(r'\s+', ' ', text)
    return text.strip()

def parse_distance(distance_str):
    """Extract distance in light years - CORRECTLY"""
    if not distance_str or distance_str == 'N/A' or distance_str == '':
        return None
    
    # Clean the string
    distance_str = clean_text(distance_str)
    
    # Extract first number
    match = re.search(r'([\d.]+)', distance_str)
    if not match:
        return None
    
    distance = float(match.group(1))
    
    # The star-ref.csv file lists distances in PARSECS for entries with format "X.XX ± Y.YY"
    # EXCEPT for very small values which are already in light years (like Sol at 0.0000158)
    
    if distance < 0.001:  # Very small values like Sol (0.0000158) are in light years
        return distance
    elif distance < 25:  # Values under 25 are definitely parsecs (nearest star is 4.24 ly = 1.3 pc)
        return distance * 3.26156  # Convert parsecs to light years
    else:
        # For larger values, check context
        # If it's in the format "XX.X ± Y.Y", it's likely parsecs
        if '±' in distance_str or '+/-' in distance_str:
            return distance * 3.26156  # Convert parsecs to light years
        else:
            # Otherwise assume light years
            return distance

def generate_random_position(distance):
    """Generate random position at given distance"""
    theta = random.uniform(0, 2 * math.pi)
    phi = math.acos(1 - 2 * random.random())
    
    x = distance * math.sin(phi) * math.cos(theta)
    y = distance * math.sin(phi) * math.sin(theta)
    z = distance * math.cos(phi)
    
    return x, y, z

def generate_stellar_properties(stellar_class):
    """Generate realistic stellar properties based on spectral class"""
    if not stellar_class or stellar_class == '?' or stellar_class == '':
        stellar_class = 'M5V'
    
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
        'W': {'mass': (0.5, 1.4), 'temp': (8000, 40000), 'lum': (0.0001, 0.01), 'abs_mag': (11.0, 16.0)},
    }
    
    # Find spectral type
    spectral_type = 'M'
    stellar_class_upper = stellar_class.upper()
    
    # Handle special prefixes for evolved stars
    if stellar_class_upper.startswith('SD'):  # Subdwarf
        stellar_class_upper = stellar_class_upper[2:]
    elif stellar_class_upper.startswith('WD') or stellar_class_upper.startswith('D'):  # White dwarf
        spectral_type = 'D'
    
    for char in stellar_class_upper:
        if char in spectral_data:
            spectral_type = char
            break
    
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

print("Parsing star catalog with CORRECT distances and stellar types...")

# Read file
with open('/mnt/c/users/davis/milky-way/stellar_data/star-ref.csv', 'rb') as f:
    content = f.read()
    content = content.replace(b'\r\n', b'\n').replace(b'\r', b'\n')
    text = content.decode('latin-1', errors='replace')

lines = text.split('\n')
print(f"Total lines: {len(lines)}")

stars = []
companion_mapping = {}

# System tracking
current_system = None
current_system_distance = None
current_system_class = None
primary_star_name = None
system_stars = []

# Process every line
for i, line in enumerate(lines):
    if i == 0 or i == 1:  # Skip headers
        continue
    
    if not line.strip():
        continue
    
    # Split by comma - handle quotes
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
    
    # Need at least 2 fields
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
        current_system_class = stellar_class
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
    
    # Get distance for this entry
    if distance_str and distance_str.strip() and distance_str != ',':
        distance = parse_distance(distance_str)
    else:
        distance = current_system_distance
    
    # Skip if no distance or too far (80 ly = ~24.5 parsecs)
    if distance is None or distance > 80:
        continue
    
    # Get stellar class
    if not stellar_class and current_system_class:
        stellar_class = current_system_class
    
    # Generate position
    x, y, z = generate_random_position(distance)
    
    # Generate properties based on actual stellar class
    mass, temperature, luminosity, absolute_magnitude = generate_stellar_properties(stellar_class)
    
    star_data = {
        "name": name,
        "x": round(x, 6),
        "y": round(y, 6),
        "z": round(z, 6),
        "stellar_class": stellar_class if stellar_class else "M5V",
        "mass": round(mass, 3),
        "temperature": temperature,
        "luminosity": round(luminosity, 6),
        "absolute_magnitude": round(absolute_magnitude, 2)
    }
    
    stars.append(star_data)
    system_stars.append(star_data)
    
    # Track primary
    if not primary_star_name or original_name == 'A' or name.endswith(' A'):
        primary_star_name = name

# Process last system
if system_stars and primary_star_name:
    for star in system_stars:
        if star['name'] != primary_star_name:
            companion_mapping[star['name']] = primary_star_name

# Fix special cases
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

# Save files
output_path = '/home/davis/correct_stars.csv'
mapping_path = '/home/davis/correct_companions.csv'

with open(output_path, 'w', newline='') as f:
    fieldnames = ['name', 'x', 'y', 'z', 'stellar_class', 'mass', 'temperature', 'luminosity', 'absolute_magnitude']
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(stars)

with open(mapping_path, 'w', newline='') as f:
    writer = csv.writer(f)
    writer.writerow(['companion_name', 'primary_star_name'])
    for companion, primary in sorted(companion_mapping.items()):
        writer.writerow([companion, primary])

print(f"\nCreated {output_path} with {len(stars)} stars")
print(f"Created {mapping_path}")

# Show first 20 stars with correct distances
print(f"\nFirst 20 stars with CORRECT distances:")
for i, star in enumerate(stars[:20]):
    dist = math.sqrt(star['x']**2 + star['y']**2 + star['z']**2)
    comp = f" -> {companion_mapping[star['name']]}" if star['name'] in companion_mapping else ""
    print(f"{i+1:3d}. {star['name']}{comp} - {dist:.2f} ly - {star['stellar_class']}")

# Try to copy to stellar_data if possible
try:
    import shutil
    shutil.copy(output_path, '/mnt/c/users/davis/milky-way/stellar_data/stars.csv')
    shutil.copy(mapping_path, '/mnt/c/users/davis/milky-way/stellar_data/companion_mapping.csv')
    print("\n✓ Successfully updated stellar_data/stars.csv!")
except Exception as e:
    print(f"\n✗ Could not update stellar_data/stars.csv: {e}")
    print("  Files saved to /home/davis/ instead")