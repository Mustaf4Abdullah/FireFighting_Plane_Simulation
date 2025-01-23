
    namespace FireFighting_Plane_Simulation.Models
    {
        public class Path
        {

            public string FromRegion { get; set; }
            public string ToRegion { get; set; }
            public int Distance { get; set; } // Distance in km
            public int FuelRequired { get; set; } // Fuel required for this route (in liters)
            public double TimeRequired { get; set; } // Time required for this route (in minutes)
    }
    }


