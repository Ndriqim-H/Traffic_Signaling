﻿using System;
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
            for (int i = S + 1; i < V + S + 1; i++)
            {
                var arr = input[i].Split(' ');
                List<Street> streetsInPath = new();
                //Get all the streets from the path except the last one, the last street is saved
                //separately as the destination
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

            //Find the intersections crossed in each path
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

            //Find the used intersections
            List<Intersection> usedIntersections = new();
            for (int i = 0; i < paths.Count; i++)
            {
                var intersectionsInPaths = paths[i].Intersections.ToList();
                for (int j = 0; j < intersectionsInPaths.Count; j++)
                {
                    var usedInterSectionsIds = usedIntersections.Select(t=> t.Id).ToList();
                    if (!usedInterSectionsIds.Contains(intersectionsInPaths[j].Id))
                        usedIntersections.Add(new Intersection()
                        {
                            Id = intersectionsInPaths[j].Id,
                            Streets = new()
                        });
                }
            }

            //Find the streets in paths
            List<int> pathStreetIntersectionIds = new List<int>();
            List<Street> streetsInPaths = new List<Street>();
            for (int i = 0; i < paths.Count; i++)
            {
                for (int j = 0; j < paths[i].Streets.Count; j++)
                {
                    Street streetInPath = paths[i].Streets[j];
                    if(!streetsInPaths.Select(t=>t.Id).Contains(streetInPath.Id))
                        streetsInPaths.Add(streetInPath);
                }
                
            }


            //Connect used intersections with the streets that are crossed by the cars
            for (int i = 0; i < usedIntersections.Count; i++)
            {
                for (int j = 0; j < streetsInPaths.Count; j++)
                {
                    if (streetsInPaths[j].Ends == usedIntersections[i].Id)
                        usedIntersections[i].Streets.Add(streetsInPaths[j]);
                }
            }

            //Now there are 3 main scenarioes for each intersection:
            // 1. An intersection is never used by any car,
            //those have been removed when the "usedIntersections" list was being filled
            // 2. An intersection which has only 1 street in all paths, here we give a constant duration
            //since the traffic light is going to always be green for that street
            // 3. When an intersection has 2 or more streets going into it, this is the case where the
            //optimization will happen since we may need to change the the duration of the green intervals.
            //Here, however, since it's the initial solution, we have given the interval equal to the
            //number of streets going inwards, making it so every car goes one by one into the intersection.

            for (int i = 0; i < usedIntersections.Count; i++)
            {
                if (usedIntersections[i].Streets.Count == 1)
                {
                    usedIntersections[i].GreenInterval = 1;
                    usedIntersections[i].GreenTimeForStreets = new List<int> { 1 };
                }
                else
                {
                    usedIntersections[i].GreenInterval = usedIntersections[i].Streets.Count;
                    usedIntersections[i].GreenTimeForStreets = new();
                    for (int j = 0; j < usedIntersections[i].Streets.Count; j++)
                    {
                        usedIntersections[i].GreenTimeForStreets.Add(1);
                    }
                }
            }
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
