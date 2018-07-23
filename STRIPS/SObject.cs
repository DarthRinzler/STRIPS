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

        public SObject this[string key]
        {
            get
            {
                return Properties[key];
            }
            set
            {
                Properties[key] = value;
            }
        }

        public SObject(string name)
		{
            Properties = new Dictionary<string, SObject>();
			Name = name;
		}

        public override string ToString()
        {
            string pvalues = String.Empty;

            if (Properties.Any())
            {
                pvalues = Properties
                    .Select(p => String.Format("{0}:{1}", p.Key, p.Value.Name))
                    .Aggregate((a, e) => a + "|" + e);
            }

            return String.Format("{0}:{{{1}}}", Name, pvalues);
        }

        public bool Satisfies(SObject goal)
        {
            foreach (var gp in goal.Properties.Values)
            {
                SObject prop = null;
                if (Properties.TryGetValue(gp.Name, out prop))
                {
                    if (!prop.Satisfies(gp))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
