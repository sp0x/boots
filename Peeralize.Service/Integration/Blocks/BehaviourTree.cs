using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;


namespace Peeralize.Service.Integration.Blocks
{

    public class BehaviourTree
    {
        public class BNode
        {
            private BehaviourTree.BNode parent;
            private string label;
            private Dictionary<string, BehaviourTree.BNode> children = new Dictionary<string, BehaviourTree.BNode>();
            private float visits;
            private int depth;
            private double[] time = new double[2]{0.0d,0.0d};
            public double Time
            {
                get { return this.time[0]; }
                set
                {
                    double d = this.time[0] + (value - this.time[0]) / this.visits;
                    this.time[1] += (value - this.time[0]) * (this.time[0] - d);
                    this.time[0] = d;
                }
            }
            public int Depth{
                get{return this.depth;}
            }
            public BNode(BehaviourTree.BNode par, string label, double time, int depth)
            {
                this.parent = par;
                this.depth = depth;
                this.label = label;
                this.visits = 1;
                this.Time = time;
            }
            public void UpdateDepth()
            {
                this.depth++;
                if (this.parent != null)
                    this.parent.UpdateDepth();
            }
            public double Frequency(bool log = true)
            {
                var f = this.parent != null ? (this.visits / this.parent.visits) : 1;
                if (log && this.parent != null)
                    return Math.Log(f);
                return f;
            }

            public BehaviourTree.BNode GetDeepestPath()
            {
                if (this.children.Count == 0) return null;
                int max_d = 0;
                BehaviourTree.BNode best_n = null;
                foreach (var c in this.children)
                {
                    if (c.Value.depth > max_d)
                    {
                        max_d = c.Value.depth;
                        best_n = c.Value;
                    }
                }
                return best_n;
            }

            public List<Dictionary<string, double>> FindCommon(BehaviourTree.BNode other, List<Dictionary<string, double>> path)
            {
                var n = this.children.Where(x => x.Key == other.label);
                if (n.Count() == 0) return path;
                path.Add(new Dictionary<string, double> { { "time", this.Time }, { "frequency", this.Frequency() } });
                var b_path = new List<Dictionary<string, double>>();
                foreach (var c in n)
                {
                    var p = new List<Dictionary<string, double>>();
                    p = c.Value.FindCommon(other.children[c.Key], p);
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
             public void UpdateSelf(List<KeyValuePair<string,double>> chain){                 
                 this.visits++;
                 var data = chain.Pop();
                 this.Time = data.Value;
                 if(chain.Count==0)return;
                 var n = chain[0].Key;
                 if (this.children.Keys.Contains(n))
                    this.children[n].UpdateSelf(chain);
                else{
                    if(this.children.Count==0)
                        this.UpdateDepth();
                    var c = new BNode(this,n,chain[0].Value,0);
                    this.children.Add(n,c);
                    c.UpdateSelf(chain);
                }
            }
        }
        Dictionary<string,BNode> paths = new Dictionary<string,BNode>();
        public BehaviourTree(){            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="chain">List<KeyValuePairs> where key is the site name and value is the time spent on it</param>
        public void Build(List<KeyValuePair<string,double>> chain){
            var n = chain[0];
            if(this.paths.Keys.Contains(n.Key))
                this.paths[n.Key].UpdateSelf(chain);
            else{
                var r = new BNode(null,n.Key,n.Value,0);
                this.paths.Add(n.Key, r);
                r.UpdateSelf(chain);
            }
        }
         public List<Dictionary<string,double>> LongestPath(){
             BNode cur=null;
             var path = new List<Dictionary<string,double>>(); 
             var d = 0;
             foreach(var p in this.paths){
                 if(p.Value.Depth > d){
                    cur = p.Value;
                 }
             }
             if(cur==null)return path;
             path.Add(new Dictionary<string,double>{{"time",cur.Time},{"frequency",cur.Frequency()}});
             while(cur!=null){
                 var old_cur = cur;
                 cur = cur.GetDeepestPath();
                 if(cur!=null)
                    path.Add(new Dictionary<string,double>{{"time",cur.Time},{"frequency",cur.Frequency()}});
                else
                    path.Add(new Dictionary<string,double>{{"time",old_cur.Time},{"frequency",old_cur.Frequency()}});
             }
            return path;
        }
        public List<Dictionary<string,double>> LCP(BehaviourTree other){
            var cur = this.paths.Where(x=> other.paths.ContainsKey(x.Key));
            var best_p = new List<Dictionary<string,double>>();
            foreach (var n in cur)
            {
                var pn = n.Value;
                var p = new List<Dictionary<string,double>>();
                p.Add(new Dictionary<string,double>{{"time",pn.Time},{"frequency",pn.Frequency()}});
                p = pn.FindCommon(other.paths[n.Key],p);
                if(p.Count>best_p.Count)
                    best_p = p;
            }
            return best_p;
        }
        private static double IC(List<Dictionary<string,double>> path, string weight){
            if(path.Count==0)return 0;
            var sum = path.Sum(x=>x[weight]);
            var p = path.Select(x=>1.0*x[weight]/sum).ToArray();
            return -p.Sum(x=> x*Math.Log(x));
        }
        public double Lin(BehaviourTree other, string w="frequency"){
            var p1 = this.LongestPath();
            var p2 = other.LongestPath();
            var lcp = this.LCP(other);
            return 2 * IC(lcp,w) / (IC(p1,w)+IC(p2,w));
        }
       
    }
}