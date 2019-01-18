using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STRIPS
{
    public class GraphPlan
    {
        public void Init(SObject facts, Dictionary<string, Action> actions)
        {
            FactLayer f = new FactLayer(facts);
            var actionList = actions.SelectMany(a => a.Value.GetActionInstances(facts)).ToList();
        }

        public ActionLayer CreateActionLayer(FactLayer factLayer)
        {
            return null;
        }
    }

    public class FactLayer
    {
        public SObject Facts { get; private set; }

        public FactLayer(SObject facts)
        {
            Facts = facts.Clone();
        }
    }

    public class ActionLayer
    {

    }

    public struct FactNode
    {
        public Literal Literal { get; set; }

        public List<ActionNode> ActionNodes { get; set; }
    }

    public struct ActionNode
    {
        public Action Action { get; set; }
    }

    public struct Literal
    {
        public string ObjectName { get; set; }
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public bool Truth { get; set; }
    }
} 
