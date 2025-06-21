#!/usr/bin/env python3
import argparse                              # for territory toggle
import pandas as pd
import numpy as np
import re
from scipy.spatial import ConvexHull
import plotly.graph_objects as go

# Parse command-line args
parser = argparse.ArgumentParser(description="Visualize nearby stars with optional territory bubbles")
parser.add_argument(
    "--no-territory",
    action="store_true",
    help="Disable territory viewing mode"
)
args = parser.parse_args()

# 1) Load & preprocess
df = pd.read_csv("stars.csv")
companion_pattern = re.compile(r".* [A-Z]$")
df = df[~df['Name'].str.match(companion_pattern, na=False)]

# Distance is already in LY
df['r_ly'] = df['Distance']

# Spherical → Cartesian
df['ra_rad']  = np.deg2rad(df['RA'])
df['dec_rad'] = np.deg2rad(df['Dec'])
df['x'] = df['r_ly'] * np.cos(df['dec_rad']) * np.cos(df['ra_rad'])
df['y'] = df['r_ly'] * np.cos(df['dec_rad']) * np.sin(df['ra_rad'])
df['z'] = df['r_ly'] * np.sin(df['dec_rad'])

# Center on Sol
sol = df[df['Name']=="Sol"]
if sol.empty:
    raise RuntimeError("No entry named 'Sol' in stars.csv")
sol = sol.iloc[0]
df['x0'] = df['x'] - sol['x']
df['y0'] = df['y'] - sol['y']
df['z0'] = df['z'] - sol['z']

# 2) Spectral classes & colors
spec_classes = sorted({st[0] for st in df['SpectralType'].dropna()})
color_map = {
    'O': 'blue', 'B': 'deepskyblue', 'A': 'lightskyblue',
    'F': 'yellow', 'G': 'gold', 'K': 'orange',
    'M': 'red', 'L': 'saddlebrown', 'T': 'mediumpurple',
    'Y': 'darkviolet', 'W': 'lightgray'
}
for cls in spec_classes:
    color_map.setdefault(cls, 'gray')

# 3) Slider cutoffs
max_dist    = df['r_ly'].max()
slider_vals = np.linspace(0, max_dist, 20)

# 4) Build doubled‐up scatter traces for hover hit-areas
def make_traces(cutoff):
    traces = []
    mask = df['r_ly'] <= cutoff
    for cls in spec_classes:
        sub = df[mask & df['SpectralType'].str.startswith(cls, na=False)]
        hover_text = [
            f"{name}: ({x0:.2f}, {y0:.2f}, {z0:.2f})\nDistance: {r:.2f} ly"
            for name, r, x0, y0, z0 in zip(
                sub['Name'], sub['r_ly'], sub['x0'], sub['y0'], sub['z0']
            )
        ]
        # invisible larger markers catch hover
        traces.append(go.Scatter3d(
            x=sub['x0'], y=sub['y0'], z=sub['z0'],
            mode='markers',
            marker=dict(size=8, color='rgba(0,0,0,0)'),
            text=hover_text, hoverinfo='text',
            showlegend=False
        ))
        # visible small markers on top
        traces.append(go.Scatter3d(
            x=sub['x0'], y=sub['y0'], z=sub['z0'],
            mode='markers',
            name=cls, legendgroup=cls,
            marker=dict(size=4, color=color_map[cls]),
            hoverinfo='skip'
        ))
    return traces

# 5) Main figure + frames
fig = go.Figure(
    data=make_traces(max_dist),
    frames=[go.Frame(data=make_traces(v), name=f"{v:.1f}") for v in slider_vals]
)

# 6) Add galactic plane (z=0) behind everything
plane = max_dist
xx, yy = np.meshgrid([-plane, plane], [-plane, plane])
zz = np.zeros_like(xx)
fig.add_trace(go.Surface(
    x=xx, y=yy, z=zz,
    showscale=False, opacity=0.2,
    colorscale=[[0, 'gray'], [1, 'gray']],
    hoverinfo='skip'
))

# 7) Distance rings & labels
ring_radii = np.arange(10, max_dist+1, 10)
theta = np.linspace(0, 2*np.pi, 200)
for r in ring_radii:
    fig.add_trace(go.Scatter3d(
        x=r*np.cos(theta), y=r*np.sin(theta), z=np.zeros_like(theta),
        mode='lines', line=dict(color='lightgray', width=1),
        hoverinfo='skip', showlegend=False
    ))
    fig.add_trace(go.Scatter3d(
        x=[r], y=[0], z=[0],
        mode='markers+text',
        marker=dict(size=2, color='lightgray'),
        text=[f"{int(r)} ly"], textposition='bottom center',
        hoverinfo='skip', showlegend=False
    ))

