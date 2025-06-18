#!/usr/bin/env python3
import csv
import re
import math

# Read star-ref.csv and build a name lookup
star_names = {}
with open('/mnt/c/users/davis/milky-way/stellar_data/star-ref.csv', 'r', encoding='utf-8', errors='replace') as f:
    lines = f.readlines()
    
    current_system = None
    for line in lines[1:]:  # Skip header
        if not line.strip():
            continue
            
        parts = line.split(',')
        if len(parts) < 3:
            continue
            
        # Column 0 is system, Column 1 is name, Column 2 is distance
        system = parts[0].strip()
        name = parts[1].strip()
        distance_str = parts[2].strip()
        
        if system:
            current_system = system
        
        if not name or name == '':
            continue
            
        # Skip entries with just letters (A, B, C, etc.)
        if len(name) <= 2 and name.replace('�', '').strip() in ['A', 'B', 'C', 'D', 'Ba', 'Bb', 'Ab']:
            continue
            
        # Extract distance
        match = re.match(r'([\d.]+)', distance_str)
        if match:
            distance_pc = float(match.group(1))
            distance_ly = distance_pc * 3.26156
            
            # Clean up name
            clean_name = name.replace('�', ' ').strip()
            clean_name = re.sub(r'\s+', ' ', clean_name)
            
            # Remove parenthetical additions for primary lookup
            primary_name = re.sub(r'\s*\([^)]*\)\s*', ' ', clean_name).strip()
            
            if primary_name and primary_name not in ['', 'A', 'B', 'C', 'D']:
                star_names[primary_name] = distance_ly
                print(f"Added: {primary_name} at {distance_ly:.2f} ly")

print(f"\nLoaded {len(star_names)} star names from reference")

# Read current stars.csv
stars = []
with open('/mnt/c/users/davis/milky-way/stellar_data/stars.csv', 'r') as f:
    reader = csv.DictReader(f)
    for row in reader:
        stars.append(row)

# Match stars by position
fixed_count = 0
for star in stars:
    if star['name'].startswith('Star-'):
        x, y, z = float(star['x']), float(star['y']), float(star['z'])
        star_dist = math.sqrt(x**2 + y**2 + z**2)
        
        # Find best match by distance
        best_match = None
        best_diff = float('inf')
        
        for ref_name, ref_dist in star_names.items():
            diff = abs(star_dist - ref_dist)
            if diff < best_diff and diff < 0.1:  # Very tight tolerance
                best_diff = diff
                best_match = ref_name
        
        if best_match:
            print(f"Matched {star['name']} -> {best_match} (diff={best_diff:.4f} ly)")
            star['name'] = best_match
            fixed_count += 1
            # Remove from dict to avoid duplicate matches
            del star_names[best_match]

print(f"\nFixed {fixed_count} star names")

# Write updated stars.csv
with open('/mnt/c/users/davis/milky-way/stellar_data/stars.csv', 'w', newline='') as f:
    fieldnames = ['name', 'x', 'y', 'z', 'stellar_class', 'mass', 'temperature', 'luminosity', 'absolute_magnitude']
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(stars)

print("Updated stars.csv written successfully")