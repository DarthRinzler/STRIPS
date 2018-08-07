﻿using System;
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
            var fparser = new Parser("data\\objects.txt");
            World = fparser.ParseObjects();

            var aparser = new Parser("data\\actions.txt");
            Actions = aparser.ParseActions();

            //GoalMode();
            DebugMode();
		}

        /*
            Find all failing Predicate 
            Foreach failing predicate: 
                Find all Actions that can change the value of that Predicate to the desired value
            Get all Pre-reqs for These dependent actions               
         */
        static void GoalMode()
        {
            var goal = Actions["goal"];

            var p = new SObject[]
            {
                World["player"],
                World["petitioner"]
            };

            Expression failReason = null;
            while (true)
            {
                
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
                // If Object
                else if (World.Properties.ContainsKey(input[0]))
                {
                    var cur = World[input[0]];

                    for(int i=1; i<input.Length; i++)
                    {
                        var key = input[i];
                        if (cur.Properties.ContainsKey(key))
                        {
                            cur = cur[key];
                        }
                        else break;
                    }

                    Console.WriteLine(cur);
                }
            }
        }
	}
}
