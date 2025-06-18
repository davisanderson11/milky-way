#!/usr/bin/env python3
import csv
import re

# Read star-ref.csv
star_ref = {}
with open('/mnt/c/users/davis/milky-way/stellar_data/star-ref.csv', 'r', encoding='utf-8') as f:
    reader = csv.reader(f)
    next(reader)  # Skip header
    for row in reader:
        if len(row) >= 3:
            name = row[1].strip()
            distance_str = row[2].strip()
            # Extract distance number
            match = re.match(r'([\d.]+)', distance_str)
            if match:
                distance = float(match.group(1))
                # Convert parsecs to light years
                distance_ly = distance * 3.26156
                star_ref[distance_ly] = name

# Read current stars.csv
stars = []
with open('/mnt/c/users/davis/milky-way/stellar_data/stars.csv', 'r') as f:
    reader = csv.DictReader(f)
    for row in reader:
        stars.append(row)

# Match stars by distance (approximate)
fixed_count = 0
for star in stars:
    if star['name'].startswith('Star-'):
        # Calculate distance from x,y,z
        x, y, z = float(star['x']), float(star['y']), float(star['z'])
        distance = (x**2 + y**2 + z**2)**0.5
        
        # Find closest match in star_ref
        best_match = None
        best_diff = float('inf')
        for ref_dist, ref_name in star_ref.items():
            diff = abs(distance - ref_dist)
            if diff < best_diff and diff < 0.5:  # Within 0.5 ly tolerance
                best_diff = diff
                best_match = ref_name
        
        if best_match:
            print(f"Matched {star['name']} (dist={distance:.2f}) -> {best_match}")
            star['name'] = best_match
            fixed_count += 1

print(f"\nFixed {fixed_count} star names")

# Write updated stars.csv
with open('/mnt/c/users/davis/milky-way/stellar_data/stars.csv', 'w', newline='') as f:
    fieldnames = ['name', 'x', 'y', 'z', 'stellar_class', 'mass', 'temperature', 'luminosity', 'absolute_magnitude']
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(stars)

print("Updated stars.csv written successfully")