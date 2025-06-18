#!/usr/bin/env python3
import csv
import re
import math

# First, extract all the real star names from generate_stars.py
print("Extracting known stars from generate_stars.py...")

known_stars_code = []
with open('/mnt/c/users/davis/milky-way/stellar_data/generate_stars.py', 'r') as f:
    content = f.read()
    # Extract the KNOWN_STARS list
    start = content.find('KNOWN_STARS = [')
    end = content.find(']', start) + 1
    known_stars_str = content[start:end]
    
    # Parse the entries
    import ast
    exec(known_stars_str)
    known_stars_code = KNOWN_STARS

print(f"Found {len(known_stars_code)} known stars in generate_stars.py")

# Convert RA/Dec to XYZ for matching
def ra_dec_to_xyz(ra_hours, dec_degrees, distance_ly):
    ra_radians = (ra_hours * 15.0) * math.pi / 180.0
    dec_radians = dec_degrees * math.pi / 180.0
    
    x = distance_ly * math.cos(dec_radians) * math.cos(ra_radians)
    y = distance_ly * math.cos(dec_radians) * math.sin(ra_radians)
    z = distance_ly * math.sin(dec_radians)
    
    return x, y, z

# Process known stars
known_stars_positions = []
for star in known_stars_code:
    if "ra" in star:
        x, y, z = ra_dec_to_xyz(star["ra"], star["dec"], star["dist"])
    else:
        x, y, z = star["x"], star["y"], star["z"]
    
    known_stars_positions.append({
        "name": star["name"],
        "x": x,
        "y": y,
        "z": z
    })

# Read additional stars from star-ref.csv
print("\nReading star-ref.csv for additional star names...")
ref_stars = []
with open('/mnt/c/users/davis/milky-way/stellar_data/star-ref.csv', 'r', encoding='latin-1') as f:
    reader = csv.reader(f)
    next(reader)  # Skip header
    
    for row in reader:
        if len(row) >= 3:
            name = row[1].strip()
            distance_str = row[2].strip()
            
            # Skip companion designations
            if name in ['A', 'B', 'C', 'D', 'Ba', 'Bb', 'Ab', '']:
                continue
            
            # Extract distance
            match = re.search(r'([\d.]+)', distance_str)
            if match and name:
                distance = float(match.group(1))
                # Check if parsecs (small number) or light years
                if distance < 100:
                    distance = distance * 3.26156  # Convert pc to ly
                
                if distance <= 80:  # Within our range
                    # Clean up name
                    clean_name = name.replace('�', ' ').strip()
                    clean_name = re.sub(r'\s+', ' ', clean_name)
                    
                    # Remove $ symbols and parenthetical info for main name
                    clean_name = clean_name.replace('$', '')
                    main_name = re.sub(r'\s*\([^)]*\)', '', clean_name).strip()
                    
                    if main_name and not any(ks['name'] == main_name for ks in known_stars_code):
                        ref_stars.append({
                            'name': main_name,
                            'distance': distance
                        })

print(f"Found {len(ref_stars)} additional star names from star-ref.csv")

# Read current stars.csv
print("\nReading current stars.csv...")
stars = []
with open('/mnt/c/users/davis/milky-way/stellar_data/stars.csv', 'r') as f:
    reader = csv.DictReader(f)
    for row in reader:
        stars.append(row)

print(f"Total stars in stars.csv: {len(stars)}")

# Match stars
fixed_count = 0
remaining_star_numbers = []

for star in stars:
    if star['name'].startswith('Star-'):
        x, y, z = float(star['x']), float(star['y']), float(star['z'])
        
        # First try to match with known stars by position
        matched = False
        for ks in known_stars_positions:
            dx = x - ks['x']
            dy = y - ks['y']
            dz = z - ks['z']
            dist = math.sqrt(dx*dx + dy*dy + dz*dz)
            
            if dist < 0.001:  # Very close match
                star['name'] = ks['name']
                fixed_count += 1
                matched = True
                print(f"Matched {star['name']} by position")
                break
        
        if not matched:
            star_num = star['name'].replace('Star-', '')
            remaining_star_numbers.append(int(star_num))

print(f"\nFixed {fixed_count} star names from known positions")
print(f"Remaining unnamed: {len(remaining_star_numbers)}")

# For remaining stars, use ref_stars names in order of distance
if remaining_star_numbers and ref_stars:
    # Sort ref_stars by distance
    ref_stars.sort(key=lambda s: s['distance'])
    
    # Sort unnamed stars by distance
    unnamed_stars = [(i, star) for i, star in enumerate(stars) if star['name'].startswith('Star-')]
    unnamed_stars.sort(key=lambda item: math.sqrt(
        float(item[1]['x'])**2 + float(item[1]['y'])**2 + float(item[1]['z'])**2
    ))
    
    # Assign names
    for i, (idx, star) in enumerate(unnamed_stars):
        if i < len(ref_stars):
            stars[idx]['name'] = ref_stars[i]['name']
            fixed_count += 1
            print(f"Assigned {ref_stars[i]['name']} to star at distance {math.sqrt(float(star['x'])**2 + float(star['y'])**2 + float(star['z'])**2):.2f} ly")

print(f"\nTotal fixed: {fixed_count} star names")

# Write updated stars.csv
with open('/mnt/c/users/davis/milky-way/stellar_data/stars.csv', 'w', newline='') as f:
    fieldnames = ['name', 'x', 'y', 'z', 'stellar_class', 'mass', 'temperature', 'luminosity', 'absolute_magnitude']
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(stars)

print("\nUpdated stars.csv written successfully")

# Count remaining Star-XXXX entries
star_count = sum(1 for s in stars if s['name'].startswith('Star-'))
print(f"Remaining generic Star-XXXX entries: {star_count}")