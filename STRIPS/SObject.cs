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

        public SObject(SObject other)
        {
            if (other != null)
            {
                this.Name = other.Name;
                this.Properties = other.Properties.ToDictionary(kv => kv.Key, kv => new SObject(kv.Value));
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
                return String.Format("{0}:{{{1}}}", Name, pvalues);
            }
            else return String.Format("{0}", Name);

        }

        public bool Satisfies(SObject goal)
        {
            foreach (var gp in goal.Properties)
            {
                // Boolean
                if (gp.Value == null)
                {
                    if (!Properties.ContainsKey(gp.Key))
                    {
                        return false;
                    }
                }
                // Reference
                else
                {
                    SObject prop = null;
                    if (Properties.TryGetValue(gp.Key, out prop))
                    {
                        if (!prop.Satisfies(gp.Value))
                        {
                            return false;
                        }
                    }
                    else return false;
                }
            }
            return true;
        }
    }
}
