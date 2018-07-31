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
        static Dictionary<string, SAction> Actions;
        static SObject World;

		static void Main(string[] args)
		{
            var fparser = new Parser("facts.txt");
            World = fparser.ParseObjects();

            var aparser = new Parser("rules.txt");
            Actions = aparser.ParseActions();

            DebugMode();
		}

        static void GoalMode()
        {
            SObject state = World;
        }

        static void DebugMode()
        {
            while(true)
            {
                Console.WriteLine("Enter Command");
                var input = Console.ReadLine().Split(' ');

                // If Action
                SAction action = null;
                if (Actions.TryGetValue(input[0], out action))
                {
                    SObject[] parameters = input
                        .Skip(1)
                        .Select(i => World[i])
                        .ToArray();

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
            }
        }
	}
}
