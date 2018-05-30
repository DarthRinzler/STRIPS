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

            var canApply = actions[1].CanApply(world);
		}
	}
}
