using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json; 

namespace Netlyt.Service.Analytics
{  
    [DataContract]
    public partial class BehaviourTree
    {

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