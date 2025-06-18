#!/usr/bin/env python3
import re

def clean_text(text):
    """Clean text encoding issues"""
    if not text:
        return text
    text = text.replace('�', ' ')
    text = text.replace('°', '')
    text = text.replace('±', '+/-')
    text = text.replace('$', '')
    text = text.replace('&', '')
    text = text.replace('"', '')
    text = re.sub(r'\s+', ' ', text)
    return text.strip()

print("Finding missing stars...")

# Read the original file and count all entries
with open('/mnt/c/users/davis/milky-way/stellar_data/star-ref.csv', 'rb') as f:
    content = f.read()
    content = content.replace(b'\r\n', b'\n').replace(b'\r', b'\n')
    text = content.decode('latin-1', errors='replace')

lines = text.split('\n')

# Track all potential star entries
all_entries = []
skipped_entries = []
star_count = 0

for i, line in enumerate(lines):
    if i <= 1:  # Skip headers
        continue
    
    if not line.strip():
        continue
    
    # Split by comma
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
    
    if len(parts) < 2:
        continue
    
    # Extract name (second field)
    name = clean_text(parts[1]) if parts[1] else ""
    
    if not name:
        continue
    
    # Track the entry
    all_entries.append({
        'line': i + 1,
        'name': name,
        'raw_line': line[:100] + '...' if len(line) > 100 else line
    })
    
    # Check for special cases we might have skipped
    if "10 parsecs" in name:
        skipped_entries.append(f"Line {i+1}: {name} - Special marker")
        continue
    
    # Check distance if available
    if len(parts) > 2:
        distance_str = parts[2]
        if distance_str:
            # Try to parse distance
            match = re.search(r'([\d.]+)', distance_str)
            if match:
                distance = float(match.group(1))
                # Convert to light years if needed
                if distance <= 25:
                    distance = distance * 3.26156
                else:
                    distance_ly = distance * 3.26156
                    if distance_ly > 260:
                        # Check if already in ly
                        if distance > 260:
                            skipped_entries.append(f"Line {i+1}: {name} - Distance {distance:.1f} ly (too far)")
                            continue
    
    star_count += 1

print(f"\nTotal entries found: {len(all_entries)}")
print(f"Valid stars counted: {star_count}")
print(f"Expected: 1421")
print(f"Difference: {1421 - star_count}")

# Read what we actually extracted
extracted_names = set()
with open('/home/davis/final_1421_stars.csv', 'r') as f:
    next(f)  # Skip header
    for line in f:
        parts = line.strip().split(',')
        if parts:
            extracted_names.add(parts[0])

print(f"\nExtracted stars: {len(extracted_names)}")

# Find specific entries we might have missed
print("\nChecking for specific missing entries...")
print("\nSkipped entries:")
for entry in skipped_entries[:10]:
    print(f"  {entry}")

# Look for patterns in the original file
print("\nSampling entries around line 1400-1422:")
for i in range(max(0, len(lines) - 25), len(lines)):
    if i < len(lines) and lines[i].strip():
        parts = lines[i].split(',')
        if len(parts) >= 2 and parts[1]:
            name = clean_text(parts[1])
            if name and not name.startswith('Column'):
                print(f"  Line {i+1}: {name}")

# Check for any entries with missing distances that we might have skipped
print("\nChecking for entries with missing distance data...")
no_distance_count = 0
for i, line in enumerate(lines):
    if i <= 1:
        continue
    if not line.strip():
        continue
    
    parts = line.split(',')
    if len(parts) >= 3:
        name = clean_text(parts[1]) if parts[1] else ""
        distance = parts[2].strip() if len(parts) > 2 else ""
        
        if name and not distance:
            no_distance_count += 1
            if no_distance_count <= 5:
                print(f"  Line {i+1}: {name} - No distance")