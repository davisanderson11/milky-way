using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Manages real astronomical data for stars within the Sol suppression bubble
/// </summary>
public class RealStellarData
{
    public class RealStar
    {
        public string SystemName { get; set; } = "";
        public string[] AlternateNames { get; set; } = Array.Empty<string>();
        public double RA { get; set; }  // Right Ascension in degrees
        public double Dec { get; set; } // Declination in degrees
        public double Distance { get; set; } // Distance in light years
        public string Type { get; set; } = "";  // Spectral type - matches procedural system
        public double Mass { get; set; } // Solar masses
        public double Temperature { get; set; } // Kelvin
        public double Luminosity { get; set; } // Solar luminosities
        public double ApparentMagnitude { get; set; }
        public double AbsoluteMagnitude { get; set; }
        public List<RealPlanet> Planets { get; set; } = new List<RealPlanet>();
        public List<RealStar> CompanionStars { get; set; } = new List<RealStar>();
        public int PlanetCount => Planets.Count;  // Match procedural system
        public bool IsMultiple => CompanionStars.Count > 0;  // Match procedural system
        
        // Computed galactic coordinates - matches Position in procedural system
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
    
    public class RealPlanet
    {
        public string Name { get; set; } = "";
        public string ParentStar { get; set; } = "";
        public double Mass { get; set; } // Earth masses (or Jupiter masses for giants)
        public string MassUnit { get; set; } = "Earth"; // "Earth" or "Jupiter"
        public double Radius { get; set; } // Earth radii (or Jupiter radii)
        public string RadiusUnit { get; set; } = "Earth";
        public double OrbitalPeriod { get; set; } // days
        public double SemiMajorAxis { get; set; } // AU
        public double Eccentricity { get; set; }
        public string PlanetType { get; set; } = ""; // Terrestrial, Gas Giant, Ice Giant, etc.
        public List<RealMoon> Moons { get; set; } = new List<RealMoon>();
    }
    
    public class RealMoon
    {
        public string Name { get; set; } = "";
        public string ParentPlanet { get; set; } = "";
        public double Mass { get; set; } // Moon masses
        public double Radius { get; set; } // Moon radii
        public double OrbitalPeriod { get; set; } // days
        public double SemiMajorAxis { get; set; } // km
    }
    
