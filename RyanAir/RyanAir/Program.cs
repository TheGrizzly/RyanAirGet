using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using OfficeOpenXml;
using MathNet.Numerics;


namespace RyanAir
{
    class MainClass
    {
        public class Coordinates {
            public float Latitude { set; get; }
            public float Longitude { set; get; }

            public Coordinates(float latitud, float longitud){
                Latitude = latitud;
                Longitude = longitud;
            }
        }

        public class Aeropuerto{
            public string Name { set; get; }
            public string IataCode { set; get; }
            public Coordinates Coordinates { set; get; }
            public Aeropuerto(string name, string Iata, Coordinates corden){
                Name = name;
                IataCode = Iata;
                Coordinates = corden;
            }
            public void SetCoordinates(Coordinates cor) { Coordinates = cor; }
        }

        public class Aeropuertos
        {
            public Aeropuerto[] aeropuertos { set; get; }
            public Aeropuertos(Aeropuerto[] Aero){
                aeropuertos = Aero;
            }
        }

        public struct ArrivalAirport{
            public string IataCode { set; get; }
            public ArrivalAirport(string iata){
                IataCode = iata;
            }
        }

        public struct Price{
            public float Value { set; get; }
            public Price(float val){
                Value = val;
            }
        }

        public struct Outbound{
            public ArrivalAirport ArrivalAirport { set; get; }
            public Price Price { set; get; }
            public Outbound(ArrivalAirport arr, Price p){
                ArrivalAirport = arr;
                Price = p;
            }
        }

        public struct Fare{
            public Outbound Outbound { set; get; }
            public Fare(Outbound o){
                Outbound = o;
            }
        }

        public struct faresResp{
            public Fare[] fares { set; get; }
        }

        public static string get(string url){
            string respuesta;
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                respuesta = reader.ReadToEnd();
            }

            return respuesta;
        }

        public static float Distancia(Coordinates cords1, Coordinates cords2){
            float lat1, lat2, lon1, lon2;
            float respuesta = 0;
            float r = 6378.1f;

            lat1 = cords1.Latitude * float.Parse(Math.PI.ToString()) / 180;
            lat2 = cords2.Latitude * float.Parse(Math.PI.ToString()) / 180;
            lon1 = cords1.Longitude * float.Parse(Math.PI.ToString()) / 180;
            lon2 = cords2.Longitude * float.Parse(Math.PI.ToString()) / 180;

            float h = float.Parse((Math.Pow(Math.Sin((lat1 - lat2) / 2), 2)+(Math.Cos(lat1)*Math.Cos(lat2)* Math.Pow(Math.Sin((lon1 - lon2) / 2), 2))).ToString());

            respuesta = (float)(2 * r * Math.Asin(Math.Sqrt(h))); 

            return respuesta;
        }

        public static void Main(string[] args)
        {
            const string depDate = "2018-11-12";
            const string airportsEndpoint = @"https://api.ryanair.com/aggregate/3/common?embedded=airports&market=en-gb";
            const string fareEndpoint = "https://api.ryanair.com/farefinder/3/oneWayFares?market=en-gb&outboundDepartureDateFrom=" + depDate + "&outboundDepartureDateTo=" + depDate + "&departureAirportIataCode=";

            List<Aeropuerto> Aeropuertos = new List<Aeropuerto>();
            List<List<int>> graphFlights = new List<List<int>>();
            List<List<float>> graphFares = new List<List<float>>();
            List<List<float>> graphDistances = new List<List<float>>();
            List<String> nombresAeropuertos = new List<string>();
            List<String> iataAirports = new List<string>();

            Console.WriteLine("Grafos de ryanair!");

            //Query para sacar todos los aeropuertos
            String aeropuertos;
            aeropuertos = get(airportsEndpoint);

            

            //Ciclo para meter los aeropuertos del string json en Aeropurtos
            JObject puertos = JObject.Parse(aeropuertos);

            IList<JToken> informacion = puertos["airports"].Children().ToList();
            //IList<JToken> info2 = puertos["airports"]["coordinates"].Children().ToList();
            Coordinates hola = new Coordinates(float.Parse(5.5.ToString()), float.Parse(8.2.ToString()));
            foreach (JToken airport in informacion){
                Aeropuerto infopuerto = airport.ToObject<Aeropuerto>();
                Aeropuertos.Add(infopuerto);
                nombresAeropuertos.Add(airport["name"].ToString());
                iataAirports.Add(airport["iataCode"].ToString());
            }

            //Ciclo grafo inicial aeropuertos


            for (int i = 0; i < Aeropuertos.Count(); i++){
                //Query de los vuelos del aerotpuerto i
                graphFlights.Add(new List<int>());
                graphFares.Add(new List<float>());
                graphDistances.Add(new List<float>());

                faresResp respuestas;
                List<String> compare = new List<string>();

                String respuesta;
                respuesta = get(fareEndpoint + Aeropuertos[i].IataCode);

                JObject resp = JObject.Parse(respuesta);

                respuestas = resp.ToObject<faresResp>();

                for (int k = 0; k < respuestas.fares.Length;k++){
                    compare.Add(respuestas.fares[k].Outbound.ArrivalAirport.IataCode);
                }

                for (int j = 0; j < iataAirports.Count(); j++)
                {
                    if(compare.Contains(iataAirports[j])){
                        int index = compare.IndexOf(iataAirports[j]);
                        float costo = respuestas.fares[index].Outbound.Price.Value;
                        Coordinates cords1 = Aeropuertos[i].Coordinates;
                        Coordinates cords2 = Aeropuertos[j].Coordinates;
                        float distancia = Distancia(cords1, cords2);
                        graphFlights[i].Add(1);
                        graphFares[i].Add(costo);
                        graphDistances[i].Add(distancia);
                    }
                    else{
                        graphFlights[i].Add(0);
                        graphFares[i].Add(-1);
                        graphDistances[i].Add(-1);
                    }
                }
            }

            string name = "RyanAir.xls";

            using (var p = new ExcelPackage())
            {
                var wsb = p.Workbook.Worksheets.Add("Names");
                var ws1 = p.Workbook.Worksheets.Add("Flights");
                var ws2 = p.Workbook.Worksheets.Add("Distance");
                var ws3 = p.Workbook.Worksheets.Add("Fares");

                //ciclo para meter todo en un archivo xls
                for (int i = 0; i < graphFlights.Count(); i++)
                {
                    wsb.Cells[i + 1, 1].Value = nombresAeropuertos[i];
                    for (int j = 0; j < graphFlights[i].Count(); j++)
                    {
                        // metod cada uno de estos en una celda de excel
                        ws1.Cells[i+1, j+1].Value = graphFlights[i][j];
                        ws2.Cells[i+1, j+1].Value = graphFares[i][j];
                        ws3.Cells[i+1, j+1].Value = graphDistances[i][j];
                    }
                }

                p.SaveAs(new FileInfo(name));
            }
        }
    }
}
