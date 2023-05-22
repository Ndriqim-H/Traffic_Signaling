using Microsoft.VisualBasic;
using System.IO;
using System.Threading.Tasks.Sources;

class Program
{

    public static void Main(string[] args)
    {
        string[] files = Directory.GetFiles("Inputs");

        // Iterate through each file
        for (int k = 0; k < files.Length; k++)
        {
            string file = files[k];
            string directoryPath = file.Replace(".in.txt", "");
            directoryPath = directoryPath.Replace("Inputs", "Outputs");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            
            string projectDirectory = "";
            projectDirectory = Directory.GetCurrentDirectory();
            projectDirectory += "\\Inputs\\";
            Console.WriteLine(projectDirectory);

            var input = File.ReadAllLines($"{file}");


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



            for (int j = 0; j < 10; j++)
            {

                
                
                State initial = FindInitialSolution(usedIntersections);

                State solution = SimulatedAnnealing(initial, paths, F, D);
                string score = EvaluationFunction(paths, solution, F, D).ToString();

                string outputFileName = file.Replace(".in", ".out").Replace("Inputs\\", "");
                string outputFile = Path.Combine(directoryPath, $"{j+1} - {score} - {outputFileName}");
                WriteOutputFile(outputFile, solution.Intersections);

            }
            // Perform operations on each file

            // Example: You can rename or delete the file, or perform any other operations here
        }



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

            //We write the streets based on the interval that is green
            foreach (var trafficLight in trafficLights.OrderBy(t => t.Value[0]))
            {
                outputLines.Add($"{trafficLight.Key} {trafficLight.Value[1] - trafficLight.Value[0]}");
            }
        }

