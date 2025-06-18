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
    text = text.replace('°', ' degrees ')
    text = text.replace('±', ' +/- ')
    text = text.replace('–', '-')
    text = text.replace('—', '-')
    text = text.replace('$', '')
    text = text.replace('&', '')
    text = text.replace('"', '')
    text = re.sub(r'\s+', ' ', text)
    return text.strip()

def parse_distance(distance_str):
    """Extract distance in light years"""
    if not distance_str or distance_str == 'N/A':
        return None
    
    # Extract all numbers
    numbers = re.findall(r'([\d.]+)', distance_str)
    if not numbers:
        return None
    
    distance = float(numbers[0])
    
    # Check if we need to convert parsecs to light years
    # If the string mentions "parsecs" or "pc", or if the number is small
    if 'parsec' in distance_str.lower() or 'pc' in distance_str.lower() or distance < 50:
        distance = distance * 3.26156
    
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
    if stellar_class and stellar_class.strip():
        return stellar_class
    
    # Well-known stars and their types
    known_stars = {
        'Sol': 'G2V',
        'Proxima Centauri': 'M5.5Ve',
        'Alpha Centauri A': 'G2V',
        'Alpha Centauri B': 'K1V',
        'Barnard': 'M4Ve',
        'Wolf 359': 'M6.5Ve',
        'Lalande 21185': 'M2V',
        'Sirius A': 'A1V',
        'Sirius B': 'DA2',
        'Luyten 726-8 A': 'M5.5Ve',
        'Luyten 726-8 B': 'M6Ve',
        'Ross 154': 'M3.5Ve',
        'Ross 248': 'M5.5Ve',
        'Epsilon Eridani': 'K2V',
        'Lacaille 9352': 'M0.5V',
        'Ross 128': 'M4V',
        'Procyon A': 'F5IV-V',
        'Procyon B': 'DQZ',
        'Tau Ceti': 'G8V',
        'Vega': 'A0V',
        'Arcturus': 'K0III',
        'Pollux': 'K0III',
        'Altair': 'A7V',
        'Deneb': 'A2Ia',
        'Betelgeuse': 'M2Iab',
        'Rigel': 'B8Ia',
        'Capella': 'G8III',
        'Aldebaran': 'K5III',
        'Spica': 'B1V',
        'Antares': 'M1.5Iab',
        'Fomalhaut': 'A3V',
        'Denebola': 'A3V',
        'Regulus': 'B8IVn',
        'Adhara': 'B2II',
        'Castor': 'A1V',
        'Gacrux': 'M3.5III',
        'Bellatrix': 'B2III',
        'Miaplacidus': 'A1III',
        'Alnilam': 'B0Ia',
        'Alnair': 'B7IV',
        'Alioth': 'A1III-IVp',
        'Alnitak': 'O9.5Ib',
        'Dubhe': 'K0III',
        'Mirfak': 'F5Ib',
        'Wezen': 'F8Ia',
        'Sargas': 'F0II',
        'Kaus Australis': 'B9.5III',
        'Avior': 'K3III',
        'Alkaid': 'B3V',
        'Menkalinan': 'A1IV',
        'Atria': 'K2Ib',
        'Alhena': 'A1.5IV',
        'Peacock': 'B2IV',
        'Alsephina': 'B2IV',
        'Mirzam': 'B1II-III',
        'Alphard': 'K3II-III',
        'Polaris': 'F7Ib',
        'Hamal': 'K2III',
        'Diphda': 'K0III',
        'Mizar': 'A2V',
        'Nunki': 'B2.5V',
        'Menkent': 'K0III',
        'Mirach': 'M0III',
        'Alpheratz': 'B9p',
        'Rasalhague': 'A5III',
        'Kochab': 'K4III',
        'Saiph': 'B0.5Ia',
        'Deneb Algedi': 'Am',
        'Aspidiske': 'A9Ib',
        'Alsuhail': 'K4Ib',
        'Alphecca': 'A0V',
        'Mintaka': 'O9.5II',
        'Sadr': 'F8Ib',
        'Eltanin': 'K5III',
        'Schedar': 'K0II-III',
        'Naos': 'O5Ia',
        'Almach': 'K3II',
        'Caph': 'F2III',
        'Izar': 'K0II-III',
        'Dschubba': 'B0.3IV',
        'Larawag': 'M1.5III',
        'Merak': 'A1V',
        'Ankaa': 'K0III',
        'Gienah': 'B8III',
        'Phecda': 'A0Ve',
        'Aludra': 'B5Ia',
        'Markeb': 'B2III',
        'Aljanah': 'F0IV',
        'Acrab': 'B0.5IV-V',
    }
    
    # Check if the name contains any known star
    name_clean = clean_text(name)
    for known_name, known_class in known_stars.items():
        if known_name.lower() in name_clean.lower():
            return known_class
    
    # Default to M dwarf for unnamed/unknown
    return 'M5V'

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

