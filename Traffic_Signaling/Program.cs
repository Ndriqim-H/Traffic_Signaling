﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Traffic_Signaling
{
    class Program
    {
        //A statistic on how much time was spent waiting a red light for every car.
        public static int NumberOfStops { get; set; } = 0;
        public static void Main(string[] args)
        {
            string inputFileName = "a_an_example.in";
            if(args.Length > 0) {
                inputFileName = args[0];
            }
            
            var input = File.ReadAllLines(Directory.GetCurrentDirectory() 
                + $"\\{inputFileName}.txt");

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
                    Streets = streets.Where(t => t.Ends == i).ToList(),
                });
            }

            //Create car paths
            List<Car> paths = new();
            for (int i = S + 1; i < V + S + 1; i++)
            {
                var arr = input[i].Split(' ');
                List<Street> streetsInPath = new();
                //Get all the streets from the path except the last one, the last street is saved
                //separately as the destination
                for (int j = 1; j < arr.Length - 1; j++)
                {
                    var street1 = streets.Where(t => t.Name == arr[j]).First();
                    streetsInPath.Add(new Street()
                    {
                        Name = arr[j],
                        Id = street1.Id,
                        Ends = street1.Ends,
                        Starts = street1.Starts,
                        Time = j == 1 ? 0 : street1.Time, //First street is 0 because we start from the
                                                          //end of the first street, i.e. at the intersection
                    });
                }

                var street = streets.Where(t => t.Name == arr[arr.Length - 1]).First();
                paths.Add(new Car()
                {
                    Id = i - (S + 1),
                    NumberOfIntersections = int.Parse(arr[0]),
                    Streets = streetsInPath,
                    DestinationName = street.Name,
                    DestinationTime = street.Time
                });
            }

            //Find the intersections crossed in each path
            for (int i = 0; i < paths.Count; i++)
            {
                List<Intersection> intersactionsInPath = new();
                for (int j = 0; j < paths[i].Streets.Count; j++)
                {

                    Intersection intersection = intersections.Where(t => t.Id == paths[i].Streets[j].Ends).First();
                    intersactionsInPath.Add(intersection);
                }
                paths[i].Intersections = intersactionsInPath;

            }

            //Find the used intersections
            List<Intersection> usedIntersections = new();
            for (int i = 0; i < paths.Count; i++)
            {
                List<Intersection> intersectionsInPaths = paths[i].Intersections;
                for (int j = 0; j < intersectionsInPaths.Count; j++)
                {
                    var usedInterSectionsIds = usedIntersections.Select(t => t.Id).ToList();
                    if (!usedInterSectionsIds.Contains(intersectionsInPaths[j].Id))
                        usedIntersections.Add(new Intersection()
                        {
                            Id = intersectionsInPaths[j].Id,
                            Streets = new()
                        });
                }
            }

            //Find the streets in paths
            List<Street> streetsInPaths = new List<Street>();
            for (int i = 0; i < paths.Count; i++)
            {
                for (int j = 0; j < paths[i].Streets.Count; j++)
                {
                    Street streetInPath = paths[i].Streets[j];
                    if (!streetsInPaths.Select(t => t.Id).Contains(streetInPath.Id))
                        streetsInPaths.Add(streetInPath);
                }

            }


            //Connect used intersections with the streets that are crossed by the cars
            for (int i = 0; i < usedIntersections.Count; i++)
            {
                for (int j = 0; j < streetsInPaths.Count; j++)
                {
                    if (streetsInPaths[j].Ends == usedIntersections[i].Id)
                    {
                        usedIntersections[i].Streets.Add(streetsInPaths[j]);
                    }

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
                    var str = usedIntersections[i].Streets[0];
                    usedIntersections[i].StreetTime = new()
                    {
                        {str.Name, new[]{0,int.MaxValue } },
                    };
                }
                else
                {
                    usedIntersections[i].GreenInterval = 2*usedIntersections[i].Streets.Count;
                    
                    usedIntersections[i].StreetTime = new();
                    int min = 0;
                    int max = 2;
                    for (int j = 0; j < usedIntersections[i].Streets.Count; j++)
                    {
                        var str = usedIntersections[i].Streets[j];
                        usedIntersections[i].StreetTime.Add(str.Name, new[] { min, max });
                        min++;
                        min++;
                        max++;
                        max++;
                    }
                }
            }

            int eval = EvaluationFunction(paths, usedIntersections, F, D);
            Console.WriteLine($"The calculated evaluation function is: {eval.ToString("#,#")} and number of " +
                $"total stop at traffic lights is {NumberOfStops}");

            //WriteOutputFile($"{inputFileName}.out.txt", usedIntersections);
            //Console.WriteLine("Hello World!");

        }

        static int EvaluationFunction1(List<Car> paths, List<Intersection> intersections, int F, int D)
        {
            int score = 0;
            for (int i = 0; i < paths.Count; i++)
            {
                int timer = 0;
                for (int j = 0; j < paths[i].Streets.Count; j++)
                {
                    var street = paths[i].Streets[j];
                    Intersection intersection = intersections.Find(t => t.Id == street.Ends);
                    if (j != 0)
                        timer += street.Time;
                    int min = intersection.StreetTime[street.Name][0];
                    int max = intersection.StreetTime[street.Name][1];
                    int interval = timer % intersection.GreenInterval;
                    while (!CheckIfInInterval(interval, min, max))
                    {
                        NumberOfStops++;
                        timer++;
                        interval = timer % intersection.GreenInterval;
                    }
                    if(j != 0)
                        timer++;
                }
                //timer += paths[i].DestinationTime;
                if (timer <= D)
                {
                    score += F + D - timer;
                }
            }
            return score;
        }

        static int EvaluationFunction(List<Car> cars, List<Intersection> intersections, int F, int D)
        {
            //We initialize the score and a global simulation timer
            int score = 0;
            int timer = 0;
            //While we are within the simulation time we continue the simulation.
            //We are not completely sure if it should be "timer <= D" or "timer < D"
            while (timer <= D) {
                List<Street> streetsToDequeue = new();
                //We iterate through all the cars
                for (int i = 0; i < cars.Count; i++)
                {
                    //If the car has already finished, we skip it
                    if (cars[i].Finished)
                        continue;

                    //If the car is moving we check if it has reached the end of the street
                    if (cars[i].Moving && timer == cars[i].T1Movement)
                        cars[i].Moving = false;

                    //If the car is still moving we skip it
                    if (cars[i].Moving)
                        continue;

                    //Using the position we find the street the car is at
                    int position = cars[i].Position;
                    var street = cars[i].Streets[position];

                    //Based on the end of the street we find the intersection and
                    //check if the green light is on for the interval
                    Intersection intersection = intersections.Find(t => t.Id == street.Ends);
                    int min = intersection.StreetTime[street.Name][0];//
                    int max = intersection.StreetTime[street.Name][1];
                    int interval = timer % intersection.GreenInterval;

                    //If the light is green we proceed to check the queue and move if allowed.
                    if (CheckIfInInterval(interval, min, max))
                    {
                        //We check to see if there is a queue at the intersection
                        //If there is no queue at all we proceed
                        if(!(street.Queue.Count == 0))
                        {
                            //If there is a queue we check if the current
                            //car is at the front of the queue
                            //If so, we dequeue it, if not we don't remove it and continue to
                            //the next car.
                            //Theoretically, it takes 1 second for the car to move up the queue if
                            //the light is green, so if the car is not in the front another car will
                            //move in the process and the timer will increment for the next one
                            if (cars[i].Id == street.Queue.Peek())
                                streetsToDequeue.Add(street);
                            else //if(street.Queue.Contains(street.Id))
                            {
                                NumberOfStops++;
                                continue;
                            }
                                
                        }
                        
                        //If the car sees the green light we increment its position and
                        //calculate how much time it will take to reach the next intersection
                        position++;
                        cars[i].Position = position;
                        if(position == cars[i].Streets.Count)
                        {
                            cars[i].Finished = true;
                            int fullTime = D - (timer + cars[i].DestinationTime);
                            if (fullTime < 0)
                                continue;

                            score += F + fullTime;
                            continue;
                        }
                        Street nextStreet = cars[i].Streets[position];

                        //If the next street is the destination, we calculate the score
                        //and mark the car as finished
                        

                        //interval = timer % intersection.GreenInterval;
                        cars[i].Moving = true;
                        cars[i].T0Movement = timer;
                        cars[i].T1Movement = timer + nextStreet.Time;
                    }
                    //If the light is red we put the car into a queue
                    //C# has made it easy for us since the street in the path(car) is also referenced
                    //from the intersection so it makes no difference.
                    else
                    {
                        //If the car is not in the queue we push it in.
                        if (!street.Queue.Contains(cars[i].Id)) {
                            NumberOfStops++;
                            street.Queue.Enqueue(cars[i].Id);
                        }
                    }
                }

                timer++;
                for (int i = 0; i < streetsToDequeue.Count; i++)
                {
                    streetsToDequeue[i].Queue.Dequeue();
                }
                
            }
            return score;
        }
        static bool CheckIfInInterval(int number, int min, int max) //is number element of [min,max)
        {
            return min <= number && number < max;
        }


        static void WriteOutputFile(string outputFile, List<Intersection> intersections)
        {
            var outputLines = new List<string>
            {
                intersections.Count.ToString()
            };

            foreach (var intersectionSchedule in intersections)
            {
                var intersectionId = intersectionSchedule.Id;
                var trafficLights = intersectionSchedule.StreetTime;

                outputLines.Add(intersectionId.ToString());
                outputLines.Add(trafficLights.Count.ToString());

                foreach (var trafficLight in trafficLights)
                {
                    outputLines.Add($"{trafficLight.Key} {trafficLight.Value[1] - trafficLight.Value[0]}");
                }
            }

            File.WriteAllLines(outputFile, outputLines);
        }

    }



    class Intersection
    {
        public int Id { get; set; }
        public List<Street> Streets { get; set; }
        public int GreenInterval { get; set; }
        public Dictionary<string, int[]> StreetTime { get; set; }
    }

    class Street
    {
        public int Id { get; set; }
        public int Starts { get; set; }
        public int Ends { get; set; }
        public string Name { get; set; }
        public int Time { get; set; }
        public Queue<int> Queue { get; set; } = new();
    }
    class Car
    {
        public int Id { get; set; }
        public int NumberOfIntersections { get; set; }
        public List<Intersection> Intersections { get; set; }
        public List<Street> Streets { get; set; }
        public string DestinationName { get; set; }
        public int DestinationTime { get; set; }
        public int Position { get; set; } = 0;
        public bool Finished { get; set; }
        public bool Moving { get; set; }
        public int T0Movement { get; set; }
        public int T1Movement { get; set; }

    }
}
