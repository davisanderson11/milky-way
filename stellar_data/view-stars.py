#!/usr/bin/env python3
import argparse
import pandas as pd
import numpy as np
import re
from scipy.spatial import ConvexHull
import plotly.graph_objects as go

# Parse command-line args
default_description = "Visualize nearby stars with optional territory bubbles"
parser = argparse.ArgumentParser(description=default_description)
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
df['r_ly'] = df['Distance']

# Spherical → Cartesian
df['ra_rad']  = np.deg2rad(df['RA'])
df['dec_rad'] = np.deg2rad(df['Dec'])
df['x'] = df['r_ly'] * np.cos(df['dec_rad']) * np.cos(df['ra_rad'])
df['y'] = df['r_ly'] * np.cos(df['dec_rad']) * np.sin(df['ra_rad'])
df['z'] = df['r_ly'] * np.sin(df['dec_rad'])

# Center on Sol (0,0,0)
sol_row = df[df['Name'] == "Sol"]
if sol_row.empty:
    raise RuntimeError("No entry named 'Sol' in stars.csv")
# After subtraction, Sol's x0,y0,z0 will be (0,0,0)
sol = sol_row.iloc[0]
df['x0'] = df['x'] - sol['x']
df['y0'] = df['y'] - sol['y']
df['z0'] = df['z'] - sol['z']

# Store Sol-centered origin explicitly
sol_x0, sol_y0, sol_z0 = 0.0, 0.0, 0.0

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
max_dist = df['r_ly'].max()
slider_vals = np.linspace(0, max_dist, 20)

# 4) Build doubled-up scatter traces for hover hit-areas
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

# 6) Add galactic plane (z=0)
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

