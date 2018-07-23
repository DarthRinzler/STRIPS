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
		static void Main(string[] args)
		{
            var aparser = new Parser("rules.txt");
            var actions = aparser.ParseActions();

            var fparser = new Parser("facts.txt");
            var world = fparser.ParseObjects();

            while(true)
            {
                Console.WriteLine("Enter Command");
                var input = Console.ReadLine().Split(' ');

                // If Action
                SAction action = null;
                if (actions.TryGetValue(input[0], out action))
                {
                    SObject[] parameters = input
                        .Skip(1)
                        .Select(i => world[i])
                        .ToArray();

                    Expression failExpr = null;
                    if (action.CanApply(parameters, world, out failExpr))
                    {
                        action.Apply(parameters, world);
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
