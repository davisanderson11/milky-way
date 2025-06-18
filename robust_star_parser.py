#!/usr/bin/env python3
import csv
import re
import math
import random

def clean_text(text):
    """Clean up text with encoding issues"""
    if not text:
        return text
    
    # Replace various problematic characters
    replacements = {
        'M-J': ' ',  # Space replacement
        'M-!': '°',  # Degree symbol
        'M-P': '-',  # Minus sign
        'M-$': ' ',  # Space or special char
        'M-`': '',   # Remove
        'M-1': '±',  # Plus-minus
        'M-^': '',   # Remove
        '  ': ' ',   # Double space
    }
    
    for old, new in replacements.items():
        text = text.replace(old, new)
    
    # Remove dollar signs and clean up
    text = text.replace('$', '')
    text = re.sub(r'\s+', ' ', text)
    return text.strip()

def parse_distance(distance_str):
    """Extract distance in light years"""
    if not distance_str or distance_str == 'N/A':
        return None
    
    # Clean the string
    distance_str = clean_text(distance_str)
    
    # Extract first number
    match = re.search(r'([\d.]+)', distance_str)
    if not match:
        return None
    
    distance = float(match.group(1))
    
    # Convert parsecs to light years if needed
    if distance < 100:  # Likely parsecs
        distance = distance * 3.26156
    
    return distance

def generate_stellar_properties(stellar_class):
    """Generate realistic stellar properties"""
    stellar_class = stellar_class.strip() if stellar_class else 'M5V'
    
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
    
    # Extract spectral type
    spectral_type = 'M'
    for char in stellar_class:
        if char.upper() in spectral_data:
            spectral_type = char.upper()
            break
    
    data = spectral_data[spectral_type]
    
    # Generate values
    mass = random.uniform(*data['mass'])
    temperature = int(random.uniform(*data['temp']))
    luminosity = random.uniform(*data['lum'])
    absolute_magnitude = random.uniform(*data['abs_mag'])
    
    # Apply subclass if present
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

print("Reading star-ref.csv with robust parsing...")

# Read file as binary and handle encoding
with open('/mnt/c/users/davis/milky-way/stellar_data/star-ref.csv', 'rb') as f:
    content = f.read()
    # Replace carriage returns with newlines
    content = content.replace(b'\r', b'\n')
    # Decode with error handling
    text = content.decode('latin-1', errors='replace')

lines = text.split('\n')
print(f"Total lines: {len(lines)}")

stars = []
companion_mapping = {}
current_system = None
primary_star_name = None

# Process lines
for i, line in enumerate(lines):
    if i == 0 or not line.strip():  # Skip header and empty lines
        continue
    
    # Parse CSV carefully
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
    
    if len(parts) < 3:  # Need at least system, name, distance
        continue
    
    # Extract fields
    system = clean_text(parts[0])
    name = clean_text(parts[1])
    distance_str = parts[2]
    stellar_class = parts[4] if len(parts) > 4 else "M5V"
    
    # Skip empty names
    if not name:
        continue
    
    # Update system tracking
    if system:
        current_system = system
        primary_star_name = None
    
    # Check if companion
    is_companion = False
    
    # Simple companion letters
    if name in ['A', 'B', 'C', 'D', 'Ba', 'Bb', 'Ab', 'Bc', 'Bd']:
        is_companion = True
        if current_system:
            name = f"{current_system} {name}"
    
    # Check for companion suffixes
    elif ' B' in name or ' C' in name or ' D' in name:
        is_companion = True
    
    # Special cases
    if 'Proxima' in name and 'Centauri' in name:
        is_companion = True
        companion_mapping[name] = "Alpha Centauri A"
    
    # Parse distance
    distance = parse_distance(distance_str)
    if distance is None or distance > 80:
        continue
    
    # Generate random position at given distance
    theta = random.uniform(0, 2 * math.pi)
    phi = math.acos(1 - 2 * random.random())
    x = distance * math.sin(phi) * math.cos(theta)
    y = distance * math.sin(phi) * math.sin(theta)
    z = distance * math.cos(phi)
    
    # Generate properties
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
    
    # Track companions
    if not is_companion:
        primary_star_name = name
    elif is_companion and primary_star_name and name != primary_star_name:
        companion_mapping[name] = primary_star_name
    
    stars.append(star_data)
    
    if len(stars) % 100 == 0:
        print(f"  Processed {len(stars)} stars...")

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

print(f"\nParsed {len(stars)} stars from star-ref.csv")
print(f"Companion mappings: {len(companion_mapping)}")

# Add procedural stars to reach 1400
if len(stars) < 1400:
    print(f"\nGenerating {1400 - len(stars)} additional procedural stars...")
    
    for i in range(len(stars), 1400):
        distance = random.random() ** 0.33 * 80
        theta = random.uniform(0, 2 * math.pi)
        phi = math.acos(1 - 2 * random.random())
        
        x = distance * math.sin(phi) * math.cos(theta)
        y = distance * math.sin(phi) * math.sin(theta)
        z = distance * math.cos(phi)
        
        # Weighted spectral class
        spectral_weights = [('M', 0.765), ('K', 0.121), ('G', 0.076), ('F', 0.030), ('A', 0.006), ('D', 0.001), ('B', 0.001)]
        spectral_type = random.choices([s[0] for s in spectral_weights], weights=[s[1] for s in spectral_weights])[0]
        subclass = random.randint(0, 9)
        stellar_class = f"{spectral_type}{subclass}V" if spectral_type != 'D' else f"D{subclass}"
        
        mass, temperature, luminosity, absolute_magnitude = generate_stellar_properties(stellar_class)
        
        stars.append({
            "name": f"Star-{i:04d}",
            "x": round(x, 6),
            "y": round(y, 6),
            "z": round(z, 6),
            "stellar_class": stellar_class,
            "mass": round(mass, 3),
            "temperature": temperature,
            "luminosity": round(luminosity, 6),
            "absolute_magnitude": round(absolute_magnitude, 2)
        })

# Sort by distance
stars.sort(key=lambda s: math.sqrt(s["x"]**2 + s["y"]**2 + s["z"]**2))

# Write stars.csv
with open('/mnt/c/users/davis/milky-way/stellar_data/stars.csv', 'w', newline='') as f:
    fieldnames = ['name', 'x', 'y', 'z', 'stellar_class', 'mass', 'temperature', 'luminosity', 'absolute_magnitude']
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(stars)

print(f"\nCreated stars.csv with {len(stars)} stars")

# Write companion mapping
with open('/mnt/c/users/davis/milky-way/stellar_data/companion_mapping.csv', 'w', newline='') as f:
    writer = csv.writer(f)
    writer.writerow(['companion_name', 'primary_star_name'])
    for companion, primary in sorted(companion_mapping.items()):
        writer.writerow([companion, primary])

print(f"Created companion_mapping.csv with {len(companion_mapping)} mappings")

# Print sample stars
print("\nSample of parsed stars:")
for i, star in enumerate(stars[:20]):
    if not star['name'].startswith('Star-'):
        dist = math.sqrt(star['x']**2 + star['y']**2 + star['z']**2)
        comp = " (companion)" if star['name'] in companion_mapping else ""
        print(f"  {star['name']}{comp} - {dist:.2f} ly - {star['stellar_class']}")

# Print companion examples
if companion_mapping:
    print("\nSample companion mappings:")
    for comp, primary in list(companion_mapping.items())[:10]:
        print(f"  {comp} -> {primary}")