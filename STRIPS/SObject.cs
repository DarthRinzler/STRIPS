using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STRIPS
{
	public class SObject
	{
		public string Name { get; set; }
		public Dictionary<string, SObject> Properties { get; }

		public SObject(string name, IList<SObject> properties = null)
		{
			Name = name;

            if (properties != null && properties.Any())
            {
                Properties = properties.ToDictionary(k => k.Name);
            }
		}
	}
}
