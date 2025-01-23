using FireFighting_Plane_Simulation.Helpers;
using FireFighting_Plane_Simulation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json; // Add this namespace


namespace FireFighting_Plane_Simulation.Controllers
{
    public class SimulationController : Controller
    {
        private List<Region> _regions;
        private List<FireFighting_Plane_Simulation.Models.Path> _routes;

        private readonly Result _result;
        private readonly Plane _plane;
        private readonly ILogger<SimulationController> _logger;
        private List<string> ReturnToBaseList = new List<string>(); // Track used routes
        private List<string> VisitedRoutes = new List<string>();



        public SimulationController(ILogger<SimulationController> logger)
        {
            _logger = logger;
            _result = new Result();
            _plane = new Plane { CurrentRegion = "B0", Fuel = 5000, Water = 20000 };
        }

        public IActionResult UploadFile()
        {
            return View();
        }
        public IActionResult VisualSimulation()
        {
            if (TempData["Regions"] == null || TempData["Routes"] == null || TempData["Result"] == null)
            {
                return RedirectToAction("UploadFile"); // Redirect to file upload if data is missing
            }

            // Deserialize the TempData values
            var regions = JsonSerializer.Deserialize<List<Region>>(TempData["Regions"].ToString());
            var routes = JsonSerializer.Deserialize<List<FireFighting_Plane_Simulation.Models.Path>>(TempData["Routes"].ToString());
            var result = JsonSerializer.Deserialize<Result>(TempData["Result"].ToString());

            // Prepare the ViewModel
            var viewModel = new SimulationViewModel
            {
                Regions = regions,
                Routes = routes
            };

            ViewBag.Result = result; // Pass result to the view
            return View(viewModel);
        }


        [HttpPost]
        public IActionResult UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewData["Message"] = "No file was uploaded.";
                return View();
            }

