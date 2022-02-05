using CefSharp;
using CefSharp.OffScreen;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CefSharpOffScrenDemo
{
    public class SiteParser
    {
        ChromiumWebBrowser browser;
        private bool isLoggedIn;
        private bool isDetailPageLoaded;
        private const string mainPageUrl = "https://www.cars.com/";
        public enum Brands : byte
        {
            Tesla = 27
        }

        public enum Models : byte
        {
            ModelS = 2,
            ModelX = 3
        }



        public delegate void CarParseHandler(object sender, List<Car> cars);
        public event CarParseHandler OnCarParse;

        public delegate void CarDetailParseHandler(object sender, CarDetail detail, string url);
        public event CarDetailParseHandler OnCarDetailParse;


        public void ParseCars(Brands brand, Models model)
        {
            browser = new ChromiumWebBrowser(mainPageUrl);

            Thread.Sleep(10000); //main page loading

            Retry.Do(() => { gotoSignin(); }, TimeSpan.FromSeconds(10));


            if (isLoggedIn)
            {
                Retry.Do(() =>
                {
                    searchCars(brand, model);
                }, TimeSpan.FromSeconds(10));

                Retry.Do(() =>
                {
                    parseListPage();
                }, TimeSpan.FromSeconds(5));

                Retry.Do(() =>
                {
                    navSecondPage();
                }, TimeSpan.FromSeconds(10));

                Retry.Do(() =>
                {
                    parseListPage();
                }, TimeSpan.FromSeconds(5));
            }
        }

        public void ParseCarDetail(string url)
        {
            if (browser == null)
                browser = new ChromiumWebBrowser(url);
            else
                browser.LoadUrl(url);

            Thread.Sleep(5000);

            Retry.Do(() =>
            {
                parseCarPage(url);
            }, TimeSpan.FromSeconds(5));
        }

        private void parseListPage()
        {
            if (browser.Address.Contains("shopping/results/"))
            {
                string listParseScript = @"(function(){
	                                        var cars = document.getElementsByClassName('vehicle-card   ');
	                                        var result = [];
	                                        for(var i=0; i < cars.length; i++)  {
		                                        var car = {
		                                        url:cars[i].getElementsByClassName('vehicle-card-visited-tracking-link')[0].href,
		                                        title:cars[i].getElementsByClassName('title')[0].innerText,
		                                        mileage:cars[i].getElementsByClassName('mileage')[0].innerText,
		                                        price:cars[i].getElementsByClassName('primary-price')[0].innerText,
		                                        images:[]
		                                        };

		                                        var images =cars[i].getElementsByTagName('img');
		                                        for(var y=0; y < images.length; y++)  {
		                                           car.images.push(images[y].src);
		                                        }

		                                        result.push(car);
	                                        }
	                                        return result; 
                                        })()";

                browser.EvaluateScriptAsync(listParseScript).ContinueWith(x =>
                {
                    if (x.Result.Success && x.Result.Result != null)
                    {
                        var cars = JsonConvert.DeserializeObject<List<Car>>(JsonConvert.SerializeObject(x.Result.Result));
                        if (cars != null)
                        {
                            if (OnCarParse == null) return;
                            OnCarParse(this, cars);
                        }
                    }
                });
            }
            else
                throw new Exception("retry cars...");
        }

        private void parseCarPage(string url)
        {
            if (browser.Address.Contains("vehicledetail"))
            {
                browser.EvaluateScriptAsync("(function(){if(document.getElementsByClassName('fancy-description-list').length > 0) return true; else return false;})()").ContinueWith(x =>
                {
                    if (x.Result.Success && x.Result.Result != null && (bool)x.Result.Result)
                    {
                        string getDetailTableScript = "(function() {return document.getElementsByClassName('fancy-description-list')[0].innerText; })()";
                        browser.EvaluateScriptAsync(getDetailTableScript).ContinueWith(r =>
                        {
                            if (r.Result.Success && r.Result.Result != null)
                            {
                                var details = r.Result.Result.ToString().Split('\n');
                                var detail = new CarDetail
                                {
                                    ExteriorColor = details[1].Trim(),
                                    InteriorColor = details[3].Trim(),
                                    DriveTrain = details[5].Trim(),
                                    MPG = details[7].Trim(),
                                    FuelType = details[9].Trim(),
                                    Transmission = details[11].Trim(),
                                    Engine = details[13].Trim(),
                                    VIN = details[15].Trim(),
                                    Stock = details[18].Trim(),
                                };

                                if (OnCarDetailParse == null) return;

                                OnCarDetailParse(this, detail, url);

                            }
                        });
                    }
                });
            }

            if (!isDetailPageLoaded)
                throw new Exception("waiting car detail page...");
        }

        private void searchCars(Brands brand, Models model)
        {
            browser.EvaluateScriptAsync("(function(){if(document.getElementsByClassName('sds-pagination__item active').length > 0) { return document.getElementsByClassName('sds-pagination__item active')[0].innerText;} else return 'mainpage'})()").ContinueWith(x =>
            {
                if (x.Result.Success && x.Result.Result != null && (x.Result.Result.ToString() == "anasayfa" || x.Result.Result.ToString() != "1"))
                {
                    browser.ExecuteScriptAsync("document.getElementById('make-model-search-stocktype').selectedIndex=3"); // select used cars
                    browser.ExecuteScriptAsync($"document.getElementById('makes').selectedIndex={(byte)brand}");

                    browser.ExecuteScriptAsync("document.getElementById('make-model-max-price').selectedIndex=18"); // price 100K
                    browser.ExecuteScriptAsync("document.getElementById('make-model-maximum-distance').selectedIndex=11"); // all miles
                    browser.ExecuteScriptAsync("document.getElementById('make-model-zip').value='94596'"); // zip

                    browser.ExecuteScriptAsync($"document.getElementById('models').selectedIndex={(byte)model}");

                    string searchButtonClickScript = @"var buttons = document.getElementsByTagName('button');
                                                var searchText = 'Search';
                                                var found;

                                                for (var i = 0; i < buttons.length; i++) {
                                                  if (buttons[i].textContent == searchText) {
                                                    found = buttons[i];
                                                    break;
                                                  }
                                                }
                                                found.click()";

                    browser.ExecuteScriptAsync(searchButtonClickScript);
                }
            });


            if (!browser.Address.Contains("shopping/results/"))
            {
                throw new Exception("waiting search page..");
            }
        }

        private void gotoSignin()
        {
            browser.EvaluateScriptAsync("(function(){return document.getElementsByClassName('desktop-nav-user-name').length;})()").ContinueWith(x =>
            {
                if (x.Result.Success && x.Result.Result != null && x.Result.Result.ToString() != "0")
                {
                    isLoggedIn = true;
                }
                else
                {
                    if (!browser.Address.Contains("/signin/"))
                    {
                        string searchButtonClickScript = @"var aTags = document.getElementsByClassName('header-signin');
                                                var searchText = 'Sign in';
                                                var found;

                                                for (var i = 0; i < aTags.length; i++) {
                                                  if (aTags[i].textContent == searchText) {
                                                    found = aTags[i];
                                                    break;
                                                  }
                                                }
                                                found.click()";

                        browser.EvaluateScriptAsync(searchButtonClickScript);
                    }

                    if (browser.Address.Contains("/signin/"))
                    {
                        setCredential();
                    }
                }
            });

            if (!isLoggedIn)
                throw new Exception("waiting...");
        }

        private void setCredential()
        {
            if (browser.Address.Contains("/signin/"))
            {
                browser.ExecuteScriptAsync("document.getElementById('email').value='johngerson808@gmail.com'");
                browser.ExecuteScriptAsync("document.getElementById('password').value='test8008'");

                string searchButtonClickScript = @"(function(){
                                                var buttons = document.getElementsByClassName('sds-button');
                                                var searchText = 'Sign in';
                                                var found;

                                                for (var i = 0; i < buttons.length; i++) {
                                                  if (buttons[i].textContent == searchText) {
                                                    found = buttons[i];
                                                    break;
                                                  }
                                                }
                                                found.click();
                                                return { email: document.getElementById('email').value, password:document.getElementById('password').value};
                                                })()";

                browser.EvaluateScriptAsync(searchButtonClickScript);
            }
        }

        private void navSecondPage()
        {
            browser.ExecuteScriptAsync("if(document.getElementById('pagination-direct-link-2') != null) {document.getElementById('pagination-direct-link-2').click()}");

            if (!browser.Address.Contains("?page=2"))
                throw new Exception("navigate second page waiting...");
        }
    }
}
