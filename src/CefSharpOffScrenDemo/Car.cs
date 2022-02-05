using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefSharpOffScrenDemo
{
    public class Car
    {
        public string url { get; set; }
        public string title { get; set; }
        public string mileage { get; set; }
        public string price { get; set; }
        public string[] images { get; set; }
        public CarDetail Detail { get; set; }
    }

    public class CarDetail
    {
        public string ExteriorColor { get; set; }
        public string InteriorColor { get; set; }
        public string DriveTrain { get; set; }
        public string MPG { get; set; }
        public string FuelType { get; set; }
        public string Transmission { get; set; }
        public string Engine { get; set; }
        public string VIN { get; set; }
        public string Stock { get; set; }
    }
}
