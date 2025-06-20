using System;
using System.Collections.Generic;
using System.Linq;
using MilkyWay.Core;

namespace MilkyWay.Utils
{
    internal static class Converter
    {
        private static StellarTypeGenerator.StellarType ConvertToScientificType(StellarTypeGenerator.StellarType type)
        {
            return type switch
            {
                StellarTypeGenerator.StellarType.O5V => StellarTypeGenerator.StellarType.O5V,
                StellarTypeGenerator.StellarType.B0V => StellarTypeGenerator.StellarType.B0V,
                StellarTypeGenerator.StellarType.B5V => StellarTypeGenerator.StellarType.B5V,
                StellarTypeGenerator.StellarType.A0V => StellarTypeGenerator.StellarType.A0V,
                StellarTypeGenerator.StellarType.A5V => StellarTypeGenerator.StellarType.A5V,
                StellarTypeGenerator.StellarType.F0V => StellarTypeGenerator.StellarType.F0V,
                StellarTypeGenerator.StellarType.F5V => StellarTypeGenerator.StellarType.F5V,
                StellarTypeGenerator.StellarType.G0V => StellarTypeGenerator.StellarType.G0V,
                StellarTypeGenerator.StellarType.G5V => StellarTypeGenerator.StellarType.G5V,
                StellarTypeGenerator.StellarType.K0V => StellarTypeGenerator.StellarType.K0V,
                StellarTypeGenerator.StellarType.K5V => StellarTypeGenerator.StellarType.K5V,
                StellarTypeGenerator.StellarType.M0V => StellarTypeGenerator.StellarType.M0V,
                StellarTypeGenerator.StellarType.M5V => StellarTypeGenerator.StellarType.M5V,
                StellarTypeGenerator.StellarType.M8V => StellarTypeGenerator.StellarType.M8V,

                // Brown dwarfs
                StellarTypeGenerator.StellarType.L0 => StellarTypeGenerator.StellarType.L0,
                StellarTypeGenerator.StellarType.L5 => StellarTypeGenerator.StellarType.L5,
                StellarTypeGenerator.StellarType.T0 => StellarTypeGenerator.StellarType.T0,
                StellarTypeGenerator.StellarType.T5 => StellarTypeGenerator.StellarType.T5,
                StellarTypeGenerator.StellarType.Y0 => StellarTypeGenerator.StellarType.Y0,

                // Giants
                StellarTypeGenerator.StellarType.G5III => StellarTypeGenerator.StellarType.G5III,
                StellarTypeGenerator.StellarType.K0III => StellarTypeGenerator.StellarType.K0III,
                StellarTypeGenerator.StellarType.K5III => StellarTypeGenerator.StellarType.K5III,
                StellarTypeGenerator.StellarType.M0III => StellarTypeGenerator.StellarType.M0III,
                StellarTypeGenerator.StellarType.B0III => StellarTypeGenerator.StellarType.B0III,

                // Supergiants
                StellarTypeGenerator.StellarType.M2I => StellarTypeGenerator.StellarType.M2I,
                StellarTypeGenerator.StellarType.B0I => StellarTypeGenerator.StellarType.B0I,

                // Compact objects
                StellarTypeGenerator.StellarType.DA => StellarTypeGenerator.StellarType.DA,
                StellarTypeGenerator.StellarType.NS => StellarTypeGenerator.StellarType.NS,
                StellarTypeGenerator.StellarType.BH => StellarTypeGenerator.StellarType.BH,
                StellarTypeGenerator.StellarType.QS => StellarTypeGenerator.StellarType.QS,
                StellarTypeGenerator.StellarType.SMBH => StellarTypeGenerator.StellarType.SMBH,

                _ => StellarTypeGenerator.StellarType.G5V
            };
        }
        static UnifiedSystemGenerator.StarSystem ConvertRealStarToSystem(Star star, UnifiedSystemGenerator unifiedGen)
        {
            var realData = star.RealStarData!;

            // Create primary star
            var primaryStar = new UnifiedSystemGenerator.Star
            {
                Id = "A",
                Name = realData.SystemName.EndsWith(" A") ? realData.SystemName : $"{realData.SystemName} A",
                Type = UnifiedSystemGenerator.ObjectType.Star,
                Mass = star.Mass,
                StellarType = ConvertToScientificType(star.Type),
                Temperature = star.Temperature,
                Luminosity = star.Luminosity,
                Relationship = UnifiedSystemGenerator.StarRelationship.Primary
            };

            // Create the system
            var system = new UnifiedSystemGenerator.StarSystem
            {
                Seed = star.Seed,
                PrimaryStar = primaryStar
            };
            system.AllStars.Add(primaryStar);

            // Add companion stars
            int compIndex = 1;
            foreach (var companion in realData.CompanionStars)
            {
                var compId = ((char)('A' + compIndex)).ToString();
                var compStar = new UnifiedSystemGenerator.Star
                {
                    Id = compId,
                    Name = companion.SystemName,
                    Type = UnifiedSystemGenerator.ObjectType.Star,
                    Mass = (float)companion.Mass,
                    StellarType = ConvertToScientificType(RealStellarData.ConvertSpectralType(companion.Type)),
                    Temperature = (float)companion.Temperature,
                    Luminosity = (float)companion.Luminosity,
                    Relationship = UnifiedSystemGenerator.StarRelationship.Binary,
                    Parent = primaryStar,
                    OrbitalDistance = 0.1 * (compIndex + 1) // Approximate orbital distance
                };

                // For binary companions, set as BinaryCompanion instead of adding to children
                if (compIndex == 1 && realData.CompanionStars.Count == 1)
                {
                    primaryStar.BinaryCompanion = compStar;
                }
                else
                {
                    // Multiple companions are satellites
                    compStar.Relationship = UnifiedSystemGenerator.StarRelationship.Satellite;
                }

                system.AllStars.Add(compStar);
                compIndex++;
            }

            // Check if we have real planet data
            if (realData.Planets.Count > 0)
            {
                // Add real planets to PRIMARY star
                int planetIndex = 1;
                foreach (var planet in realData.Planets)
                {
                    var planetObj = new UnifiedSystemGenerator.Planet
                    {
                        Id = planetIndex.ToString(),
                        Name = planet.Name,
                        Type = UnifiedSystemGenerator.ObjectType.Planet,
                        Mass = (float)planet.Mass * (planet.MassUnit == "Jupiter" ? 317.8f : 1.0f), // Convert to Earth masses
                        Radius = (float)planet.Radius * (planet.RadiusUnit == "Jupiter" ? 11.21f : 1.0f), // Convert to Earth radii
                        Parent = primaryStar,
                        OrbitalDistance = planet.SemiMajorAxis,
                        OrbitalPeriod = planet.OrbitalPeriod, // Already in days
                        PlanetType = ConvertPlanetType(planet.PlanetType)
                    };

                    // Add moons if any
                    int moonIndex = 0;
                    foreach (var moon in planet.Moons)
                    {
                        var moonId = ((char)('a' + moonIndex)).ToString();
                        var moonObj = new UnifiedSystemGenerator.Moon
                        {
                            Id = moonId,
                            Name = moon.Name,
                            Type = UnifiedSystemGenerator.ObjectType.Moon,
                            Mass = (float)moon.Mass * 0.0123f, // Convert to Earth masses (Moon = 0.0123 Earth masses)
                            Radius = (float)moon.Radius * 0.273f, // Convert to Earth radii (Moon = 0.273 Earth radii)
                            Parent = planetObj,
                            OrbitalDistance = moon.SemiMajorAxis / 384400.0, // Convert km to lunar distances
                            OrbitalPeriod = moon.OrbitalPeriod // Already in days
                        };
                        planetObj.Children.Add(moonObj);
                        moonIndex++;
                    }

                    primaryStar.Children.Add(planetObj);
                    planetIndex++;
                }
            }

            // Add planets for COMPANION stars
            foreach (var companion in realData.CompanionStars)
            {
                if (companion.Planets.Count > 0)
                {
                    // Find the corresponding star in our system
                    var companionStar = system.AllStars.FirstOrDefault(s => s.Name == companion.SystemName);
                    if (companionStar != null)
                    {
                        int planetIndex = 1;
                        foreach (var planet in companion.Planets)
                        {
                            var planetObj = new UnifiedSystemGenerator.Planet
                            {
                                Id = planetIndex.ToString(),
                                Name = planet.Name,
                                Type = UnifiedSystemGenerator.ObjectType.Planet,
                                Mass = (float)planet.Mass * (planet.MassUnit == "Jupiter" ? 317.8f : 1.0f),
                                Radius = (float)planet.Radius * (planet.RadiusUnit == "Jupiter" ? 11.21f : 1.0f),
                                Parent = companionStar,
                                OrbitalDistance = planet.SemiMajorAxis,
                                OrbitalPeriod = planet.OrbitalPeriod,
                                PlanetType = ConvertPlanetType(planet.PlanetType)
                            };

                            // Add moons if any
                            int moonIndex = 0;
                            foreach (var moon in planet.Moons)
                            {
                                var moonId = ((char)('a' + moonIndex)).ToString();
                                var moonObj = new UnifiedSystemGenerator.Moon
                                {
                                    Id = moonId,
                                    Name = moon.Name,
                                    Type = UnifiedSystemGenerator.ObjectType.Moon,
                                    Mass = (float)moon.Mass * 0.0123f,
                                    Radius = (float)moon.Radius * 0.273f,
                                    Parent = planetObj,
                                    OrbitalDistance = moon.SemiMajorAxis / 384400.0,
                                    OrbitalPeriod = moon.OrbitalPeriod
                                };
                                planetObj.Children.Add(moonObj);
                                moonIndex++;
                            }

                            companionStar.Children.Add(planetObj);
                            planetIndex++;
                        }
                    }
                }
            }

            if (realData.Planets.Count == 0 && realData.CompanionStars.All(c => c.Planets.Count == 0))
            {
                // No real planet data - use procedural generation
                var tempSystem = unifiedGen.GenerateSystem(star.Seed, ConvertToScientificType(star.Type), star.Mass,
                    star.Temperature, star.Luminosity);

                // Copy the generated planets to our real star
                foreach (var child in tempSystem.PrimaryStar.Children)
                {
                    if (child is UnifiedSystemGenerator.Planet)
                    {
                        child.Parent = primaryStar;
                        primaryStar.Children.Add(child);
                    }
                }
            }

            // Sort children by orbital distance
            primaryStar.Children = primaryStar.Children.OrderBy(c => c.OrbitalDistance).ToList();

            return system;
        }
    static UnifiedSystemGenerator.PlanetType ConvertPlanetType(string type)
    {
        return type.ToLower() switch
        {
            "terrestrial" => UnifiedSystemGenerator.PlanetType.Terra,
            "gas giant" => UnifiedSystemGenerator.PlanetType.Jupiter,
            "ice giant" => UnifiedSystemGenerator.PlanetType.Neptune,
            "ocean" => UnifiedSystemGenerator.PlanetType.Aquaria,
            "rocky" => UnifiedSystemGenerator.PlanetType.Selena,
            _ => UnifiedSystemGenerator.PlanetType.Terra
        };
    }
    }
}