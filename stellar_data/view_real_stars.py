#!/usr/bin/env python3
import pandas as pd
import numpy as np
import plotly.express as px
import re

# 1) Load your real-star CSV
df = pd.read_csv("stars.csv")

# 1.1) Filter to primary stars only (drop companion entries ending with a single uppercase letter)
companion_pattern = re.compile(r".* [A-Z]$")
df = df[~df['Name'].str.match(companion_pattern, na=False)]

# 2) Convert Distance (pc) → ly
df['r_ly'] = df['Distance'] * 3.26156

# 3) Convert RA/Dec (degrees) → radians
df['ra_rad']  = np.deg2rad(df['RA'])
df['dec_rad'] = np.deg2rad(df['Dec'])

# 4) Spherical → Cartesian in LY
df['x'] = df['r_ly'] * np.cos(df['dec_rad']) * np.cos(df['ra_rad'])
df['y'] = df['r_ly'] * np.cos(df['dec_rad']) * np.sin(df['ra_rad'])
df['z'] = df['r_ly'] * np.sin(df['dec_rad'])

# 5) Center on Sol
sol = df[df['Name']=="Sol"]
if sol.empty:
    raise RuntimeError("No entry named 'Sol' in stars.csv")
sol = sol.iloc[0]
df['x0'] = df['x'] - sol['x']
df['y0'] = df['y'] - sol['y']
df['z0'] = df['z'] - sol['z']

# 6) Plot
fig = px.scatter_3d(
    df,
    x='x0', y='y0', z='z0',
    color='SpectralType',
    hover_name='Name',
    title='Primary Real Stars around Sol (0,0,0)',
    width=900, height=700
)
fig.show()
