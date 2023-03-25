using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Traffic_Signaling
{
    class Program
    {
        public static void Main(string[] args)
        {
            var input = File.ReadAllLines(Directory.GetCurrentDirectory() + "\\a_an_example.in.txt");

            // Parse input
            var parameters = input[0].Split(' ');
            //Simulation duration
            var D = int.Parse(parameters[0]);
            //Number of intersections
            var I = int.Parse(parameters[1]);
            //Number of streets
            var S = int.Parse(parameters[2]);
            //Number of cars
            var V = int.Parse(parameters[3]);
            //Number of points for reaching the destination on time
            var F = int.Parse(parameters[4]);

            
            //Parse all the streets
            List<Street> streets = new();
            for (int i = 1; i <= S; i++)
            {
                streets.Add(new Street()
                {
                    Id = i,
                    Starts = int.Parse(input[i].Split(' ')[0]),
                    Ends = int.Parse(input[i].Split(' ')[1]),
                    Name = input[i].Split(' ')[2],
                    Time = int.Parse(input[i].Split(' ')[3])
                });
            }

            //Create intersections
            List<Intersection> intersections = new();
            for (int i = 0; i < I; i++)
            {
                intersections.Add(new Intersection()
                {
                    Id = i,
                    Streets = streets.Where(t=>t.Ends == i).ToList(),
                });
            }

            //Create car paths
            List<Path> paths = new();
            //List<Intersection> intersectionsInPath = new();

            for (int i = S + 1; i < V + S + 1; i++)
            {
                var arr = input[i].Split(' ');
                //List<string> pathStreets = new();
                //for (int j = 0; j < pathStreets.Count; j++)
                //{
                //    pathStreets.Add(arr[j + 1]);
                //}

                List<Street> streetsInPath = new();
                //Get all the streets from the path except the last one
                for (int j = 1; j < arr.Length - 1; j++)
                {
                    var street = streets.Where(t => t.Name == arr[j]).First();
                    streetsInPath.Add(new Street()
                    {
                        Name = arr[j],
                        Id = street.Id,
                        Ends = street.Ends,
                        Starts = street.Starts,
                        Time = street.Time,
                    });
                    

                    
                }

                paths.Add(new Path()
                {
                    Id = i - (S + 1),
                    NumberOfIntersections = int.Parse(arr[0]),
                    Streets = streetsInPath,
                    Destination = arr[arr.Length - 1]
                });

                


            }


            
            for (int i = 0; i < paths.Count; i++)
            {
                List<Intersection> intersactionsInPath = new();
                
                for (int j = 0; j < paths[i].Streets.Count; j++)
                {
                    
                    Intersection intersection = intersections.Where(t=>t.Id == paths[i].Streets[j].Ends).First();
                    intersactionsInPath.Add(intersection);
                }
                paths[i].Intersections = intersactionsInPath;
                
            }
            //for (int i = 0; i < intersectionsInPath.Count; i++)
            //{
            //    if (intersectionsInPath[i].Streets.Count == 1)
            //    {
            //        intersectionsInPath[i].GreenInterval = 1;
            //        intersectionsInPath[i].GreenTimeForStreets = new List<int> { 1 };
            //    }
            //    else
            //    {
            //        intersectionsInPath[i].GreenInterval = intersectionsInPath[i].Streets.Count;
            //        intersectionsInPath[i].GreenTimeForStreets = new();
            //        for (int j = 0; j < intersectionsInPath[i].Streets.Count; j++)
            //        {
            //            intersectionsInPath[i].GreenTimeForStreets.Add(1);
            //        }
            //    }
            //}
            Console.WriteLine("Hello World!");

        }

    }

    class Intersection
    {
        public int Id { get; set; }
        public List<Street> Streets { get; set; }
        public int GreenInterval { get; set; }
        public List<int> GreenTimeForStreets { get; set; }
    }

    class Street
    {
        public int Id { get; set; }
        public int Starts { get; set; }
        public int Ends { get; set; }
        public string Name { get; set; }
        public int Time { get; set; }
    }
    class Path
    {
        public int Id { get; set; }
        public int NumberOfIntersections { get; set; }
        public List<Intersection> Intersections { get; set; }
        public List<Street> Streets { get; set; }
        public string Destination { get; set; }
    }
}
