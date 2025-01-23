using FireFighting_Plane_Simulation.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FireFighting_Plane_Simulation.Helpers
{
    public static class RouterHelper
    {
        public static List<Region> FindReachableRegions(
            Plane plane,
            List<FireFighting_Plane_Simulation.Models.Path> routes,
            string currentRegion,
            Dictionary<string, Region> remainingRegions)
        {
            var reachableRegions = FindRegionVisitSequence(currentRegion, routes, plane, remainingRegions);
            return reachableRegions;
        }

        private static List<Region> FindRegionVisitSequence(
            string startRegion,
            List<FireFighting_Plane_Simulation.Models.Path> routes,
            Plane plane,
            Dictionary<string, Region> remainingRegions)
        {
            var visitedRegions = new List<Region>();
            var availableFuel = plane.Fuel;
            var availableWater = plane.Water;
            var currentRegion = startRegion;

            while (true)
            {
                var nextRegionData = FindNextBestRegion(currentRegion, routes, plane, remainingRegions, availableFuel, availableWater);

                if (nextRegionData == null) break;

                var (nextRegionName, fuelUsed, waterUsed) = nextRegionData.Value;

                availableFuel -= fuelUsed;
                availableWater -= waterUsed;
                currentRegion = nextRegionName;

                var nextRegion = remainingRegions[nextRegionName];
                visitedRegions.Add(nextRegion);
                remainingRegions.Remove(nextRegionName);
            }

            return visitedRegions;
        }

        public static (string NextRegion, int FuelUsed, int WaterUsed)? FindNextBestRegion(
    string currentRegion,
    List<FireFighting_Plane_Simulation.Models.Path> routes,
    Plane plane,
    Dictionary<string, Region> remainingRegions,
    int availableFuel,
    int availableWater)
        {
            (string NextRegion, int FuelUsed, int WaterUsed)? bestRegion = null;
            int bestPriority = int.MaxValue;

            bool noDirectRouteAvailable = true;

            foreach (var region in remainingRegions.Values)
            {
                var route = routes.FirstOrDefault(r => r.FromRegion == currentRegion && r.ToRegion == region.Name);

                if (route == null)
                {
                    continue; // Skip if no direct route exists
                }
                else
                {
                    noDirectRouteAvailable = false; // A direct route exists
                }

                int fuelRequired = route.FuelRequired + region.Severity * 100;
                int waterRequired = region.Severity * 1000;

                if (fuelRequired > availableFuel || waterRequired > availableWater) continue;

                int priority = route.Distance + region.Severity;

                if (priority < bestPriority)
                {
                    bestPriority = priority;
                    bestRegion = (region.Name, fuelRequired, waterRequired);
                }
            }

            // If no direct routes are available and the plane is at B0, use Dijkstra
            if (noDirectRouteAvailable && currentRegion == "B0")
            {
                foreach (var region in remainingRegions.Values)
                {
                    var dijkstraResult = DijkstraAlgorithm(currentRegion, region.Name, routes);

                    if (dijkstraResult.HasValue)
                    {
                        var dijkstraData = dijkstraResult.Value;
                        int fuelRequired = dijkstraData.Distance * 10 + region.Severity * 100;
                        int waterRequired = region.Severity * 1000;

                        if (fuelRequired > availableFuel || waterRequired > availableWater) continue;

                        int priority = dijkstraData.Distance + region.Severity;

                        if (priority < bestPriority)
                        {
                            bestPriority = priority;
                            bestRegion = (region.Name, fuelRequired, waterRequired);
                        }
                    }
                }
            }

            return bestRegion;
        }



        public static (int Distance, List<string> Path)? DijkstraAlgorithm(
            string startRegion,
            string targetRegion,
            List<FireFighting_Plane_Simulation.Models.Path> routes)
        {
            var distances = new Dictionary<string, int>();
            var previous = new Dictionary<string, string>();
            var unvisited = new HashSet<string>();

            foreach (var path in routes.SelectMany(r => new[] { r.FromRegion, r.ToRegion }).Distinct())
            {
                distances[path] = int.MaxValue;
                unvisited.Add(path);
            }

            distances[startRegion] = 0;

            while (unvisited.Count > 0)
            {
                var current = unvisited.OrderBy(region => distances[region]).First();
                unvisited.Remove(current);

                if (current == targetRegion)
                {
                    var path = new List<string>();
                    while (current != null)
                    {
                        path.Add(current);
                        previous.TryGetValue(current, out current);
                    }
                    path.Reverse();

                    return (distances[targetRegion], path);
                }

                var neighbors = routes
                    .Where(r => r.FromRegion == current && unvisited.Contains(r.ToRegion))
                    .ToList();

                foreach (var neighbor in neighbors)
                {
                    int newDist = distances[current] + neighbor.Distance;
                    if (newDist < distances[neighbor.ToRegion])
                    {
                        distances[neighbor.ToRegion] = newDist;
                        previous[neighbor.ToRegion] = current;
                    }
                }
            }

            return null; // No path found
        }
    }
}
