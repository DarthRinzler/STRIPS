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
        public bool Truth { get; set; }
		public Dictionary<string, SObject> Properties { get; }
        public static SObject Refs = new SObject("references");

        public SObject(SObject other)
        {
            if (other != null)
            {
                this.Name = other.Name;
                this.Properties = other.Properties.ToDictionary(kv => kv.Key, kv => new SObject(kv.Value));
                this.Truth = other.Truth;
            }
        }

        public SObject(string name)
		{
            Properties = new Dictionary<string, SObject>();
			Name = name;
            Truth = true;
		}

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

        public override string ToString()
        {
            string pvalues = String.Empty;

            if (Properties.Any())
            {
                pvalues = Properties
                    .Select(p => p.Value.ToString())
                    .Aggregate((a, e) => a + "|" + e);

                return String.Format("{0}:{1}", Name, pvalues);
            }
            else return String.Format("{0}", Name);
        }

        public bool ContainsKey(string key)
        {
            return Properties.ContainsKey(key);
        }

        public bool IsTrue(params string[] predicates)
        {
            SObject cur = this;
            foreach(var pred in predicates)
            {
                if (!cur.TryGetValue(pred, out cur))
                {
                    return false;
                }
            }

            return cur.Truth;
        }

        public bool TryGetValue(string key, out SObject value)
        {
            return Properties.TryGetValue(key, out value);
        }

        public bool Remove(string key)
        {
            return Properties.Remove(key);
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

        public void GetNonSatisfyingProperties(SObject goal, List<SObject> missing)
        {
            foreach (var gp in goal.Properties)
            {
                // Boolean
                if (gp.Value == null)
                {
                    if (!Properties.ContainsKey(gp.Key))
                    {
                        missing.Add(gp.Value);
                    }
                }
                // Reference
                else
                {
                    SObject prop = null;
                    if (Properties.TryGetValue(gp.Key, out prop))
                    {
                        prop.GetNonSatisfyingProperties(gp.Value, missing);
                    }
                    else
                    {
                        missing.Add(gp.Value);
                    }
                }
            }
        }

        public SObject Clone()
        {
            return new SObject(this);
        }
    }
}