# 7.5) Territory bubbles via axis-aligned expansion
if not args.no_territory:
    # Store territory data for filtering
    territory_star_indices = {}
    territory_meshes = {}
    
    # define each region with a center offset and expansions
    territories = [
        {
            'name': 'Allied Core Systems',
            'center': (sol_x0, sol_y0, sol_z0),
            'expansion': {'x': (-15, 15), 'y': (-15, 7), 'z': (-7, 13)},
            'color': 'green'
        },
        {
            'name': 'Boreal Congress',
            # center at Alpha Coronae Borealis
            'center_name': 'Alpha Coronae Borealis',
            'expansion': {'x': (-20, 20), 'y': (-20, 20), 'z': (-20, 20)},
            'color': 'cyan'
        },
        {
            'name': 'Sirius-Procyon Assembly',
            'center_name': 'Procyon',
            'expansion': {'x': (-10, 15), 'y': (-3, 15), 'z': (-12, 15)},
            'color': 'yellow'
        },
        {
            'name': 'Second Tribunal',
            'center_name': '41 G. Arae',
            'expansion': {'x': (-10, 10), 'y': (-10, 10), 'z': (-10, 10)},
            'color': 'red'
        },
        {
            'name': 'Regulus Electorate',
            'center_name': 'Regulus',
            'expansion': {'x': (-35, 35), 'y': (-35, 35), 'z': (-20, 20)},
            'color': 'orange'
        },
        {
            'name': 'Aldebaran Electorate',
            'center_name': 'Aldebaran',
            'expansion': {'x': (-15, 15), 'y': (-15, 15), 'z': (-15, 15)},
            'color': 'orange'
        },
        {
            'name': 'Alpha Cephei Electorate',
            'center_name': 'Alpha Cephei',
            'expansion': {'x': (-15, 15), 'y': (-15, 15), 'z': (-15, 15)},
            'color': 'orange'
        }
    ]

    for info in territories:
        # determine center
        if 'center_name' in info:
            row = df[df['Name'] == info['center_name']]
            if row.empty:
                print(f"Warning: {info['center_name']} not found!")
                continue
            cx, cy, cz = row.iloc[0][['x0', 'y0', 'z0']]
            print(f"\n{info['name']} centered at {info['center_name']}: ({cx:.2f}, {cy:.2f}, {cz:.2f})")
        else:
            cx, cy, cz = info['center']

        ex = info['expansion']
        print(f"  Bounds: X[{cx + ex['x'][0]:.2f}, {cx + ex['x'][1]:.2f}], Y[{cy + ex['y'][0]:.2f}, {cy + ex['y'][1]:.2f}], Z[{cz + ex['z'][0]:.2f}, {cz + ex['z'][1]:.2f}]")
        mask = (
            (df['x0'] >= cx + ex['x'][0]) & (df['x0'] <= cx + ex['x'][1]) &
            (df['y0'] >= cy + ex['y'][0]) & (df['y0'] <= cy + ex['y'][1]) &
            (df['z0'] >= cz + ex['z'][0]) & (df['z0'] <= cz + ex['z'][1])
        )
        # Store the indices of stars in this territory
        territory_indices = df[mask].index.tolist()
        pts = df[mask][['x0', 'y0', 'z0']].values
        print(f"  Stars in territory: {len(pts)}")
        if len(pts) < 4:
            print(f"  Skipping - need at least 4 stars")
            continue

        # Store star indices for this territory
        territory_star_indices[info['name']] = territory_indices

        # convex hull + smoothing
        hull = ConvexHull(pts)
        hull_pts = pts.copy()
        for s in hull.simplices:
            for i, j in [(0,1), (1,2), (2,0)]:
                hull_pts = np.vstack([hull_pts,
                    (pts[s[i]] + pts[s[j]]) / 2
                ])
        smooth = ConvexHull(hull_pts)

        # Create mesh trace
        mesh_trace = go.Mesh3d(
            x=smooth.points[:,0],
            y=smooth.points[:,1],
            z=smooth.points[:,2],
            i=smooth.simplices[:,0],
            j=smooth.simplices[:,1],
            k=smooth.simplices[:,2],
            opacity=0.2,
            color=info['color'],
            name=info['name'],
            showscale=False,
            showlegend=True
        )
        
        fig.add_trace(mesh_trace)
        territory_meshes[info['name']] = len(fig.data) - 1  # Store trace index
    
    # Update stars.csv with allegiance information
    print("\nUpdating stars.csv with allegiance information...")
    
    # Read the original CSV to preserve all data including companion stars
    df_original = pd.read_csv("stars.csv")
    
    # Add Allegiance column if it doesn't exist
    if 'Allegiance' not in df_original.columns:
        df_original['Allegiance'] = 'Independent'  # Default to Independent
    
    # Reset all to Independent first (in case territories changed)
    df_original['Allegiance'] = 'Independent'
    
    # For each territory, find matching stars in the original dataframe
    for territory_name, star_indices in territory_star_indices.items():
        # Get the names of stars in this territory
        territory_star_names = df.loc[star_indices, 'Name'].tolist()
        
        # Update allegiance for these stars in the original dataframe
        df_original.loc[df_original['Name'].isin(territory_star_names), 'Allegiance'] = territory_name
    
    # Save updated CSV
    df_original.to_csv('stars.csv', index=False)
    print(f"stars.csv updated with Allegiance column - {len(territory_star_indices)} territories assigned")

# 8) Slider steps
steps = [
    dict(
        method='animate',
        args=[[f"{v:.1f}"], dict(mode='immediate', frame=dict(duration=0), transition=dict(duration=0))],
        label=f"{v:.0f} ly"
    ) for v in slider_vals
]

# 9) Reorder so planes, rings, & meshes are beneath stars
mesh_count = 1 + len(ring_radii)*2 + (0 if args.no_territory else len(territories))
all_traces = list(fig.data)
under = all_traces[:mesh_count]
over  = all_traces[mesh_count:]
fig.data = tuple(under + over)

