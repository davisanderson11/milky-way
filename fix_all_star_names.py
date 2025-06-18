#!/usr/bin/env python3
import csv
import re
import math

# First, let's understand the star-ref.csv structure better
print("Analyzing star-ref.csv structure...")
with open('/mnt/c/users/davis/milky-way/stellar_data/star-ref.csv', 'r', encoding='latin-1') as f:
    reader = csv.reader(f)
    header = next(reader)
    print(f"Headers: {header}")
    
    # Collect all stars with proper names and distances
    ref_stars = []
    row_count = 0
    for row in reader:
        row_count += 1
        if len(row) >= 3:
            system = row[0].strip() if row[0] else ""
            name = row[1].strip() if row[1] else ""
            distance_str = row[2].strip() if row[2] else ""
            
            # Extract distance in light years
            if distance_str:
                # Handle various distance formats
                match = re.search(r'([\d.]+)', distance_str)
                if match:
                    distance = float(match.group(1))
                    # Check if it's already in ly or needs conversion from pc
                    if "pc" in distance_str or distance < 100:  # Assume parsecs if small
                        distance = distance * 3.26156  # Convert to ly
                    
                    # Clean up the name
                    if name and not name in ['A', 'B', 'C', 'D', 'Ba', 'Bb', 'Ab']:
                        # Remove special characters and extra spaces
                        clean_name = name.replace('�', ' ')
                        clean_name = re.sub(r'\s+', ' ', clean_name).strip()
                        
                        if clean_name and clean_name != system:
                            ref_stars.append({
                                'name': clean_name,
                                'distance': distance,
                                'system': system
                            })
    
    print(f"Found {len(ref_stars)} named stars in {row_count} rows")

# Now read the original star catalog that was used to generate stars.csv
print("\nReading original star catalog...")
original_stars = []
with open('/mnt/c/users/davis/milky-way/stellar_data/hygdata_v3.csv', 'r', encoding='latin-1', errors='replace') as f:
    reader = csv.DictReader(f)
    for row in reader:
        if row.get('proper') or row.get('gl') or row.get('bf'):
            name = row.get('proper', '')
            if not name and row.get('gl'):
                name = f"Gliese {row['gl']}"
            elif not name and row.get('bf'):
                name = row['bf']
            
            if name and row.get('x') and row.get('y') and row.get('z'):
                try:
                    x, y, z = float(row['x']), float(row['y']), float(row['z'])
                    dist = math.sqrt(x*x + y*y + z*z)
                    if dist <= 80:  # Within 80 ly
                        original_stars.append({
                            'name': name,
                            'x': x,
                            'y': y,
                            'z': z,
                            'dist': dist
                        })
                except:
                    pass

print(f"Found {len(original_stars)} stars within 80 ly in original catalog")

# Read current stars.csv
stars = []
with open('/mnt/c/users/davis/milky-way/stellar_data/stars.csv', 'r') as f:
    reader = csv.DictReader(f)
    for row in reader:
        stars.append(row)

print(f"\nCurrent stars.csv has {len(stars)} entries")

# Match stars by position
fixed_count = 0
unmatched_star_numbers = []

for star in stars:
    if star['name'].startswith('Star-'):
        x, y, z = float(star['x']), float(star['y']), float(star['z'])
        
        # Try to match with original catalog by position
        best_match = None
        best_dist = float('inf')
        
        for orig in original_stars:
            dx = x - orig['x']
            dy = y - orig['y'] 
            dz = z - orig['z']
            dist = math.sqrt(dx*dx + dy*dy + dz*dz)
            
            if dist < best_dist and dist < 0.01:  # Very close position match
                best_dist = dist
                best_match = orig['name']
        
        if best_match:
            print(f"Matched {star['name']} -> {best_match} (position diff={best_dist:.6f})")
            star['name'] = best_match
            fixed_count += 1
        else:
            # Track unmatched for manual lookup
            star_num = star['name'].replace('Star-', '')
            unmatched_star_numbers.append(int(star_num))

print(f"\nFixed {fixed_count} additional star names by position matching")
print(f"Still have {len(unmatched_star_numbers)} unmatched stars")

# Write updated stars.csv
with open('/mnt/c/users/davis/milky-way/stellar_data/stars.csv', 'w', newline='') as f:
    fieldnames = ['name', 'x', 'y', 'z', 'stellar_class', 'mass', 'temperature', 'luminosity', 'absolute_magnitude']
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(stars)

print("\nUpdated stars.csv written successfully")

# Report some unmatched star numbers for investigation
if unmatched_star_numbers:
    unmatched_star_numbers.sort()
    print(f"\nFirst 10 unmatched star numbers: {unmatched_star_numbers[:10]}")
    print(f"Last 10 unmatched star numbers: {unmatched_star_numbers[-10:]}")