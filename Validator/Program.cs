using Microsoft.VisualBasic;
using System.IO;

class Program
{

    public static void Main(string[] args)
    {
        string inputFileName = "a_an_example.in";

        string projectDirectory = "";
        projectDirectory = Directory.GetCurrentDirectory();
        projectDirectory += "\\Inputs\\";
        Console.WriteLine(projectDirectory);

        var input = File.ReadAllLines($"{projectDirectory}{inputFileName}.txt");

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

        List<Car> cars = new();
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
            cars.Add(new Car()
            {
                Id = i - (S + 1),
                NumberOfIntersections = int.Parse(arr[0]),
                Streets = streetsInPath,
                DestinationName = street.Name,
                DestinationTime = street.Time
            });
        }

        string uo = "a_an_example.in";
        string asd = $"{projectDirectory}{uo}";
        List<Intersection> inasd = ParseSubmissionFile(streets, uo);
        State state1 = new()
        {
            Intersections = inasd
        };

        int score = EvaluationFunction(cars, state1, F, D);
    }




    static List<Intersection> ParseSubmissionFile(List<Street> streets, string outputFile)
    {
        List<Intersection> intersections = new();
        //Read file 
        if (outputFile.Contains(".out"))
            outputFile.Replace(".out", "");
        var output = File.ReadAllLines($@"Outputs\{outputFile}.out.txt");
        int intersectionCount = int.Parse(output[0]);
        int count = 1;
        while (intersectionCount > 0)
        {
            int streetId = 0;
            int intersectionId = int.Parse(output[count]);
            count++;
            Intersection intersection = new()
            {
                Id = intersectionId,
                Streets = new()
            };
            int streetCount = int.Parse(output[count]);
            count++;
            if (streetCount == 1)
            {
                string[] street = output[count].Split(" ");
                count++;
                intersection.StreetTime = new()
                    {
                        {street[0], new[]{0,int.MaxValue } },
                    };
                intersection.GreenInterval = int.MaxValue;
                Street streetFromInput = streets.Where(t => t.Name == street[0]).FirstOrDefault();
                Street street1 = new()
                {
                    Ends = intersectionId,
                    Name = street[0],
                    Time = streetFromInput.Time,
                    Id = streetFromInput.Id,
                };
                intersection.Streets.Add(street1);
                //count += 3;
                streetId++;
            }
            else
            {
                int time = 0;

                for (int i = 0; i < streetCount; i++)
                {
                    string[] street = output[count].Split(" ");
                    count++;
                    int t = int.Parse(street[1]);
                    intersection.StreetTime ??= new Dictionary<string, int[]>();

                    intersection.StreetTime[street[0]] = new[] { time, time + t };

                    time += t;
                    Street streetFromInput = streets.Where(t => t.Name == street[0]).FirstOrDefault();

                    Street street1 = new()
                    {
                        Ends = intersectionId,
                        Name = street[0],
                        Time = streetFromInput.Time,
                        Id = streetFromInput.Id,
                    };

                    streetId++;
                }
                intersection.GreenInterval = time;

            }

            intersections.Add(intersection);
            intersectionCount--;
        }





        return intersections;
    }


    static int EvaluationFunction(List<Car> cars1, State state, int F, int D)
    {
        List<Car> cars = cars1.ConvertAll(t => new Car()
        {
            DestinationName = t.DestinationName,
            DestinationTime = t.DestinationTime,
            Finished = false,
            Moving = false,
            Id = t.Id,
            Intersections = t.Intersections,
            NumberOfIntersections = t.NumberOfIntersections,
            Position = 0,
            Streets = t.Streets,
            T1Movement = t.T1Movement,
            Score = t.Score,
        });
        List<Intersection> intersections = state.Intersections;

        //We initialize the score and a global simulation timer
        int score = 0;
        int timer = 0;
        //While we are within the simulation time we continue the simulation.
        //We are not completely sure if it should be "timer <= D" or "timer < D"
        while (timer <= D)
        {
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
                    if (!(street.Queue.Count == 0))
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
                            continue;
                        }

                    }

                    //If the car sees the green light we increment its position and
                    //calculate how much time it will take to reach the next intersection
                    position++;
                    cars[i].Position = position;
                    if (position == cars[i].Streets.Count)
                    {
                        cars[i].Finished = true;
                        int fullTime = D - (timer + cars[i].DestinationTime);
                        if (fullTime < 0)
                            continue;

                        score += F + fullTime;
                        cars1[i].Score = F + fullTime;

                        continue;
                    }
                    Street nextStreet = cars[i].Streets[position];

                    //If the next street is the destination, we calculate the score
                    //and mark the car as finished


                    //interval = timer % intersection.GreenInterval;
                    cars[i].Moving = true;
                    cars[i].T1Movement = timer + nextStreet.Time;
                }
                //If the light is red we put the car into a queue
                //C# has made it easy for us since the street in the path(car) is also referenced
                //from the intersection so it makes no difference.
                else
                {
                    //If the car is not in the queue we push it in.
                    if (!street.Queue.Contains(cars[i].Id))
                    {
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

}

public class State
{
    public List<Intersection> Intersections { get; set; }


}

public class Street
{
    public int Id { get; set; }
    public int Starts { get; set; }
    public int Ends { get; set; }
    public string Name { get; set; }
    public int Time { get; set; }
    public Queue<int> Queue { get; set; } = new();
}
public class Intersection
{
    public int Id { get; set; }
    public List<Street> Streets { get; set; }
    public int GreenInterval { get; set; }
    public Dictionary<string, int[]> StreetTime { get; set; }
}
public class Car
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
    public int T1Movement { get; set; }
    public int Score { get; set; }

}