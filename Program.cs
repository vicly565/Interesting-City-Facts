using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;

namespace Program2
{
    /**
     * @author Victor Ly
     * @version 1.0
     * @since 2021-01-26
     * 
     * This program uses RESTful APIs to print out the weather given by a user, and helps them locate the 3 closests starbucks
     * to the heart of the city.
     * 
     * In the event that the user gives an invalid city it will try it 3 times before giving up on it.
     * If the API service is down it will try 3 times exponentially before giving up on it.
     * If the program was unsucessful to printing weather information this program will not find the closest starbucks'
     */
    class Program
    {
        //API Keys
        private static String openWeatherKey = "ba058774fcefcd01bfc084c2c65dc012";
        private static String googlePlacesKey = "AIzaSyAHy7WCtLh15rwqNV43T7GuvTI-zntPnFg";

        //Root objects
        private static OpenWeatherAPI.Rootobject weatherInfo;
        private static GooglePlacesAPI.Rootobject starbucksInfo;

        //HTTP Response
        private static String result;
        private static HttpResponseMessage response = new HttpResponseMessage();

        //connection and city information
        private static float cityLat;
        private static float cityLon;
        private static bool connected = false;

        static void Main(string[] args)
        {
            //this string holds the city's name that is given by the user
            string city;

            //if the user didn't pass in arguments throw an exception
            if (args.Length == 0)
            {
                throw new ArgumentException("Nothing was entered into the console. Please try running the program again");
            }

            //check if the city has a space in the name or not
            if (args.Length == 1)
            {
                city = args[0];
            }
            else
            {
                //if the city has a space in its name concatinate the rest of the arguments into one string
                city = args[0];
                for (int i = 1; i < args.Length; i++)
                {
                    city += " " + args[i];


                }
            }

            //print weather information for the city
            PrintWeather(city);

            //if we were able to print information for the city
            if (connected)
            {
                //reset connecrtion and try and print out the 3 closest starbucks
                connected = false;
                FindStarbucks(city);
            }

        }

        /**
         * This method connects to the OpenWeatherMap's API then prints the forrecast and tempurature for the day
         *
         * @param city The name of the city
         */
        private static void PrintWeather(string city)
        {
            String weatherSite = "http://api.openweathermap.org/data/2.5/weather?q=" + city + "&units=metric&mode=json&appid=" + openWeatherKey;

            //connect to openweathermap's api
            ConnectToWebsite(weatherSite);

            if (connected)
            {
                //deserializing json info
                weatherInfo = JsonConvert.DeserializeObject<OpenWeatherAPI.Rootobject>(result);

                Console.WriteLine("Weather today for: " + weatherInfo.name + ", " + weatherInfo.sys.country);
                Console.WriteLine("Forcast: " + weatherInfo.weather[0].description);

                //if the city is in the US then we will convert the temperature to fahrenheit
                if (weatherInfo.sys.country.Equals("US"))
                {
                    //printing out information about weather today in fahrenheit
                    Console.WriteLine("Current temperature: " + ToFahrenheit(weatherInfo.main.temp) + " °F");
                    Console.WriteLine("Minimum temperature:  " + ToFahrenheit(weatherInfo.main.temp_min) + " °F");
                    Console.WriteLine("Max temperature: " + ToFahrenheit(weatherInfo.main.temp_max) + " °F");
                    Console.WriteLine("Feels like " + ToFahrenheit(weatherInfo.main.feels_like) + " °F");
                }
                else
                {
                    //printing out information about weather today in celsius
                    Console.WriteLine("Current temperature: " + weatherInfo.main.temp + " °C");
                    Console.WriteLine("Minimum temperature:  " + weatherInfo.main.temp_min + " °C");
                    Console.WriteLine("Max temperature: " + weatherInfo.main.temp_max + " °C");
                    Console.WriteLine("Feels like " + weatherInfo.main.feels_like + " °C");
                }

                Console.WriteLine("Humidity: " + weatherInfo.main.humidity + "%");
                Console.WriteLine("Wind Speed: " + weatherInfo.wind.speed + " m/s");

                //get the latitude and longitude of the city
                cityLat = weatherInfo.coord.lat;
                cityLon = weatherInfo.coord.lon;

            }
            else
            {
                //if we were not able to connect to the api then we let the user know
                Console.WriteLine("Unable to connect to OpenWeatherMap's API with the city " + city);
                Console.WriteLine("Please retry the program again with a new city, or retry with the same city at a later time");
            }
        }

