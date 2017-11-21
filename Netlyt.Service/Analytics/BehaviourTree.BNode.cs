using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization; 
using nvoid.extensions;

namespace Netlyt.Service.Analytics
{
    public partial class BehaviourTree
    {
        [DataContract]
        public class BNode
        {
            [DataMember]
            private BehaviourTree.BNode Parent { get; set; }
            [DataMember]
            private string Label { get; set; }
            [DataMember]
            private Dictionary<string, BehaviourTree.BNode> Children { get; set; } = new Dictionary<string, BehaviourTree.BNode>();
            [DataMember]
            private float Visits { get; set; }
            [DataMember]
            public int Depth { get; private set; }
            [DataMember]
            private double[] time = new double[2] { 0.0d, 0.0d };

            public double Time
            {
                get { return this.time[0]; }
                set
                {
                    double d = this.time[0] + (value - this.time[0]) / this.Visits;
                    this.time[1] += (value - this.time[0]) * (this.time[0] - d);
                    this.time[0] = d;
                }
            }
            public BNode(BehaviourTree.BNode par, string label, double time, int depth)
            {
                this.Parent = par;
                this.Depth = depth;
                this.Label = label;
                this.Visits = 1;
                this.Time = time;
            }
            public void UpdateDepth()
            {
                this.Depth++;
                if (this.Parent != null)
                    this.Parent.UpdateDepth();
            }
            public double Frequency(bool log = true)
            {
                var f = this.Parent != null ? (this.Visits / this.Parent.Visits) : 1;
                if (log && this.Parent != null)
                    return Math.Log(f);

                return f;
            }
            public BehaviourTree.BNode GetDeepestPath()
            {
                if (this.Children.Count == 0) return null;
                int max_d = 0;
                BehaviourTree.BNode best_n = null;
                foreach (var c in this.Children)
                {
                    if (c.Value.Depth > max_d)
                    {
                        max_d = c.Value.Depth;
                        best_n = c.Value;
                    }
                }
                return best_n;
            }

            public List<Dictionary<string, double>> FindCommon(BehaviourTree.BNode other, List<Dictionary<string, double>> path)
            {
                var n = this.Children.Where(x => x.Key == other.Label);
                if (n.Count() == 0) return path;
                path.Add(new Dictionary<string, double> { { "time", this.Time }, { "frequency", this.Frequency() } });
                var b_path = new List<Dictionary<string, double>>();
                foreach (var c in n)
                {
                    var p = new List<Dictionary<string, double>>();
                    if (other.Children.ContainsKey(c.Key))
                    {
                        p = c.Value.FindCommon(other.Children[c.Key], p);
                    }
                    if (p.Count > b_path.Count)
                        b_path = p;
                }
                path.AddRange(b_path);
                return path;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="chain">List of keyvalue pairs where the key is the sitename and the value is the time spent</param>
            public void UpdateSelf(List<KeyValuePair<string, double>> chain)
            {
                this.Visits++;
                var data = chain.Pop();
                this.Time = data.Value;
                if (chain.Count == 0) return;
                var n = chain[0].Key;
                if (this.Children.Keys.Contains(n))
                    this.Children[n].UpdateSelf(chain);
                else
                {
                    if (this.Children.Count == 0)
                        this.UpdateDepth();
                    var c = new BNode(this, n, chain[0].Value, 0);
                    this.Children.Add(n, c);
                    c.UpdateSelf(chain);
                }
            }
        }

    }
}
