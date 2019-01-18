using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace STRIPS
{
	class Program
	{
        static Dictionary<string, Action> Actions;
        static SObject World;

		static void Main(string[] args)
		{
            var fparser = new Parser("data\\objects.txt");
            World = fparser.ParseObjects();

            var aparser = new Parser("data\\actions.txt");
            Actions = aparser.ParseActions();



            GoalMode();
            //DebugMode();
		}

        /*
            Find all failing Predicate 
            Foreach failing predicate: 
                Find all Actions that can change the value of that Predicate to the desired value
            Get all Pre-reqs for These dependent actions               
         */
        static void GoalMode()
        {
            while (true)
            {
                Console.ReadLine();
                var allPossibleActions = Actions.Values.SelectMany(a => a.GetActionInstances(World)).ToArray();

                foreach (var a in allPossibleActions)
                {
                    Console.WriteLine(a);
                }
            }

            Console.WriteLine("Found solution!");
        }

        static void DebugMode()
        {
            while(true)
            {
                Console.WriteLine("\nEnter Command");
                var input = Console.ReadLine().ToLower().Split(' ');

                // If Action
                Action action = null;
                if (Actions.TryGetValue(input[0], out action))
                {
                    var notFound = input.Skip(1).FirstOrDefault(p => !World.ContainsKey(p));
                    if (notFound != null)
                    {
                        Console.WriteLine("ERROR: Unable to find object: {0}", notFound);
                        continue;
                    }

                    SObject[] parameters = input
                        .Skip(1)
                        .Select(i => World[i])
                        .ToArray();

                    if (!parameters.Any())
                    {
                        var actions = Actions[input[0]].GetActionInstances(World);
                        foreach (var a in actions) Console.WriteLine(a);
                        continue;
                    }

                    Expression failExpr = null;
                    if (action.CanApply(parameters, World, out failExpr))
                    {
                        action.Apply(parameters, World);
                    }
                    else
                    {
                        Console.WriteLine("Expression failed: ");
                        Console.WriteLine(failExpr.Print(parameters));
                        continue;
                    }

                    var str = parameters.Select(p => p.ToString());
                    var newState = String.Join("\r\n", str);
                    Console.WriteLine(newState);
                }
                // If Object
                else if (World.ContainsKey(input[0]))
                {
                    Console.WriteLine(World.IsTrue(input));
                }
            }
        }
	}
}
