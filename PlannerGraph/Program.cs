using System;

namespace PlannerGraph
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser p = new Parser("data");

            var actions = p.ParseActionFile("worldActions.txt");
            var start = p.ParseState("worldStart.txt");
            var end = p.ParseState("worldEnd.txt");

            Console.WriteLine(start.ToString());
            start.ApplyAction(new ActionInst())
        }
    }
}
