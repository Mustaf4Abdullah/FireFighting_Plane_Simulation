namespace FireFighting_Plane_Simulation.Models
{
    public class Result
    {
        public double TotalTime { get; set; } // Total time used in minutes
        public double TotalWaterUsed { get; set; } // Total water used in liters
        public double TotalFuelUsed { get; set; } // Total fuel used in liters
        public double TotalDistance { get; set; } // Total distance traveled in km
        public int RefillCount { get; set; } // Number of times refueled
        public List<string> VisitedRegions { get; set; } = new List<string>(); // List of visited regions
        public List<string> UsedRoutes { get; set; } = new List<string>(); // List of used routes
        public List<string> RefillLog { get; set; } = new List<string>();


    }

}
