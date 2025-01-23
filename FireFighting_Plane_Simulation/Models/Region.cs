namespace FireFighting_Plane_Simulation.Models
{
    public class Region
    {

        public string Name { get; set; }
        public int Severity { get; set; }
        public int TimeRequired => Severity * 10; // Minutes
        public int WaterRequired => Severity * 1000; // Liters
        public int FuelRequired => Severity * 100; // Liters

    }

    


}