        File.WriteAllLines(outputFile, outputLines);
    }


    public static State FindInitialSolution(List<Intersection> usedIntersections)
    {
        
        //Now there are 3 main scenarioes for each intersection:
        // 1. An intersection is never used by any car,
        //those have been removed when the "usedIntersections" list was being filled
        // 2. An intersection which has only 1 street in all paths, here we give a constant duration
        //since the traffic light is going to always be green for that street
        // 3. When an intersection has 2 or more streets going into it, this is the case where the
        //optimization will happen since we may need to change the the duration of the green intervals.
        //Here, however, since it's the initial solution, we have given the interval equal to the
        //number of streets going inwards, making it so every car goes one by one into the intersection.

        //usedIntersections = usedIntersections.OrderBy(t => Guid.NewGuid()).ToList();
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
                usedIntersections[i].GreenInterval = 2 * usedIntersections[i].Streets.Count;

                usedIntersections[i].StreetTime = new();
                int min = 0;
                int max = 2;//[0,2)
                var usedStreets = usedIntersections[i].Streets.OrderBy(t => Guid.NewGuid()).ToList();//Randmoness

                for (int j = 0; j < usedStreets.Count; j++)
                {
                    var str = usedStreets[j];
                    usedIntersections[i].StreetTime.Add(str.Name, new[] { min, max });
                    min++;
                    min++;
                    max++;
                    max++;
                }
            }
        }

        //For testing purposes we made the output a tuplo of the list and the index of
        //the element that was changed

        State state = new State()
        {
            Intersections = usedIntersections,
        };

        return state;
    }

    public static State Clone(State state)
    {

        List<Intersection> resultIntersections = state.Intersections.ConvertAll(intersection => new Intersection
        {
            GreenInterval = intersection.GreenInterval,
            Id = intersection.Id,
            Streets = intersection.Streets.ToList(), // create a new list with a copy of Streets
            StreetTime = new Dictionary<string, int[]>(intersection.StreetTime) // create a new dictionary with a copy of StreetTime
        });
        State newState = new State()
        {
            Intersections = resultIntersections,
        };

        return newState;
    }


    public static State SimulatedAnnealing(State state, List<Car> cars, int F, int D, double T = 100000, double CoolingRate = 0.99, int maxIterations = 10000)
    {
        Random random = new Random();
        State currentSolution = Clone(state);
        State bestSolution = Clone(state);
        int currentEnergy = EvaluationFunction(cars, state, F, D);
        int bestEnergy = currentEnergy;
        double temperature = T;
        int iterations = maxIterations;
        DateTime startTime = DateTime.Now;
        TimeSpan timeLimit = TimeSpan.FromMinutes(5);

        //Return solution after 5 minutes
        while (DateTime.Now - startTime < timeLimit)
        {
            Tuple<List<Intersection>, int[]> op;

            double rand = random.NextDouble();

            //Ensure to never enter the "RandomMultipleTimeDistributionOperator"
            //if there are less than 15 intersections in the solution.
            if (currentSolution.Intersections.Count < 15)
                rand -= 0.25;
            

            if (rand < 0.25)
                op = SwitchRandomValuesOperator(currentSolution.Intersections);
            else if (rand < 0.5)
                op = NudgeRandomTimesOperator(currentSolution.Intersections);
            else if (rand < 0.75)
                op = RandomTimeDistributionOperator(currentSolution.Intersections);
            else
                op = RandomMultipleTimeDistributionOperator(currentSolution.Intersections, random.Next(2, 15));

            State newSolution = new()
            {
                Intersections = op.Item1
            };
            //int newEnergy = DeltaFunction(cars, newSolution, F, D, op.Item2);
            int newEnergy = EvaluationFunction(cars, newSolution, F, D);

            //int x = 0;
            //newEnergy = DeltaFunction(cars, newSolution, F, D, op.Item2);
            //newEnergy1 = EvaluationFunction(cars, newSolution, F, D);
            if (newEnergy > currentEnergy)
            {
                currentSolution = newSolution;
                currentEnergy = newEnergy;

                if (newEnergy > bestEnergy)
                {
                    bestSolution = newSolution;
                    bestEnergy = newEnergy;
                }
            }
            else
            {
                double acceptanceProbability = Math.Exp(-Math.Abs(newEnergy - currentEnergy) / T);
                if (random.NextDouble() < acceptanceProbability)
                {
                    currentSolution = newSolution;
                    currentEnergy = newEnergy;
                }
            }
            T *= CoolingRate;
            if (T <= 0.0005)
                T = 0;
            iterations--;
        }
        return bestSolution;
    }


    //This operator finds a random intersection that has more than one incoming street and switches 
    //the green time intervals between 2 random streets.
    //This is a simple operator and it doesn't change the period of the signaling.
    static Tuple<List<Intersection>, int[]> SwitchRandomValuesOperator(List<Intersection> intersections)
    {
        //We first deep copy the list, since we the algorithm may still select the old one.
        List<Intersection> resultIntersections = intersections.ConvertAll(intersection => new Intersection
        {
            GreenInterval = intersection.GreenInterval,
            Id = intersection.Id,
            Streets = intersection.Streets.ToList(), // create a new list with a copy of Streets
            StreetTime = new Dictionary<string, int[]>(intersection.StreetTime) // create a new dictionary with a copy of StreetTime
        });

        //Select a random element from the new list
        int index = new Random().Next(resultIntersections.Count);
        Intersection resultIntersection = resultIntersections[index];

        //Make sure that the intersection has more than 1 incoming street
        while (resultIntersection.StreetTime.Count == 1)
        {
            index = new Random().Next(resultIntersections.Count);
            resultIntersection = resultIntersections[index];
        }

        //If the intersection has 2 streets we directly switch them
        //This is done to optimize the code so that we don't unnecessarily repeat new elements
        if (resultIntersection.StreetTime.Count == 2)
        {
            string key1 = resultIntersection.StreetTime.Keys.First();
            string key2 = resultIntersection.StreetTime.Keys.Last();
            int[] vals1 = resultIntersection.StreetTime[key1];
            resultIntersection.StreetTime[key1] = resultIntersection.StreetTime[key2];
            resultIntersection.StreetTime[key2] = vals1;

            resultIntersections[index] = resultIntersection;

        }
        //If the intersection has more than 2 incoming streets,
        //then we pick 2 randomly and switch their green schedules.
        else
        {
            int valIndex1 = new Random().Next(resultIntersection.StreetTime.Keys.Count);
            int valIndex2;
            do
            {
                valIndex2 = new Random().Next(resultIntersection.StreetTime.Keys.Count);
            } while (valIndex2 == valIndex1);

            string key1 = resultIntersection.StreetTime.Keys.ElementAt(valIndex1);
            string key2 = resultIntersection.StreetTime.Keys.ElementAt(valIndex2);
            int[] vals1 = resultIntersection.StreetTime[key1];
            resultIntersection.StreetTime[key1] = resultIntersection.StreetTime[key2];
            resultIntersection.StreetTime[key2] = vals1;

            resultIntersections[index] = resultIntersection;
        }

        return Tuple.Create(resultIntersections, new int[] { resultIntersection.Id });
    }

    static Tuple<List<Intersection>, int[]> NudgeRandomTimesOperator(List<Intersection> intersections)
    {

        List<Intersection> resultIntersections = intersections.ConvertAll(intersection => new Intersection
        {
            GreenInterval = intersection.GreenInterval,
            Id = intersection.Id,
            Streets = intersection.Streets.ToList(), // create a new list with a copy of Streets
            StreetTime = new Dictionary<string, int[]>(intersection.StreetTime) // create a new dictionary with a copy of StreetTime
        });

        Random random = new Random();
        Intersection selectedIntersection;

        do
        {
            int index = new Random().Next(resultIntersections.Count);
            selectedIntersection = resultIntersections[index];
        } while (selectedIntersection.StreetTime.Keys.Count == 1);

        int period = selectedIntersection.GreenInterval;
        int range = (int)(period / 2) + 1;
        do
        {
            period = selectedIntersection.GreenInterval;
            int rand = new Random().Next(-range, range);
            period += rand;
        }
        while (period < selectedIntersection.Streets.Count);
        selectedIntersection.GreenInterval = period;
        List<int> distributedTime = DistributeValue(selectedIntersection.GreenInterval,
            selectedIntersection.StreetTime.Keys.Count, 1);
        //string street = selectedIntersection.StreetTime.Where(t => t.Value[0] == 0).Select(t=>t.Key).First();
        List<string> streets = selectedIntersection.StreetTime.OrderBy(t => t.Value[0])
            .Select(t => t.Key).ToList();
        int lastTime = 0;
        for (int i = 0; i < streets.Count; i++)
        {
            string street = streets[i];
            int greenTimeForStreet = (int)distributedTime[i];
            selectedIntersection.StreetTime[street] = new int[] { lastTime, lastTime + greenTimeForStreet };
            lastTime += greenTimeForStreet;
        }

        return new Tuple<List<Intersection>, int[]>(resultIntersections, new int[] { selectedIntersection.Id });
    }

    static Tuple<List<Intersection>, int[]> RandomTimeDistributionOperator(List<Intersection> intersections)
    {

        List<Intersection> resultIntersections = intersections.ConvertAll(intersection => new Intersection
        {
            GreenInterval = intersection.GreenInterval,
            Id = intersection.Id,
            Streets = intersection.Streets.ToList(), // create a new list with a copy of Streets
            StreetTime = new Dictionary<string, int[]>(intersection.StreetTime) // create a new dictionary with a copy of StreetTime
        });

        Random random = new Random();
        Intersection selectedIntersection;

        do
        {
            int index = new Random().Next(resultIntersections.Count);
            selectedIntersection = resultIntersections[index];
        } while (selectedIntersection.StreetTime.Keys.Count == 1);

        List<int> distributedTime = DistributeValue(selectedIntersection.GreenInterval,
             selectedIntersection.StreetTime.Keys.Count, 1);
        //string street = selectedIntersection.StreetTime.Where(t => t.Value[0] == 0).Select(t=>t.Key).First();
        List<string> streets = selectedIntersection.StreetTime.OrderBy(t => t.Value[0])
            .Select(t => t.Key).ToList();
        int lastTime = 0;
        for (int i = 0; i < streets.Count; i++)
        {
            string street = streets[i];
            int greenTimeForStreet = (int)distributedTime[i];
            selectedIntersection.StreetTime[street] = new int[] { lastTime, lastTime + greenTimeForStreet };
            lastTime += greenTimeForStreet;
        }

        return new Tuple<List<Intersection>, int[]>(resultIntersections, new int[] { selectedIntersection.Id });
    }

    static Tuple<List<Intersection>, int[]> RandomMultipleTimeDistributionOperator(List<Intersection> intersections, int nrIntersections)
    {
        List<Intersection> resultIntersections = intersections.ConvertAll(intersection => new Intersection
        {
            GreenInterval = intersection.GreenInterval,
            Id = intersection.Id,
            Streets = intersection.Streets.ToList(), // create a new list with a copy of Streets
            StreetTime = new Dictionary<string, int[]>(intersection.StreetTime) // create a new dictionary with a copy of StreetTime
        });

        int count = 0;
        int[] changedIntersections = new int[nrIntersections];
        for (int i = 0; i < changedIntersections.Length; i++)
        {
            changedIntersections[i] = -1;
        }
        for (int i = 0; i < changedIntersections.Length; i++)
        {
            Random random = new Random();
            Intersection selectedIntersection;
            int index = 0;
            do
            {
                index = new Random().Next(resultIntersections.Count);
                selectedIntersection = resultIntersections[index];
            } while (selectedIntersection.StreetTime.Keys.Count == 1 || changedIntersections.Contains(index));

            List<int> distributedTime = DistributeValue(selectedIntersection.GreenInterval,
                 selectedIntersection.StreetTime.Keys.Count, 1);
            //string street = selectedIntersection.StreetTime.Where(t => t.Value[0] == 0).Select(t=>t.Key).First();
            List<string> streets = selectedIntersection.StreetTime.OrderBy(t => t.Value[0])
                .Select(t => t.Key).ToList();
            int lastTime = 0;
            for (int j = 0; j < streets.Count; j++)
            {
                string street = streets[j];
                int greenTimeForStreet = (int)distributedTime[j];
                selectedIntersection.StreetTime[street] = new int[] { lastTime, lastTime + greenTimeForStreet };
                lastTime += greenTimeForStreet;
            }

            changedIntersections[i] = index;
        }

        return new Tuple<List<Intersection>, int[]>(resultIntersections, changedIntersections);
    }
    public static List<int> DistributeValue(int totalValue, int numPlaces, int minValue)
    {
        if (numPlaces * minValue > totalValue)
        {
            throw new ArgumentException("Minimum value is too large");
        }

        List<int> result = new List<int>(numPlaces);
        int sum = 0;

        for (int i = 0; i < numPlaces; i++)
        {
            int value = minValue;
            result.Add(value);
            sum += value;
        }

        int remainingValue = totalValue - sum;

        while (remainingValue > 0)
        {
            int value = Math.Min(remainingValue, minValue);
            int index = new Random().Next(numPlaces);
            result[index] += value;
            remainingValue -= value;
        }

        return result;
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