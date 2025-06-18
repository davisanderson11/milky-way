#!/usr/bin/env python3
import csv
import re
import math
import random

def clean_text(text):
    """Clean text encoding issues"""
    if not text:
        return text
    
    # Common replacements
    text = text.replace('�', ' ')
    text = text.replace('°', ' degrees ')
    text = text.replace('±', ' +/- ')
    text = text.replace('–', '-')
    text = text.replace('—', '-')
    text = text.replace('$', '')
    text = text.replace('"', '')
    text = re.sub(r'\s+', ' ', text)
    return text.strip()

def parse_distance(distance_str):
    """Extract distance in light years"""
    if not distance_str or distance_str == 'N/A':
        return None
    
    # Extract first number
    match = re.search(r'([\d.]+)', distance_str)
    if not match:
        return None
    
    distance = float(match.group(1))
    
    # The file specifies distances in parsecs for values < 50
    if distance < 50:
        distance = distance * 3.26156  # Convert to light years
    
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
    if not stellar_class:
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
    }
    
    # Find spectral type
    spectral_type = 'M'
    for char in stellar_class.upper():
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
        subclass = float(subclass_match.group(1))
        if subclass <= 9:
            factor = (9 - subclass) / 9.0
            mass = data['mass'][0] + (data['mass'][1] - data['mass'][0]) * factor
            temperature = int(data['temp'][0] + (data['temp'][1] - data['temp'][0]) * factor)
            luminosity = data['lum'][0] * math.pow(data['lum'][1] / data['lum'][0], factor)
            absolute_magnitude = data['abs_mag'][1] + (data['abs_mag'][0] - data['abs_mag'][1]) * factor
    
    return mass, temperature, luminosity, absolute_magnitude

print("Parsing complete star catalog from star-ref.csv...")

# Read and parse the file
with open('/mnt/c/users/davis/milky-way/stellar_data/star-ref.csv', 'rb') as f:
    content = f.read()
    # Handle line endings
    content = content.replace(b'\r\n', b'\n').replace(b'\r', b'\n')
    text = content.decode('latin-1', errors='replace')

lines = text.split('\n')
print(f"Total lines: {len(lines)}")

stars = []
companion_mapping = {}

# Track system information
current_system = None
current_system_distance = None
primary_star_name = None
system_stars = []

# Process each line
for i, line in enumerate(lines):
    if i == 0:  # Skip header
        continue
    
    if not line.strip():
        continue
    
    # Split by comma (simple split since we'll handle quotes manually if needed)
    parts = line.split(',')
    if len(parts) < 3:
        continue
    
    # Extract fields
    system = clean_text(parts[0])
    name = clean_text(parts[1])
    distance_str = parts[2]
    stellar_class = clean_text(parts[4]) if len(parts) > 4 else "M5V"
    
    # Handle system changes
    if system and system != current_system:
        # Save previous system's stars if any
        if system_stars and primary_star_name:
            # Map all non-primary stars to primary
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
    
    # Handle companion naming
    original_name = name
    is_companion = False
    
    # Check if it's just a letter (companion designation)
    if name in ['A', 'B', 'C', 'D', 'Ba', 'Bb', 'Ab', 'Ca', 'Cb', 'Da', 'Db']:
        if current_system:
            name = f"{current_system} {name}"
            is_companion = (original_name != 'A')  # A is usually primary
    
    # Get distance
    if distance_str and distance_str.strip():
        distance = parse_distance(distance_str)
    else:
        distance = current_system_distance
    
    if distance is None or distance > 80:
        continue
    
    # Generate position
    x, y, z = generate_random_position(distance)
    
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
    
    # Track primary star (first star in system or one ending with A)
    if not primary_star_name or original_name == 'A' or name.endswith(' A'):
        primary_star_name = name

# Handle last system
if system_stars and primary_star_name:
    for star in system_stars:
        if star['name'] != primary_star_name:
            companion_mapping[star['name']] = primary_star_name

# Special handling for known companions
special_companions = {
    "Proxima Centauri": "Alpha Centauri A",
    "Proxima Centauri (C, V645 Centauri)": "Alpha Centauri A",
    "Rigil Kentaurus (A)": "Alpha Centauri A",  # This IS Alpha Centauri A
    "Toliman (B)": "Alpha Centauri A",  # This is Alpha Centauri B
}

# Apply special companion mappings
for star in stars:
    name = star['name']
    if name in special_companions and name != special_companions[name]:
        companion_mapping[name] = special_companions[name]

# Rename some stars for clarity
for star in stars:
    if star['name'] == "Rigil Kentaurus (A)":
        star['name'] = "Alpha Centauri A"
    elif star['name'] == "Toliman (B)":
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

print(f"\nTotal stars parsed: {len(stars)}")
print(f"Companion mappings: {len(companion_mapping)}")

# Sort by distance
stars.sort(key=lambda s: math.sqrt(s["x"]**2 + s["y"]**2 + s["z"]**2))

# Output results
output_path = '/home/davis/stars_output.csv'
mapping_path = '/home/davis/companion_mapping_output.csv'

# Write stars.csv
with open(output_path, 'w', newline='') as f:
    fieldnames = ['name', 'x', 'y', 'z', 'stellar_class', 'mass', 'temperature', 'luminosity', 'absolute_magnitude']
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(stars)

print(f"\nCreated {output_path} with {len(stars)} stars")

# Write companion mapping
with open(mapping_path, 'w', newline='') as f:
    writer = csv.writer(f)
    writer.writerow(['companion_name', 'primary_star_name'])
    for companion, primary in sorted(companion_mapping.items()):
        writer.writerow([companion, primary])

print(f"Created {mapping_path} with {len(companion_mapping)} mappings")

# Show sample results
print("\nFirst 30 stars:")
for i, star in enumerate(stars[:30]):
    dist = math.sqrt(star['x']**2 + star['y']**2 + star['z']**2)
    comp = " -> " + companion_mapping[star['name']] if star['name'] in companion_mapping else ""
    print(f"  {star['name']}{comp} - {dist:.2f} ly")

print(f"\nSample companion mappings:")
for i, (comp, primary) in enumerate(sorted(companion_mapping.items())[:20]):
    print(f"  {comp} -> {primary}")

# Now copy the files to the proper location
print("\nCopying files to stellar_data directory...")
import shutil
shutil.copy(output_path, '/mnt/c/users/davis/milky-way/stellar_data/stars.csv')
shutil.copy(mapping_path, '/mnt/c/users/davis/milky-way/stellar_data/companion_mapping.csv')
print("Files copied successfully!")