    private List<RealStar> stars = new List<RealStar>();
    private Dictionary<string, RealStar> starLookup = new Dictionary<string, RealStar>(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Load real stellar data from CSV files
    /// </summary>
    public void LoadData(string starsFile, string? planetsFile = null, string? moonsFile = null)
    {
        Console.WriteLine($"Loading real stellar data from: {starsFile}");
        
        if (!File.Exists(starsFile))
        {
            Console.WriteLine($"Stars file not found: {starsFile}");
            LoadDefaultData();
            return;
        }
        
        LoadStars(starsFile);
        
        // Group companion stars (B, C, etc.) with their primary (A) stars
        GroupCompanionStars();
        
        if (planetsFile != null && File.Exists(planetsFile))
        {
            Console.WriteLine($"Loading planets from: {planetsFile}");
            LoadPlanets(planetsFile);
        }
            
        if (moonsFile != null && File.Exists(moonsFile))
        {
            Console.WriteLine($"Loading moons from: {moonsFile}");
            LoadMoons(moonsFile);
        }
            
        ComputeGalacticCoordinates();
        
        int primaryCount = stars.Count(s => s.CompanionStars.Count > 0 || !IsCompanionDesignation(s.SystemName));
        Console.WriteLine($"Loaded {stars.Count} real stars ({primaryCount} primary systems), {GetTotalPlanetCount()} planets");
    }
    
    /// <summary>
    /// Load default data message
    /// </summary>
    public void LoadDefaultData()
    {
        Console.WriteLine("No CSV files found in stellar_data/ directory.");
        Console.WriteLine("Please ensure stars.csv, planets.csv, and moons.csv are present.");
    }
    
    private void LoadStars(string filename)
    {
        // Load stars from CSV
        // Format: Name,RA,Dec,Distance,SpectralType,Mass,Temperature,Luminosity,ApparentMag
        if (!File.Exists(filename))
        {
            Console.WriteLine($"Stars file not found: {filename}. Using default data.");
            LoadDefaultData();
            return;
        }
        
        using (var reader = new StreamReader(filename))
        {
            string? line = reader.ReadLine(); // Skip header
            while ((line = reader.ReadLine()) != null)
            {
                var parts = ParseCsvLine(line);
                if (parts.Count >= 8)
                {
                    try
                    {
                        var star = new RealStar
                        {
                            SystemName = parts[0].Trim(),
                            RA = ParseDouble(parts[1]),
                            Dec = ParseDouble(parts[2]),
                            Distance = ParseDouble(parts[3]),
                            Type = MapSpectralType(parts[4].Trim()),
                            Mass = ParseDouble(parts[5]),
                            Temperature = ParseDouble(parts[6]),
                            Luminosity = ParseDouble(parts[7]),
                            ApparentMagnitude = parts.Count > 8 ? ParseDouble(parts[8]) : 0
                        };
                        
                        // Calculate absolute magnitude if not provided
                        if (star.ApparentMagnitude != 0 && star.Distance > 0)
                        {
                            star.AbsoluteMagnitude = star.ApparentMagnitude - 5 * Math.Log10(star.Distance / 10.0);
                        }
                        
                        AddStar(star);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing star data: {line}");
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }
        }
    }
    
    private void LoadPlanets(string filename)
    {
        // Load planets from CSV
        // Format: PlanetName,StarName,Mass,MassUnit,Radius,RadiusUnit,Period,SemiMajorAxis,Eccentricity,Type
        using (var reader = new StreamReader(filename))
        {
            string? line = reader.ReadLine(); // Skip header
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(',');
                if (parts.Length >= 10)
                {
                    var planet = new RealPlanet
                    {
                        Name = parts[0].Trim(),
                        ParentStar = parts[1].Trim(),
                        Mass = double.Parse(parts[2]),
                        MassUnit = parts[3].Trim(),
                        Radius = double.Parse(parts[4]),
                        RadiusUnit = parts[5].Trim(),
                        OrbitalPeriod = double.Parse(parts[6]),
                        SemiMajorAxis = double.Parse(parts[7]),
                        Eccentricity = double.Parse(parts[8]),
                        PlanetType = parts[9].Trim()
                    };
                    
                    // Find parent star and add planet
                    if (starLookup.TryGetValue(planet.ParentStar, out var star))
                    {
                        star.Planets.Add(planet);
                    }
                }
            }
        }
    }
    
    private void LoadMoons(string filename)
    {
        // Load moons from CSV
        // Format: MoonName,PlanetName,Mass,Radius,Period,SemiMajorAxis
        if (!File.Exists(filename))
        {
            Console.WriteLine($"Moons file not found: {filename}");
            return;
        }
        
        using (var reader = new StreamReader(filename))
        {
            string? line = reader.ReadLine(); // Skip header
            while ((line = reader.ReadLine()) != null)
            {
                try
                {
                    var parts = ParseCsvLine(line);
                    if (parts.Count >= 6)
                    {
                        var moon = new RealMoon
                        {
                            Name = parts[0].Trim(),
                            ParentPlanet = parts[1].Trim(),
                            Mass = ParseDouble(parts[2]),
                            Radius = ParseDouble(parts[3]),
                            OrbitalPeriod = ParseDouble(parts[4]),
                            SemiMajorAxis = ParseDouble(parts[5])
                        };
                        
                        // Find parent planet across all stars
                        foreach (var star in stars)
                        {
                            var planet = star.Planets.FirstOrDefault(p => p.Name == moon.ParentPlanet);
                            if (planet != null)
                            {
                                planet.Moons.Add(moon);
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing moon line: {line}");
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
    
    private void AddStar(RealStar star)
    {
        stars.Add(star);
        starLookup[star.SystemName] = star;
        
        // Extract primary name (before parentheses) for planet/moon matching
        string primaryName = star.SystemName;
        int parenIndex = primaryName.IndexOf('(');
        if (parenIndex > 0)
        {
            primaryName = primaryName.Substring(0, parenIndex).Trim();
        }
        
        // Remove trailing $ or other markers
        primaryName = primaryName.TrimEnd('$', ' ');
        
        // Add primary name to lookup if different from system name
        if (primaryName != star.SystemName && !starLookup.ContainsKey(primaryName))
        {
            starLookup[primaryName] = star;
        }
        
        // Also add alternate names to lookup
        foreach (var altName in star.AlternateNames)
        {
            starLookup[altName] = star;
        }
    }
    
    /// <summary>
    /// Convert RA/Dec to galactic XYZ coordinates relative to Sol
    /// </summary>
    private void ComputeGalacticCoordinates()
    {
        // Sol's position in the galaxy (as defined in suppression bubble)
        const double SOL_R = 26550.0;
        const double SOL_THETA_DEG = 0.5;
        const double SOL_Z = 0.0;
        
        double solTheta = SOL_THETA_DEG * Math.PI / 180.0;
        double solX = SOL_R * Math.Cos(solTheta);
        double solY = SOL_R * Math.Sin(solTheta);
        
        Console.WriteLine($"Computing galactic coordinates for {stars.Count} real stars");
        Console.WriteLine($"Sol position: ({solX:F1}, {solY:F1}, {SOL_Z:F1})");
        
        foreach (var star in stars)
        {
            if (star.Distance == 0) // Sol itself
            {
                star.X = solX;
                star.Y = solY;
                star.Z = SOL_Z;
                continue;
            }
            
            // For now, use a simple random distribution within 80 ly of Sol
            // This ensures stars actually appear in the suppression bubble
            var rng = new Random((int)(star.RA * 1000 + star.Dec * 1000 + star.Distance * 1000));
            
            // Generate random position within 80 ly sphere
            double r = star.Distance; // Use actual distance
            double theta = rng.NextDouble() * 2 * Math.PI;
            double phi = Math.Acos(2 * rng.NextDouble() - 1);
            
            double x = r * Math.Sin(phi) * Math.Cos(theta);
            double y = r * Math.Sin(phi) * Math.Sin(theta);
            double z = r * Math.Cos(phi);
            
            // Add to Sol's position
            star.X = solX + x;
            star.Y = solY + y;
            star.Z = SOL_Z + z;
        }
        
        Console.WriteLine($"Real stars positioned around Sol");
    }
    
    /// <summary>
    /// Get all stars within a certain distance of a position
    /// </summary>
    public List<RealStar> GetStarsWithinRadius(double x, double y, double z, double radius)
    {
        return stars.Where(s =>
        {
            double dx = s.X - x;
            double dy = s.Y - y;
            double dz = s.Z - z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz) <= radius;
        }).ToList();
    }
    
    /// <summary>
    /// Get all stars (for integration with chunk system)
    /// </summary>
    public List<RealStar> GetAllStars()
    {
        return stars;
    }
    
    /// <summary>
    /// Parse a CSV line handling quoted fields
    /// </summary>
    private List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        var currentField = new System.Text.StringBuilder();
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }
        
        // Add the last field
        fields.Add(currentField.ToString());
        
        return fields;
    }
    
    /// <summary>
    /// Safely parse a double value
    /// </summary>
    private double ParseDouble(string value)
    {
        value = value.Trim();
        
        // Remove any non-numeric suffixes (like units)
        value = System.Text.RegularExpressions.Regex.Replace(value, @"[^0-9\.\-\+eE]", "");
        
        if (string.IsNullOrEmpty(value))
            return 0.0;
            
        if (double.TryParse(value, out double result))
            return result;
            
        return 0.0;
    }
    
    /// <summary>
    /// Map spectral type string to a valid spectral type
    /// </summary>
    private string MapSpectralType(string spectralType)
    {
        if (string.IsNullOrEmpty(spectralType))
            return "G5V";
            
        spectralType = spectralType.ToUpper().Trim();
        
        // Remove any reference markers or special characters
        spectralType = System.Text.RegularExpressions.Regex.Replace(spectralType, @"[\[\]\$\?\!\*]", "");
        
        // Handle white dwarfs
        if (spectralType.Contains("D") && (spectralType.StartsWith("D") || spectralType.Contains("WD")))
        {
            if (spectralType.Contains("DA")) return "DA";
            if (spectralType.Contains("DB")) return "DB";
            if (spectralType.Contains("DC")) return "DC";
            if (spectralType.Contains("DO")) return "DO";
            if (spectralType.Contains("DQ")) return "DQ";
            if (spectralType.Contains("DZ")) return "DZ";
            if (spectralType.Contains("DX")) return "DX";
            return "DA"; // Default white dwarf
        }
        
        // Handle brown dwarfs
        if (spectralType.StartsWith("L")) return "L0";
        if (spectralType.StartsWith("T")) return "T0";
        if (spectralType.StartsWith("Y")) return "Y0";
        
        // Handle main sequence stars
        char firstChar = spectralType.Length > 0 ? spectralType[0] : 'G';
        
        switch (firstChar)
        {
            case 'O': return "O5V";
            case 'B': return "B5V";
            case 'A': return "A5V";
            case 'F': return "F5V";
            case 'G': return "G5V";
            case 'K': 
                if (spectralType.Contains("III")) return "K0III";
                return "K5V";
            case 'M':
                if (spectralType.Contains("III")) return "M0III";
                return "M5V";
            default:
                // Try to handle special cases or default
                if (spectralType.Contains("SUBDWARF") || spectralType.StartsWith("SD"))
                    return "M5V"; // Subdwarfs mapped to M dwarfs
                return "G5V"; // Default
        }
    }
    
    /// <summary>
    /// Convert spectral type string to StellarTypeGenerator enum
    /// </summary>
    public static StellarTypeGenerator.StellarType ConvertSpectralType(string spectralType)
    {
        // First map to a valid spectral type
        var instance = new RealStellarData();
        spectralType = instance.MapSpectralType(spectralType);
        
        // Now convert to enum
        if (spectralType == "O5V") return StellarTypeGenerator.StellarType.O5V;
        if (spectralType == "B5V") return StellarTypeGenerator.StellarType.B0V;
        if (spectralType == "A5V") return StellarTypeGenerator.StellarType.A0V;
        if (spectralType == "F5V") return StellarTypeGenerator.StellarType.F0V;
        if (spectralType == "G5V") return StellarTypeGenerator.StellarType.G5V;
        if (spectralType == "K5V") return StellarTypeGenerator.StellarType.K0V;
        if (spectralType == "K0III") return StellarTypeGenerator.StellarType.K0III;
        if (spectralType == "M5V") return StellarTypeGenerator.StellarType.M0V;
        if (spectralType == "M0III") return StellarTypeGenerator.StellarType.M0III;
        if (spectralType == "L0") return StellarTypeGenerator.StellarType.L0;
        if (spectralType == "T0") return StellarTypeGenerator.StellarType.T0;
        if (spectralType == "Y0") return StellarTypeGenerator.StellarType.Y0;
        if (spectralType.StartsWith("D")) return StellarTypeGenerator.StellarType.DA;
        
        // Default
        return StellarTypeGenerator.StellarType.G5V;
    }
    
    /// <summary>
    /// Get total planet count across all stars
    /// </summary>
    public int GetTotalPlanetCount()
    {
        return stars.Sum(s => s.Planets.Count);
    }
    
    /// <summary>
    /// Group companion stars (B, C, etc.) with their primary (A) stars
    /// </summary>
    private void GroupCompanionStars()
    {
        var starsBySystem = new Dictionary<string, List<RealStar>>();
        
        // First pass: group stars by base system name
        foreach (var star in stars)
        {
            string baseName = GetBaseSystemName(star.SystemName);
            
            // Special case: Proxima Centauri belongs to Alpha Centauri system
            if (star.SystemName.Contains("Proxima") && star.SystemName.Contains("Centauri"))
            {
                baseName = "Alpha Centauri";
            }
            
            if (!starsBySystem.ContainsKey(baseName))
            {
                starsBySystem[baseName] = new List<RealStar>();
            }
            starsBySystem[baseName].Add(star);
        }
        
        // Second pass: link companions to primaries
        foreach (var systemGroup in starsBySystem.Values)
        {
            if (systemGroup.Count > 1)
            {
                // Find the primary star (usually A, or the one without a letter designation)
                RealStar? primary = null;
                
                // First try to find an 'A' component
                primary = systemGroup.FirstOrDefault(s => s.SystemName.Contains(" A") || s.SystemName.EndsWith(" A"));
                
                // If no 'A', use the one without any letter designation
                if (primary == null)
                {
                    primary = systemGroup.FirstOrDefault(s => !IsCompanionDesignation(s.SystemName));
                }
                
                // If still none, use the first one
                if (primary == null)
                {
                    primary = systemGroup[0];
                }
                
                // Add all others as companions to the primary
                foreach (var star in systemGroup)
                {
                    if (star != primary)
                    {
                        primary.CompanionStars.Add(star);
                        
                        // Keep companion stars accessible by their base name for planet assignment
                        // For example, "Proxima Centauri" should find the actual Proxima Centauri star object
                        string companionBaseName = GetBaseSystemName(star.SystemName);
                        if (!starLookup.ContainsKey(companionBaseName))
                        {
                            starLookup[companionBaseName] = star;
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Get the base system name (without component designation)
    /// </summary>
    private string GetBaseSystemName(string fullName)
    {
        // Remove component designations like " A", " B", etc.
        string baseName = fullName;
        
        // Remove trailing component letters
        baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"\s+[A-Z]$", "");
        
        // Also handle cases like "Sirius A" -> "Sirius"
        if (baseName.Contains(" "))
        {
            var parts = baseName.Split(' ');
            if (parts.Length >= 2 && parts[parts.Length - 1].Length == 1 && char.IsUpper(parts[parts.Length - 1][0]))
            {
                baseName = string.Join(" ", parts.Take(parts.Length - 1));
            }
        }
        
        return baseName;
    }
    
    /// <summary>
    /// Check if a star name has a companion designation (B, C, etc.)
    /// </summary>
    private bool IsCompanionDesignation(string starName)
    {
        // Check for component letters at the end
        if (System.Text.RegularExpressions.Regex.IsMatch(starName, @"\s+[B-Z]$"))
            return true;
            
        // Check for component in the middle like "Sirius B"
        var parts = starName.Split(' ');
        if (parts.Length >= 2)
        {
            var lastPart = parts[parts.Length - 1];
            if (lastPart.Length == 1 && lastPart[0] >= 'B' && lastPart[0] <= 'Z')
                return true;
        }
        
        return false;
    }
}