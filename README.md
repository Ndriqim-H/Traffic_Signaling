# Traffic Signaling using Simulated annealing algorithm

This console application is designed to simulate traffic signaling using the simulated annealing algorithm. It requires command-line arguments to be passed in order to run correctly. <br> Below you will find the execution details and description of the application.

## Execution details
To run the application, follow the steps below:

Ensure that you have the .NET runtime installed on your system.

Open a command prompt or terminal.

Navigate to the directory where the application is located.

Execute the following command:

`dotnet run -i <input filename> -o <output filename> -t <SA temperature> -mi <SA max iterations> -cr <SA cooling rate>`

Replace `<input filename>` with the name of the input file containing traffic data. <br>
Replace `<output filename>` with the name of the output file where the optimized traffic signaling plan will be stored. <br>
Replace `<SA temperature>, <SA max iterations>, and <SA cooling rate>` with the desired values for the simulated annealing algorithm. <br>

Press Enter to run the command.

## Description
The purpose of this application is to optimize traffic signaling using the simulated annealing algorithm. <br> Simulated annealing is a probabilistic optimization algorithm that aims to find an optimal solution to a given problem by simulating the annealing process in metallurgy.

Upon executing the application, the command-line arguments are expected to be provided in the specified format. Here is a description of the parameters:

`-i` specifies the input filename parameter (required). It should be a string representing the name of the input file containing traffic data, such as traffic volumes and intersection details. <br>
`-o` specifies the output filename parameter (required). It should be a string representing the name of the output file where the optimized traffic signaling plan will be stored. <br>
`-t` specifies the temperature parameter (required). It should be a positive integer value and represents the initial temperature used in the simulated annealing algorithm. <br>
`-mi` specifies the max iterations parameter (required). It should be a positive integer value and represents the maximum number of iterations for the simulated annealing algorithm. <br>
`-cr` specifies the cooling rate parameter (required). It should be a positive double value and represents the cooling rate used in the simulated annealing algorithm. <br>
The application will read the traffic data from the input file, apply the simulated annealing algorithm to optimize the traffic signaling plan, and then write the optimized plan to the output file.

For more information about the application or the institution behind it, please visit https://fiek.uni-pr.edu/.

Feel free to contact the application developers for any further assistance or inquiries related to traffic signaling optimization using simulated annealing algorithm.
