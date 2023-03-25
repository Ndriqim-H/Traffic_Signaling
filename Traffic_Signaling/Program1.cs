using System.Linq;

class Program1
{
    public static void Main1(string[] args)
    {
        var input = File.ReadAllLines(Directory.GetCurrentDirectory() + "\\a_an_example.in.txt");

        // Parse input
        var parameters = input[0].Split(' ');
        var D = int.Parse(parameters[0]);
        var I = int.Parse(parameters[1]);
        var S = int.Parse(parameters[2]);
        var V = int.Parse(parameters[3]);
        var F = int.Parse(parameters[4]);

        //Fill the intersections
        List<int> intersections = new();
        for (int i = 1; i <= I + 1; i++)
        {
            intersections.Add(int.Parse(input[i].Split(' ')[1]));            
        }
        List<Street> streets = new();
        for (int i = 1; i <= S; i++)
        {
            var arr = input[i].Split(' ');
            streets.Add(new Street()
            {
                Id = i - 1,
                Starts = int.Parse(arr[0]),
                Ends = int.Parse(arr[1]),
                Name = arr[2],
                Time = int.Parse(arr[3])
            });
        }

        
        //List<string> streets = new();
        //for (int i = 0; i < S; i++)
        //{
        //    streets.Add(intersections[2][i]);
        //}

        //Fill the paths for the cars
        List<string[]> paths = new();
        List<Path> pathsList = new();
        for (int i = 1 + S; i < input.Length; i++)
        {
            var arr = input[i].Split(' ');
            string[] arr1 = new string[arr.Length - 1];
            for (int j = 1; j < arr.Length - 1; j++)
            {
                arr1[j-1] = arr[j];
                
                pathsList.Add(new Path()
                {
                    Order = j,
                    StreetId = streets.Where(t => t.Name == arr[j]).Select(t=>t.Id).First()
                });
            }
            paths.Add(arr1);

        }
        
        //Connect all the paths to the intersections
        List<InterSectionPath> allPathIntersections = new();
        for (int i = 0; i < paths.Count; i++)
        {
            allPathIntersections.Add(new InterSectionPath()
            {
                PathId = i,
                Streets = streets.Where(t => paths[i].Contains(t.Name)).ToList(),
            });
        }


        List<int> notUsedIntersections = new();

        //Find all intersections which are not in any path
        List<int> allEnds = new();
        for (int i = 0; i < allPathIntersections.Count; i++)
        {
            allEnds.AddRange(allPathIntersections[i].Streets.Select(t => t.Ends));
        }
        //Remove unused intersections
        notUsedIntersections.AddRange(intersections.Where(t => !allEnds.Contains(t)));

        intersections = allEnds.Where(t => !notUsedIntersections.Contains(t)).ToList();
        
        //Find duplicates in intersections and save them
        
        var importatIntersections = intersections.GroupBy(x => x)
              //.Where(g => g.Count() > 1)
              .Select(y => (y.Key, y.Count()))
              .ToList();
        var trivialIntersections = intersections.Where(t => !importatIntersections
        .Where(x=>x.Item2 > 1)
        .Select(x=>x.Key)
                    .Contains(t)).ToList();

        //Find the paths whose intersections are unique, i.e. intersections where only 1 car passes through
        string x = "";

        for (int i = 0; i < allPathIntersections.Count; i++)
        {
            x += allPathIntersections[i].Streets.Where(t => trivialIntersections.Contains(t.Ends))
                .Select(t => t.Name).FirstOrDefault() + ", ";
        }

        List<IntersectionSchedule> y = new();
        for (int i = 0; i < allPathIntersections.Count; i++)
        {
            y.Add(allPathIntersections[i].Streets.Where(t => trivialIntersections.Contains(t.Ends))
                .Select(t => new IntersectionSchedule()
                {
                    Interval = 2,
                    IncomingNo = 1,
                    StreetName = t.Name
                }).First());
        }


        
        Console.WriteLine(intersections);
    }

    class Street
    {
        public int Id { get; set; }
        public int Starts { get; set; }
        public int Ends { get; set; }
        public string Name { get; set; }
        public int Time { get; set; }
    }

    class InterSectionPath
    {
        public List<Street> Streets { get; set; }
        public int PathId { get; set; }
    }

    class Path
    {
        public int StreetId  { get; set; }
        public int Order { get; set; }
    }

    class IntersectionSchedule
    {
        public int IncomingNo { get; set; }
        public int Interval { get; set; }
        public string StreetName { get; set; }
        public List<FinalStreet> Street { get; set; }
    }

    class FinalStreet
    {
        public string Name { get; set; }
        public int GreenTime { get; set; }
    }
    
}