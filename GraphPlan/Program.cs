using System;
using System.Collections.Generic;
using System.Linq;

namespace Planner {

    class Program
    {
        static void Main(string[] args)
        {
            Parser p = new Parser("data");

            var actions = p.ParseActionFile("baseActions.txt");
            var start = p.ParseState("baseStart.txt");
            //var end = p.ParseState("worldEnd.txt");

            Cmd(start, actions.Values);
        }

        private static void Cmd(State current, IEnumerable<ActionDef> actions)
        {
            while(true)
            {
                var availableActions = current 
                    .GetAllActions(actions)
                    .ToList();

                Console.WriteLine("Enter Action:");

                // Read Action
                var input = Console.ReadLine().Trim();
                if (input == "a")  // a prints all available actions
                {
                    foreach (var a in availableActions)
                    {
                        Console.WriteLine(a.ToString());
                    }
                }
                // Print state
                else if (input == "s")
                {
                    Console.WriteLine(current.ToString()); 
                }
                // Find and Apply Action
                else
                {
                    // Find Action
                    var action = availableActions.FirstOrDefault(a => a.ToString().Equals(input, StringComparison.InvariantCultureIgnoreCase));
                    if (action == null)
                    {
                        Console.WriteLine($"Unable to find action {input}");
                        continue;
                    }

                    // Apply Action
                    current = current.ApplyAction(action);
                }
            }
        }
    }
}
