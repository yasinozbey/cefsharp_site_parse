using CefSharp;
using CefSharp.OffScreen;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefSharpOffScrenDemo
{
    class Program
    {
        static List<Car> CarList = new List<Car>();
        public static void Main(string[] args)
        {
            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
            string cachePath = Path.Combine(Environment.GetFolderPath(
                                         Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache");

            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, true);

            var settings = new CefSettings()
            {
                CachePath = cachePath
            };
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);


            SiteParser parser = new SiteParser();
            parser.OnCarParse += Parser_OnCarParse;
            parser.OnCarDetailParse += Parser_OnCarDetailParse;
            parser.ParseCars(SiteParser.Brands.Tesla, SiteParser.Models.ModelS);
            parser.ParseCars(SiteParser.Brands.Tesla, SiteParser.Models.ModelX);

            parser.ParseCarDetail(CarList[4].url);
            parser.ParseCarDetail(CarList[5].url);
            parser.ParseCarDetail(CarList[6].url);


            string json = JsonConvert.SerializeObject(CarList);
            File.WriteAllText("cars.json", json);

            Console.WriteLine("");
            Console.WriteLine("Parse işlemi tamamlandı!");



            Console.ReadKey();
            Cef.Shutdown();
        }


        private static void Parser_OnCarParse(object sender, List<Car> cars)
        {
            cars.ForEach(car =>
            {
                if (!CarList.Where(x => x.url == car.url).Any())
                    CarList.Add(car);
            });
        }

        private static void Parser_OnCarDetailParse(object sender, CarDetail detail, string url)
        {
            var car = CarList.Where(x => x.url == url).FirstOrDefault();
            if (car != null)
            {
                car.Detail = detail;
            }
        }

    }
}