print("Extracting ALL stars from star-ref.csv...")

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
primary_star_name = None
system_stars = []

# Process every line thoroughly
for i, line in enumerate(lines):
    if i == 0:  # Skip header
        continue
    
    if not line.strip():
        continue
    
    # Parse CSV manually to handle quotes
    parts = []
    current_part = []
    in_quotes = False
    
    for char in line:
        if char == '"' and (not current_part or current_part[-1] != '\\'):
            in_quotes = not in_quotes
        elif char == ',' and not in_quotes:
            parts.append(''.join(current_part).strip())
            current_part = []
        else:
            current_part.append(char)
    
    if current_part:
        parts.append(''.join(current_part).strip())
    
    # Need at least 3 fields (system, name, distance)
    if len(parts) < 3:
        continue
    
    # Extract fields
    system = clean_text(parts[0]) if parts[0] else ""
    name = clean_text(parts[1]) if parts[1] else ""
    distance_str = parts[2] if len(parts) > 2 else ""
    coord_str = parts[3] if len(parts) > 3 else ""
    stellar_class = clean_text(parts[4]) if len(parts) > 4 else ""
    
    # If system field has a value, it's a new system
    if system:
        # Process previous system if exists
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
    
    # Handle companion naming
    original_name = name
    is_companion = False
    
    # If name is just a letter, it's a companion
    if name in ['A', 'B', 'C', 'D', 'Ba', 'Bb', 'Ab', 'Ca', 'Cb', 'Da', 'Db']:
        if current_system:
            name = f"{current_system} {name}"
            is_companion = (original_name != 'A')
    
    # Get distance for this star
    if distance_str and distance_str.strip() and distance_str != ',':
        distance = parse_distance(distance_str)
    else:
        # Use system distance
        distance = current_system_distance
    
    if distance is None:
        # Try to extract from later fields if distance field is empty
        for part in parts[2:]:
            dist = parse_distance(part)
            if dist:
                distance = dist
                break
    
    if distance is None or distance > 80:
        continue
    
    # Generate position
    x, y, z = generate_random_position(distance)
    
    # Infer stellar class if missing
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
    
    # Track primary star
    if not primary_star_name or original_name == 'A' or name.endswith(' A'):
        primary_star_name = name

# Process last system
if system_stars and primary_star_name:
    for star in system_stars:
        if star['name'] != primary_star_name:
            companion_mapping[star['name']] = primary_star_name

# Special companion cases
special_mappings = {
    "Proxima Centauri": "Alpha Centauri A",
    "Proxima Centauri (C, V645 Centauri)": "Alpha Centauri A",
    "Rigil Kentaurus (A)": "Alpha Centauri A",
    "Toliman (B)": "Alpha Centauri A",
    "Alpha Centauri B": "Alpha Centauri A",
    "Alpha Centauri C": "Alpha Centauri A",
}

# Apply special mappings and fix names
for star in stars:
    # Fix Alpha Centauri naming
    if "Rigil Kentaurus (A)" in star['name']:
        star['name'] = "Alpha Centauri A"
    elif "Toliman (B)" in star['name']:
        star['name'] = "Alpha Centauri B"
    elif "Proxima" in star['name'] and "Centauri" in star['name']:
        star['name'] = "Proxima Centauri"
    
    # Apply companion mappings
    for comp_name, primary_name in special_mappings.items():
        if star['name'] == comp_name and comp_name != primary_name:
            companion_mapping[comp_name] = primary_name

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

# Save to home directory first
output_path = '/home/davis/all_stars.csv'
mapping_path = '/home/davis/all_companions.csv'

# Write stars
with open(output_path, 'w', newline='') as f:
    fieldnames = ['name', 'x', 'y', 'z', 'stellar_class', 'mass', 'temperature', 'luminosity', 'absolute_magnitude']
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(stars)

# Write companion mappings
with open(mapping_path, 'w', newline='') as f:
    writer = csv.writer(f)
    writer.writerow(['companion_name', 'primary_star_name'])
    for companion, primary in sorted(companion_mapping.items()):
        writer.writerow([companion, primary])

print(f"\nCreated {output_path} with {len(stars)} stars")
print(f"Created {mapping_path} with {len(companion_mapping)} mappings")

# Show stats
print("\nFirst 30 stars:")
for star in stars[:30]:
    dist = math.sqrt(star['x']**2 + star['y']**2 + star['z']**2)
    comp = f" -> {companion_mapping[star['name']]}" if star['name'] in companion_mapping else ""
    print(f"  {star['name']}{comp} - {dist:.2f} ly - {star['stellar_class']}")

print(f"\n10 sample companion mappings:")
for i, (comp, primary) in enumerate(sorted(companion_mapping.items())[:10]):
    print(f"  {comp} -> {primary}")