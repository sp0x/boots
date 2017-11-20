using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using nvoid.extensions;

namespace Netlyt.Service.Analytics
{
    [DataContract]
    public class BehaviourTree
    {
        [DataContract]
        public class BNode
        {
            [DataMember]
            private BehaviourTree.BNode Parent { get; set; }
            [DataMember]
            private string Label { get; set; }
            [DataMember]
            private Dictionary<string, BehaviourTree.BNode> Children { get; set; }  = new Dictionary<string, BehaviourTree.BNode>();
            [DataMember]
            private float Visits { get; set; }
            [DataMember]
            public int Depth { get; private set; }
            [DataMember]
            private double[] time  = new double[2]{0.0d,0.0d};

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
             public void UpdateSelf(List<KeyValuePair<string,double>> chain){                 
                 this.Visits++;
                 var data = chain.Pop();
                 this.Time = data.Value;
                 if(chain.Count==0)return;
                 var n = chain[0].Key;
                 if (this.Children.Keys.Contains(n))
                    this.Children[n].UpdateSelf(chain);
                else{
                    if(this.Children.Count==0)
                        this.UpdateDepth();
                    var c = new BNode(this,n,chain[0].Value,0);
                    this.Children.Add(n,c);
                    c.UpdateSelf(chain);
                }
            }
        }


        private object _buildLock = new object();
        [DataMember]
        Dictionary<string,BNode> Paths = new Dictionary<string,BNode>();
        public BehaviourTree(){            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="chain">List<KeyValuePairs> where key is the site name and value is the time spent on it</param>
        public void Build(List<KeyValuePair<string,double>> chain){
            lock (_buildLock)
            {
                var dom = chain[0];
                string domVal = dom.Key;
                if (this.Paths.ContainsKey(domVal))
                    this.Paths[domVal].UpdateSelf(chain);
                else
                {
                    var r = new BNode(null, domVal, dom.Value, 0);
                    this.Paths.Add(domVal, r);
                    r.UpdateSelf(chain);
                }
            }
        }
         public List<Dictionary<string,double>> LongestPath(){
             BNode cur=null;
             var path = new List<Dictionary<string,double>>(); 
             var d = 0;
             foreach(var p in this.Paths){
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
            var cur = this.Paths.Where(x=> other.Paths.ContainsKey(x.Key));
            var best_p = new List<Dictionary<string,double>>();
            foreach (var n in cur)
            {
                var pn = n.Value;
                var p = new List<Dictionary<string,double>>();
                p.Add(new Dictionary<string,double>{{"time",pn.Time},{"frequency",pn.Frequency()}});
                p = pn.FindCommon(other.Paths[n.Key],p);
                if(p.Count>best_p.Count)
                    best_p = p;
            }
            return best_p;
        }
        public void Save(string path){
            var serializer = new DataContractJsonSerializer(typeof(BehaviourTree));           
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, this);
                using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                    ms.WriteTo(file);
                }
            }   
        }
        public static BehaviourTree Load(string path){
            var deserializer = new DataContractJsonSerializer(typeof(BehaviourTree));
            BehaviourTree tree = null;
            MemoryStream ms = new MemoryStream();
            using(FileStream f = new FileStream(path, FileMode.Open, FileAccess.Read)){
                f.CopyTo(ms);
                tree = deserializer.ReadObject(ms) as BehaviourTree;
            }              
            ms.Close();
            return tree;
        }
        private static double IC(List<Dictionary<string,double>> path, string weight){
            if(path.Count==0)return 0;
            var sum = path.Sum(x=>x[weight]);
            var p = path.Select(x=>1.0*x[weight]/sum).ToArray();
            return -p.Sum(x=> x*Math.Log(x));
        }
        public double LinScore(BehaviourTree other, string w="frequency"){
            var p1 = this.LongestPath();
            var p2 = other.LongestPath();
            var lcp = this.LCP(other);
            var ic = IC(lcp,w);
            var d = (IC(p1,w)+IC(p2,w));
            var linScore = 2 * ic / d; 
            return double.IsNaN(linScore) ? 0.0 : linScore;
        }
       
    }
}