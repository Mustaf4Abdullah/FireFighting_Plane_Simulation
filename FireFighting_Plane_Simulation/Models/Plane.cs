namespace FireFighting_Plane_Simulation.Models
{
    public class Plane
    {
        public int Fuel { get; set; } = 5000; // Initial fuel (liters)
        public int Water { get; set; } = 20000; // Initial water (liters)
        public string CurrentRegion { get; set; } // Current region name
    }

}