            List<string> lines = new List<string>();

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine());
                }
            }

            var viewModel = ProcessData(lines);

            // Pass data via ViewBag (if needed for debugging or temporary usage)
            ViewBag.Regions = viewModel.Regions;
            ViewBag.Routes = viewModel.Routes;
            TempData["Regions"] = JsonSerializer.Serialize(viewModel.Regions);
            TempData["Routes"] = JsonSerializer.Serialize(viewModel.Routes);

            ViewData["Message"] = "File processed successfully.";
            return View("StartSimulation", viewModel);
        }

        private SimulationViewModel ProcessData(List<string> lines)
        {
            int regionCount = lines.Count;

            var regions = new List<Region>();
            var routes = new List<FireFighting_Plane_Simulation.Models.Path>();

            // Hardcoded fire severity values for demonstration purposes
            var hardcodedSeverities = new Dictionary<string, int>
    {
        {"B1", 1}, {"B2", 3}, {"B3", 2}, {"B4", 5}, {"B5", 4},
        {"B6", 5}, {"B7", 2}, {"B8", 1}, {"B9", 4}, {"B10", 3},
        {"B11", 5}, {"B12", 2}, {"B13", 1}
    };

            // Step 1: Define Regions (B0 as the start point, others with fire severities)
            for (int i = 0; i < regionCount; i++)
            {
                string regionName = i == 0 ? "B0" : $"B{i}";
                regions.Add(new Region
                {
                    Name = regionName,
                    Severity = regionName == "B0" ? 0 : (hardcodedSeverities.ContainsKey(regionName) ? hardcodedSeverities[regionName] : 0)
                });
            }

            for (int i = 0; i < regionCount; i++)
            {
                var distances = lines[i].Split(',').Select(int.Parse).ToArray();
                for (int j = 0; j < distances.Length; j++)
                {
                    if (distances[j] > 0)
                    {
                        routes.Add(new FireFighting_Plane_Simulation.Models.Path
                        {
                            FromRegion = regions[i].Name,
                            ToRegion = regions[j].Name,
                            Distance = distances[j],
                            FuelRequired = distances[j] * 10,
                            TimeRequired = distances[j]
                        });
                    }
                }
            }

            /*
            var severityLines = lines.Skip(regionCount); // dosyada matrixi gecip yangin siddetine gel
            foreach (var line in severityLines)
            {
                var parts = line.Split(' ');
                if (parts.Length == 2)
                {
                    string regionName = parts[0];
                    if (int.TryParse(parts[1], out int severity))
                    {
                        var region = regions.FirstOrDefault(r => r.Name == regionName);
                        if (region != null) region.Severity = severity;
                    }
                }
            }
            */

            return new SimulationViewModel
            {
                Regions = regions,
                Routes = routes
            };
            TempData["Regions"] = JsonSerializer.Serialize(_regions);

        }
        public IActionResult StartSimulation(SimulationViewModel viewModel)
        {
            _regions = viewModel.Regions; // Use passed regions
            _routes = viewModel.Routes;

            while (_regions.Any(r => r.Severity > 0))
            {
                var remainingRegions01 = _regions
                    .Where(r => r.Name != "B0") // Exclude the base region
                    .ToDictionary(r => r.Name, r => r);
                // Find a list of regions that can be visited
                var remainingRegions = _regions
                    .Where(r => r.Name != "B0") // Exclude the base region
                    .ToDictionary(r => r.Name, r => r);
                var visitableRegions = RouterHelper.FindReachableRegions(_plane, _routes, _plane.CurrentRegion, remainingRegions01);
                var reversedRegions = visitableRegions.AsEnumerable().Reverse();
                foreach (var region in reversedRegions)
                {
                    ReturnToBaseList.Add(region.Name); // Assuming ReturnToBaseList is the target list
                }

                if (visitableRegions == null || !visitableRegions.Any())
                {
                    // No feasible regions to visit with current fuel and water, return to base
                    ReturnToBase();
                    continue;
                }

                // Traverse the visitable regions
                foreach (var regionName in visitableRegions)
                {
                    var nextRegion = remainingRegions[regionName.Name];
                    TravelToRegion(_plane.CurrentRegion, nextRegion.Name);
                    FightFire(nextRegion);

                    // Remove region if severity is 0
                    if (nextRegion.Severity == 0)
                    {
                        _regions.Remove(nextRegion);
                    }

                    // Break if resources are insufficient to continue
                    
                }
                ReturnToBase();
            }

            SaveResult();
         //   TempData["Regions"] = JsonSerializer.Serialize(_regions);
            TempData["Routes"] = JsonSerializer.Serialize(_routes);
            TempData["Result"] = JsonSerializer.Serialize(_result);
            return View("SimulationResult", _result);
        }

        private void TravelToRegion(string fromRegion, string toRegion)
        {
            var directRoute = _routes.FirstOrDefault(r => r.FromRegion == fromRegion && r.ToRegion == toRegion);
            if (directRoute != null)
            {
                _plane.Fuel -= directRoute.FuelRequired;
                _result.TotalFuelUsed += directRoute.FuelRequired;
                _result.TotalTime += directRoute.TimeRequired;
                _result.UsedRoutes.Add($"{fromRegion} -> {toRegion}");
                VisitedRoutes.Add($"{toRegion} -> {fromRegion}"); // Track route
                _result.TotalDistance += directRoute.Distance;
                _plane.CurrentRegion = toRegion;
                return;
            }

            var dijkstraResult = RouterHelper.DijkstraAlgorithm(fromRegion, toRegion, _routes);
            if (dijkstraResult == null)
            {
                _logger.LogInformation($"No path exists from {fromRegion} to {toRegion}.");
                return;
            }

            var (distance, path) = dijkstraResult.Value;
            for (int i = 0; i < path.Count - 1; i++)
            {
                string intermediateFrom = path[i];
                string intermediateTo = path[i + 1];

                var route = _routes.FirstOrDefault(r => r.FromRegion == intermediateFrom && r.ToRegion == intermediateTo);
                if (route == null)
                {
                    _logger.LogError($"Unexpected missing route between {intermediateFrom} and {intermediateTo}.");
                    return;
                }

                _plane.Fuel -= route.FuelRequired;
                _result.TotalFuelUsed += route.FuelRequired;
                _result.TotalTime += route.TimeRequired;
                _result.UsedRoutes.Add($"{intermediateFrom} -> {intermediateTo}");
                VisitedRoutes.Add($"{intermediateTo} -> {intermediateFrom}"); // Track route
                _result.TotalDistance += route.Distance;
                _plane.CurrentRegion = intermediateTo;
            }
        }


        private void ReturnToBase()
        {
            // Reverse the routes and log them
            var reversedRoutes = VisitedRoutes.AsEnumerable().Reverse();
            foreach (var route in reversedRoutes)
            {
                _result.UsedRoutes.Add(route+"back"); // Log the return path
            }

            // Clear the VisitedRoutes list
            VisitedRoutes.Clear();

            string logEntry = $"Refill Log: Region: {_plane.CurrentRegion}, Fuel: {_plane.Fuel}, Water: {_plane.Water}";
            _result.RefillLog.Add(logEntry);

            RefuelAndReload();
            _plane.CurrentRegion = "B0"; // Reset to base
        }

        private void FightFire(Region region)
        {
            while (region.Severity > 0)
            {
               

                int extinguishTime = 10; // Time per severity level
                int waterConsumed =  1000; // Water per severity level
                int fuelConsumed = 100; // Fuel consumption during extinguishing

                _plane.Fuel -= fuelConsumed;
                _plane.Water -= waterConsumed;
                _result.TotalFuelUsed += fuelConsumed;
                _result.TotalWaterUsed += waterConsumed;
                _result.TotalTime += extinguishTime;

                region.Severity -= 1; // Reduce severity

                
            }           

            _result.VisitedRegions.Add(region.Name);
        }
        private void RefuelAndReload()
        {
            _plane.Fuel = 5000;
            _plane.Water = 20000;
            _result.RefillCount++;
        }

        private void SaveResult()
        {
            var resultContent = $"Simulation Results:\n" +
                                $"Total Time: {_result.TotalTime} minutes\n" +
                                $"Total Fuel Used: {_result.TotalFuelUsed} liters\n" +
                                $"Total Water Used: {_result.TotalWaterUsed} liters\n" +
                                $"Total Distance: {_result.TotalDistance} km\n" +
                                $"Refill Count: {_result.RefillCount}\n" +
                                $"Visited Regions: {string.Join(", ", _result.VisitedRegions)}\n" +
                                $"Used Routes: {string.Join(", ", _result.UsedRoutes)}\n\n" +
                                $"Refill Logs:\n{string.Join("\n", _result.RefillLog)}"; // Append all logs

            System.IO.File.WriteAllText(@"C:\Users\mustafa\OneDrive\Masaüstü\SimulationResults.txt", resultContent);
        }

    }
}