# 10) Add territory filter dropdown if territories exist
dropdown_menus = []
if not args.no_territory and territory_star_indices:
    # Get the number of star traces (before territories were added)
    num_star_traces = len(spec_classes) * 2  # 2 traces per spectral class
    num_base_traces = 1 + len(ring_radii) * 2  # plane + rings
    
    # Store original star trace data for "All Territories" option
    original_star_data = []
    for i in range(num_base_traces, num_base_traces + num_star_traces):
        trace = fig.data[i]
        original_star_data.append({
            'x': trace.x,
            'y': trace.y,
            'z': trace.z,
            'text': getattr(trace, 'text', None)
        })
    
    buttons = [
        dict(
            label="All Territories",
            method="update",
            args=[
                {
                    "visible": [True] * len(fig.data),
                    "x": [d['x'] for d in original_star_data] + [None] * (len(fig.data) - num_base_traces - num_star_traces),
                    "y": [d['y'] for d in original_star_data] + [None] * (len(fig.data) - num_base_traces - num_star_traces),
                    "z": [d['z'] for d in original_star_data] + [None] * (len(fig.data) - num_base_traces - num_star_traces),
                    "text": [d['text'] for d in original_star_data] + [None] * (len(fig.data) - num_base_traces - num_star_traces)
                },
                [num_base_traces + i for i in range(num_star_traces)]
            ]
        )
    ]
    
    # Add individual territory views
    for territory_name, star_indices in territory_star_indices.items():
        # Get stars in this territory
        territory_stars = df.loc[star_indices]
        
        # Build trace data for only this territory's stars
        trace_data = []
        
        # Recreate star traces with only territory stars
        for cls in spec_classes:
            cls_stars = territory_stars[territory_stars['SpectralType'].str.startswith(cls, na=False)]
            
            if len(cls_stars) > 0:
                hover_text = [
                    f"{name}: ({x0:.2f}, {y0:.2f}, {z0:.2f})\nDistance: {r:.2f} ly"
                    for name, r, x0, y0, z0 in zip(
                        cls_stars['Name'], cls_stars['r_ly'], 
                        cls_stars['x0'], cls_stars['y0'], cls_stars['z0']
                    )
                ]
                
                # Hover trace
                trace_data.append(dict(
                    x=cls_stars['x0'].tolist(),
                    y=cls_stars['y0'].tolist(),
                    z=cls_stars['z0'].tolist(),
                    text=hover_text
                ))
                
                # Visible trace
                trace_data.append(dict(
                    x=cls_stars['x0'].tolist(),
                    y=cls_stars['y0'].tolist(),
                    z=cls_stars['z0'].tolist()
                ))
            else:
                # Empty traces for this spectral class
                trace_data.append(dict(x=[None], y=[None], z=[None], text=[""]))
                trace_data.append(dict(x=[None], y=[None], z=[None]))
        
        # Create visibility array
        visible = [True] * len(fig.data)  # Start with all visible
        
        # Hide all territory meshes except this one
        for t_name, mesh_idx in territory_meshes.items():
            visible[mesh_idx] = (t_name == territory_name)
        
        buttons.append(dict(
            label=territory_name,
            method="update",
            args=[
                {
                    "visible": visible,
                    "x": [td.get('x') for td in trace_data] + [None] * (len(fig.data) - num_base_traces - num_star_traces),
                    "y": [td.get('y') for td in trace_data] + [None] * (len(fig.data) - num_base_traces - num_star_traces),
                    "z": [td.get('z') for td in trace_data] + [None] * (len(fig.data) - num_base_traces - num_star_traces),
                    "text": [td.get('text', []) for td in trace_data] + [None] * (len(fig.data) - num_base_traces - num_star_traces)
                },
                [num_base_traces + i for i in range(num_star_traces)]  # Update only star traces
            ]
        ))
    
    dropdown_menus.append(dict(
        buttons=buttons,
        direction="down",
        showactive=True,
        x=0.1,
        xanchor="left",
        y=1.15,
        yanchor="top"
    ))

# 11) Final layout
fig.update_layout(
    title="Primary Real Stars around Sol (0,0,0)",
    autosize=True,
    margin=dict(l=0, r=0, t=100, b=0),  # Increased top margin for dropdown
    sliders=[dict(
        active=len(steps)-1,
        currentvalue={'prefix':'Max distance: '},
        pad={'t':50},
        steps=steps
    )],
    updatemenus=dropdown_menus,
    legend=dict(title='Key', itemsizing='constant'),
    hovermode='closest',
    scene=dict(
        bgcolor='rgb(10,10,10)',
        aspectmode='cube',
        xaxis=dict(range=[-max_dist, max_dist], backgroundcolor='rgb(10,10,10)', gridcolor='gray', zerolinecolor='gray', showspikes=True, spikesides=True, spikethickness=1),
        yaxis=dict(range=[-max_dist, max_dist], backgroundcolor='rgb(10,10,10)', gridcolor='gray', zerolinecolor='gray', showspikes=True, spikesides=True, spikethickness=1),
        zaxis=dict(range=[-max_dist, max_dist], backgroundcolor='rgb(10,10,10)', gridcolor='gray', zerolinecolor='gray', showspikes=True, spikesides=True, spikethickness=1),
        camera=dict(center=dict(x=0, y=0, z=0), eye=dict(x=1.25, y=1.25, z=1.25))
    )
)

fig.show(config={'responsive': True})