        /**
         * This method connects to google place's api then prints the address and information of the 3 closest starbucks
         * to the latitude and longitude of the city the user passed in
         *
         * @param city The name of the city
         */
        private static void FindStarbucks(string city)
        {
            //creating the website to connect to
            String starbucksMaps = "https://maps.googleapis.com/maps/api/place/nearbysearch/json?location=" + cityLat + "," +
                cityLon + "&keyword=starbucks&rankby=distance&key=" + googlePlacesKey;

            //connect to google maps api with the latitude and longitude of the cit that was entered
            ConnectToWebsite(starbucksMaps);

            //if we were able to connect to the google place api
            if (connected)
            {
                //deserialize the json file
                starbucksInfo = JsonConvert.DeserializeObject<GooglePlacesAPI.Rootobject>(result);

                Console.WriteLine("\nPrinting out the 3 nearest starbucks  (if found) to the heart of " + city);

                //runs until we either print 3 times or the max amount of starbucks in the area
                for (int i = 0, prints = 0; i < starbucksInfo.results.Length && prints != 3; i++)
                {
                    try
                    {
                        //if the starbucks isn't in operation still we skip over it
                        if (!(starbucksInfo.results[i].business_status.Equals("OPERATIONAL")))
                        {
                            continue;
                        }

                        Console.WriteLine("Starbucks #" + (prints + 1));

                        //calculate the distance from the given city using the latitude and longitude of the starbucks from the results
                        double distanceFromCity = CalculateMiles(starbucksInfo.results[i].geometry.location.lat, starbucksInfo.results[i].geometry.location.lng);
                        Console.WriteLine("Address: " + starbucksInfo.results[i].vicinity);
                        Console.WriteLine(distanceFromCity + " miles away");
                        Console.WriteLine("Rating " + starbucksInfo.results[i].rating);

                        //figure out if the starbucks is currently open or not
                        if (starbucksInfo.results[i].opening_hours.open_now == true)
                        {
                            Console.WriteLine("Currently Open\n");
                        }
                        else
                        {
                            Console.WriteLine("Currently Closed\n");
                        }
                        //catch any exceptions that may be thrown
                    } catch(Exception)
                    {
                        Console.Out.WriteLine("Unexpected Error when printing out information about Starbucks\n");
                    }
                    //increment how many we've printed
                    prints++;
                }
            }
            else
            {
                //if we were not able to connect to the Google Places API
                Console.WriteLine("Unable to connect to Google Places' API with the city " + city);
                Console.WriteLine("Please retry the program again with a new city, or retry with the same city at a later time");
            }
        }

        /**
         * This method figures out the distance in miles between two spots given in latitude and longitude
         * This method uses the city's location as one spot and the starbuck's location as the other spot
         * This is done by using the haversine formula.
         * Haversine formula was found and implemented following GeeksForGeeks' page on the Haversine Formula.
         *
         * @param locLat The latitude of the location
         * @param lonLat The longitude of the location
         */
        private static double CalculateMiles(float locLat, float locLon)
        {
            //find the difference between the lat and lon of the two locations in radians 
            double latDif = (cityLat - locLat) * (Math.PI / 180);
            double lonDif = (cityLon - locLon) * (Math.PI / 180);

            //convert the latitudes into radians
            double cityRadianLat = cityLat * (Math.PI / 180);
            double locRadianLat = cityLat * (Math.PI / 180);

            //earth's radius in kilometers
            double rad = 6371;

            //find the distance in kilometers using the haversine formula
            double a = Math.Pow(Math.Sin(latDif / 2), 2) +
               Math.Pow(Math.Sin(lonDif / 2), 2) *
               Math.Cos(locRadianLat) * Math.Cos(cityRadianLat);

            double c = 2 * Math.Asin(Math.Sqrt(a));

            //get the distance in kilometers then multiply it by 0.621371 to convert it into miles
            //then round it to 2 decimal places
            double distanceInMiles = Math.Round((rad * c) * 0.621371, 2);
            return distanceInMiles;
        }

        /**
         * This method converts celsius to fahrenheit
         *
         * @param temp the temeprature in celsius
         */
        private static double ToFahrenheit(float temp)
        {
            return Math.Round((temp * 1.8) + 32, 2);
        }


        /**
         * This method is a helper method for connecting the the api
         *
         * @param url the website being connected to
         */
        private static bool ConnectionHelper(string url)
        {
            HttpClient client = new HttpClient();
            response = client.GetAsync(url).Result;

            //if we get a succesful status code then we were able to connect to the site
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            //otherwise it was not successful
            return false;
        }

        /**
         * This method attempts to connect to the website for the API
         * If it is unsucessful it does an exponential backoff retry for between 7 seconds to 9 seconds
         *
         * @param url the website being connected to
         */
        private static void ConnectToWebsite(string url)
        {
            //this variable is used for how long we will wait before retrying
            int milliseconds = 1000;
            Random generator = new Random();
            //if we're not able to connect to the website
            while (!connected)
            {
                //attempt to connect
                if (ConnectionHelper(url))
                {
                    //if we're able to connect then we can exit the while loop
                    break;
                }

                //if milliseconds is over or equal to 8000 then we have exponentially grown to 8 seconds
                // and we don't want the user to wait for longer than 8 seconds total
                if (milliseconds >= 8000)
                {
                    connected = false;
                    return;
                }

                Console.WriteLine("Unable to connect to website retrying in " + (milliseconds / 1000) + " seconds");

                //wait for either 1, 2, or 4 miliseconds depending on how many retries
                Thread.Sleep(milliseconds);
                //exponentially increase milliseconds by multiplying it by 2 and adding a random number of milliseconds between 0 and 1000
                milliseconds = (milliseconds * 2) + generator.Next(0, 1000);
            }

            //once we hit this point then we have been able to connect to the website
            result = response.Content.ReadAsStringAsync().Result;
            connected = true;
            return;
        }
    }

}