# 7.5) Allied Core territory bubble (inflated, smoothed, legend toggle)
if not args.no_territory:
    allied = [
        "Sol", "Proxima Centauri", "Α Centauri", "Barnard’s Star",
        "Lalande 21185", "Wolf 359", "Luyten’s Star", "61 Cygni",
        "YZ Ceti", "DX Cancri", "Ross 248", "Ε Eridani",
        "Teegarden’s Star", "Ε Indi", "Gliese 65", "Ross 128",
        "Lacaille 9352", "EZ Aquarii", "Struve 2398", "GJ 1061"
    ]
    pts = df[df['Name'].isin(allied)][['x0','y0','z0']].values

    if len(pts) >= 4:
        # inflate points outwards from centroid
        centroid = pts.mean(axis=0)
        directions = pts - centroid
        inflated = centroid + directions * 1.15  # 15% expansion

        # compute convex hull on inflated points
        hull = ConvexHull(inflated)
        hull_pts = inflated.copy()

        # add midpoint of each hull edge for extra vertices
        for simplex in hull.simplices:
            for i, j in [(0,1), (1,2), (2,0)]:
                mid = (inflated[simplex[i]] + inflated[simplex[j]]) / 2
                hull_pts = np.vstack([hull_pts, mid])

        # final hull for mesh
        smooth_hull = ConvexHull(hull_pts)

        fig.add_trace(go.Mesh3d(
            x=smooth_hull.points[:,0],
            y=smooth_hull.points[:,1],
            z=smooth_hull.points[:,2],
            i=smooth_hull.simplices[:,0],
            j=smooth_hull.simplices[:,1],
            k=smooth_hull.simplices[:,2],
            opacity=0.2,
            color='green',
            name='Allied Core Territory',
            showscale=False,
            showlegend=True         # ← makes it appear under the legend (key)
        ))

# 8) Slider steps
steps = [
    dict(
        method='animate',
        args=[[f"{v:.1f}"],
              dict(mode='immediate', frame=dict(duration=0), transition=dict(duration=0))],
        label=f"{v:.0f} ly"
    ) for v in slider_vals
]

# 9) Reorder so plane, rings, & (optional) bubble are under all scatter traces
extra = 1 + len(ring_radii)*2 + (0 if args.no_territory else 1)
all_traces = list(fig.data)
plane_and_rings = all_traces[:extra]
stars = all_traces[extra:]
fig.data = tuple(plane_and_rings + stars)

# 10) Final layout: responsive full-window, dark plot-area, axis spikes, legend as “Key”
fig.update_layout(
    title="Primary Real Stars around Sol (0,0,0)",
    autosize=True,
    margin=dict(l=0, r=0, t=50, b=0),
    sliders=[dict(
        active=len(steps)-1,
        currentvalue={'prefix':'Max distance: '},
        pad={'t':50},
        steps=steps
    )],
    legend=dict(
        title='Key',                # ← legend now titled “Key”
        itemsizing='constant'
    ),
    hovermode='closest',
    scene=dict(
        bgcolor='rgb(10,10,10)',
        aspectmode='cube',
        xaxis=dict(
            range=[-max_dist, max_dist],
            backgroundcolor='rgb(10,10,10)',
            gridcolor='gray', zerolinecolor='gray',
            showspikes=True, spikesides=True, spikethickness=1
        ),
        yaxis=dict(
            range=[-max_dist, max_dist],
            backgroundcolor='rgb(10,10,10)',
            gridcolor='gray', zerolinecolor='gray',
            showspikes=True, spikesides=True, spikethickness=1
        ),
        zaxis=dict(
            range=[-max_dist, max_dist],
            backgroundcolor='rgb(10,10,10)',
            gridcolor='gray', zerolinecolor='gray',
            showspikes=True, spikesides=True, spikethickness=1
        ),
        camera=dict(
            center=dict(x=0, y=0, z=0),
            eye=dict(x=1.25, y=1.25, z=1.25)
        )
    )
)

# Show responsively to fill browser window
fig.show(config={'responsive': True})
