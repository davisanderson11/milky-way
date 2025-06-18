#!/usr/bin/env python3
import csv
import re
import math
import random

def clean_text(text):
    """Clean up text with encoding issues"""
    if not text:
        return text
    
    # Replace problematic character sequences
    replacements = {
        '\u00ca': ' ',
        'Ê': ' ',
        '\u00a0': ' ',  # Non-breaking space
        '\u00a1': '°',  # Degree symbol
        '¡': '°',
        '\u00b1': '±',  # Plus-minus
        '±': '±',
        '\u2013': '-',  # En dash
        '\u2014': '-',  # Em dash
        '\u2212': '-',  # Minus sign
        'M-J': ' ',
        'M-!': '°',
        'M-P': '-',
        'M-$': '',
        'M-`': '',
        'M-1': '±',
        'M-^': '',
        '$': '',
        '\ufffd': '?',
        '  ': ' ',
    }
    
    for old, new in replacements.items():
        text = text.replace(old, new)
    
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

def parse_coordinates(coord_str):
    """Parse RA/Dec from coordinate string"""
    if not coord_str or coord_str == 'N/A':
        return None, None
    
    # Clean the string
    coord_str = clean_text(coord_str)
    
    # Try to extract RA and Dec
    ra_match = re.search(r'(\d+)h\s*(\d+)m\s*([\d.]+)s', coord_str)
    dec_match = re.search(r'([+-]?\d+)°\s*(\d+)[′\']\s*([\d.]+)', coord_str)
    
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

def ra_dec_to_xyz(ra_hours, dec_degrees, distance_ly):
    """Convert RA/Dec/Distance to Cartesian coordinates"""
    ra_radians = (ra_hours * 15.0) * math.pi / 180.0
    dec_radians = dec_degrees * math.pi / 180.0
    
    x = distance_ly * math.cos(dec_radians) * math.cos(ra_radians)
    y = distance_ly * math.cos(dec_radians) * math.sin(ra_radians)
    z = distance_ly * math.sin(dec_radians)
    
    return x, y, z

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

print("Parsing ALL stars from star-ref.csv...")

# Read file with proper encoding
with open('/mnt/c/users/davis/milky-way/stellar_data/star-ref.csv', 'rb') as f:
    content = f.read()
    # Replace various line endings
    content = content.replace(b'\r\n', b'\n').replace(b'\r', b'\n')
    text = content.decode('latin-1', errors='replace')

lines = text.split('\n')
print(f"Total lines: {len(lines)}")

stars = []
companion_mapping = {}
current_system = None
primary_star_name = None
all_star_names = []  # Track all names for debugging

# First pass - collect all star entries
for i, line in enumerate(lines):
    if i == 0 or not line.strip():
        continue
    
    # Parse CSV
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
    
    if len(parts) < 3:
        continue
    
    # Extract fields
    system = clean_text(parts[0])
    name = clean_text(parts[1])
    distance_str = parts[2]
    coord_str = parts[3] if len(parts) > 3 else ""
    stellar_class = clean_text(parts[4]) if len(parts) > 4 else "M5V"
    
    # Skip empty names
    if not name:
        continue
    
    # Update system tracking
    if system:
        current_system = system
        primary_star_name = None
    
    # Check if this is a companion designation
    is_companion = False
    companion_letter = None
    
    # Check for simple companion letters
    if name in ['A', 'B', 'C', 'D', 'Ba', 'Bb', 'Ab', 'Bc', 'Bd', 'Ca', 'Cb', 'Da', 'Db']:
        is_companion = True
        companion_letter = name
        # Use system name + letter
        if current_system:
            name = f"{current_system} {companion_letter}"
    
    # Check for names ending with companion designation
    for suffix in [' A', ' B', ' C', ' D', ' Ba', ' Bb', ' Ab']:
        if name.endswith(suffix):
            companion_letter = suffix.strip()
            # If it's not the A star, it's a companion
            if companion_letter != 'A':
                is_companion = True
            break
    
    # Special handling for specific systems
    if 'Proxima' in name and 'Centauri' in name:
        is_companion = True
        companion_mapping[name] = "Alpha Centauri A"
    elif name == "Toliman (B)":
        is_companion = True
        name = "Alpha Centauri B"
        companion_mapping[name] = "Alpha Centauri A"
    elif current_system == "Alpha Centauri" and companion_letter == "B":
        is_companion = True
        companion_mapping[name] = "Alpha Centauri A"
    
    # Parse distance
    distance = parse_distance(distance_str)
    if distance is None or distance > 80:
        continue
    
    # Track primary stars
    if not is_companion or companion_letter == 'A':
        primary_star_name = name
    elif is_companion and primary_star_name and name != primary_star_name:
        companion_mapping[name] = primary_star_name
    
    # Parse coordinates if available
    ra, dec = parse_coordinates(coord_str)
    
    # Generate position
    if ra is not None and dec is not None:
        x, y, z = ra_dec_to_xyz(ra, dec, distance)
    else:
        # Random position at given distance
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
        "stellar_class": stellar_class,
        "mass": round(mass, 3),
        "temperature": temperature,
        "luminosity": round(luminosity, 6),
        "absolute_magnitude": round(absolute_magnitude, 2)
    }
    
    stars.append(star_data)
    all_star_names.append(name)

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

# If we don't have 1400 stars, something's wrong
if len(stars) < 1400:
    print(f"\nWARNING: Only found {len(stars)} stars, expected ~1400")
    print("This suggests the parsing needs adjustment.")

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

# Show statistics
print("\nStatistics:")
print(f"- Total stars: {len(stars)}")
print(f"- Companion mappings: {len(companion_mapping)}")

# Show sample stars
print("\nFirst 20 stars:")
for i, star in enumerate(stars[:20]):
    dist = math.sqrt(star['x']**2 + star['y']**2 + star['z']**2)
    comp = " -> " + companion_mapping[star['name']] if star['name'] in companion_mapping else ""
    print(f"  {star['name']}{comp} - {dist:.2f} ly")

# Show companion mappings
if len(companion_mapping) > 0:
    print(f"\nFirst 20 companion mappings (of {len(companion_mapping)} total):")
    for i, (comp, primary) in enumerate(sorted(companion_mapping.items())[:20]):
        print(f"  {comp} -> {primary}")