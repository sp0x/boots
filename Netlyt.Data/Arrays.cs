using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Netlyt.Data
{
    public static class Arrays
    {
        public static Dictionary<TKey, TValue> AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dict, Dictionary<TKey, TValue> dict2)
        {
            foreach (var pair in dict2)
            {
                dict[pair.Key] = pair.Value;
            }
            return dict;
        }
        public static void PostAll<T>(this ITargetBlock<T> block, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                block.Post(item);
            }
        }
        public static IEnumerable<T> Pairwise<T>(
            this IEnumerable<T> source, Func<T, T, T> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw new InvalidOperationException("Sequence cannot be empty.");

                T prev = e.Current;

                if (!e.MoveNext()) throw new InvalidOperationException("Sequence must contain at least two elements.");

                do
                {
                    yield return selector(prev, e.Current);
                    prev = e.Current;
                } while (e.MoveNext());
            }
        }


        public static T Pop<T>(this List<T> list)
        {
            T r = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return r;
        }


        /// <summary>
        ///  Adds all the given items to the hashset
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="st"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static HashSet<T> AddRange<T>(this HashSet<T> st, params T[] items)
        {
            if ((items == null)) return st;
            foreach (var var in items)
            { 
                st.Add(var);
            }
            return st;
        }

        /// <summary>
        /// Adds all the given items to the hashset
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="st"></param>
        /// <param name="items"></param>
        /// <returns></returns> 
        public static HashSet<T> AddRange<T>(this HashSet<T> st, IEnumerable<T> items)
        {
            if ((items == null))
                return st; 
            foreach (var item in items)
            { 
                st.Add(item);
            }
            return st;
        }

        public static Int32 MaxCharactersMatching(this string a, string b, ref int biggestMatchIndex)
        {
            return MaxElementsMatching(a.ToCharArray(), b.ToCharArray(), ref biggestMatchIndex);
        }


        
        public static void AddHashRange<T>(this HashSet<T> hSet, IEnumerable<T> items)
        {
            foreach (var item in items)
            { 
                hSet.Add(item);
            }
        }

        /// <summary>
        /// Tries to figure out if a dictionary contains a value, using a predicate.
        /// If a match is found, the output key is set to the match, and the same is done for the value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="filter">A predicate to filter with</param>
        /// <param name="outKey">The matching key that was found</param>
        /// <param name="outValue">The matching value that was found</param>
        /// <returns>Whether a match was found</returns>
        
        public static bool ContainsValueEx<T, K>(this Dictionary<T, K> dictionary,
            Func<K, bool> filter, ref T outKey, ref K outValue)
        {
            if (dictionary == null)
                return false;
            foreach (var key in dictionary.Keys)
            { 
                if (filter(dictionary[key]))
                {
                    outKey = key;
                    outValue = dictionary[key];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        
        public static Int32 MaxElementsMatching<T>(this T[] a, T[] b, ref int biggestMatchIndex)
        {
            var firstIndex = -1;
            Int32 maxLength = -1;
            var crMatchLength = 0;
            var aCurrIx = 0;
            for (Int32 i = 0; i <= b.Length - 1; i++)
            {
                dynamic bElem = b[i];
                if ((bElem.Equals(a[aCurrIx])))
                {
                    //This the first match currently
                    if (firstIndex == -1)
                    {
                        crMatchLength += 1;
                        aCurrIx += 1;
                    }
                    else
                    {
                        //'If this isn't the first match, increase the index
                        aCurrIx += 1;
                        crMatchLength += 1;
                    }
                }
                else
                {

                    if (crMatchLength != 0)
                    {
                        if (maxLength < crMatchLength)
                        {
                            maxLength = crMatchLength;
                            biggestMatchIndex = i - crMatchLength;
                        }
                        crMatchLength = 0;
                    }
                    firstIndex = -1;
                    // Reset it
                    crMatchLength = 0;
                }
            }
            return maxLength;
        }
        
        public static T[] JoinArr<T>(this T[] src, T[] data2Copy)
        {
            src.Join(data2Copy);
            return src;
        }
        
        public static void Join<T>(this T[] src, T[] dta2copy)
        {
            if (src == null)
                src = new T[]{};
            T[] newarr = NewArray<T>((src.Length -1) + dta2copy.Length + 1);
            try
            {
                Int32 oldSize = src.Length;
                Array.Copy(src, 0, newarr, 0, src.Length);
                Array.Copy(dta2copy, 0, newarr, oldSize, dta2copy.Length);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            dta2copy = null;
            src = newarr;
        }
        
        public static object NewArray(this Type x, Int32 limit)
        {
            return x.GetConstructors()[0].Invoke(new object[]{ limit });
        }
        public static T[] NewArray<T>(Int32 limit)
        {
            return typeof(T).GetConstructors()[0].Invoke(new object[] { limit }) as T[];
        }

        public static Int32 LastIndexOf<TT>(this TT[] arr, TT[] search)
        {
            if (arr.Length < search.Length)
                return -1;
            for (Int32 i = arr.Length - 1; i >= 0; i += -1)
            {
                if ((arr.Length - i) < search.Length)
                    continue;
                //Key cannot fit
                if (arr[i].Equals(search[0]))
                {
                    for (Int32 i2 = 1; i2 <= search.Length - 1; i2++)
                    {
                        if ((arr.Length - (i2 + i)) < 0)
                            return -1;
                        if (!(arr[i + i2].Equals(search[i2])))
                            continue;
                    }
                    return i;
                }
            }
            return -1;
        }

        public static Int32 IndexOf<TT>(this TT[] arr, TT[] search)
        {
            if (arr.Length < search.Length)
                return -1;
            for (Int32 i = 0; i <= arr.Length - 1; i++)
            {
                if (arr[i].Equals(search[0]))
                {
                    for (Int32 i2 = 1; i2 <= search.Length - 1; i2++)
                    {
                        if ((arr.Length - (i2 + i)) < 0)
                            return -1;
                        if (!(arr[i + i2].Equals(search[i2])))
                            continue;
                    }
                    return i;
                }
            }
            return -1;
        } 

        public static Int32 IndexOf<TT>(this TT[] arr, TT key)
        {
            return Array.FindIndex(arr, (TT o) => o.Equals(key));
        }

        public static Int32 LastIndexOf<TT>(this TT[] arr, TT key)
        {
            return Array.FindLastIndex(arr, (TT o) => o.Equals(key));
        }

        public static byte[] Segment(this byte[] arrSrc, int index, int len)
        {
            if (len == -1)
                len = arrSrc.Length - index;
            var @out = new byte[len];
            Array.Copy(arrSrc, index, @out, 0, len);
            return @out;
        }
        public static Int64[] Segment(this int[] arrSrc, int index, int len)
        {
            if (len == -1)
                len = arrSrc.Length - index;
            var @out = new Int64[len];
            Array.Copy(arrSrc, index, @out, 0, len);
            return @out;
        }
        
        public static t[] Segment<t>(this t[] arrSrc, int index, int len)
        {
            if (len == -1)
                len = arrSrc.Length - index;
            t[] @out = new t[len];
            Array.Copy(arrSrc, index, @out, 0, len);
            return @out;
        }

        public static List<t[]> Split<t>(this IList<t> bin, t[] splitbits)
        {
            return bin.ToArray().Split(splitbits);
        }
        public static List<t[]> Split<t>(this IList<t> bin, int count)
        {
            return bin.ToArray().Split(count).ToList();
        }
        public static List<t[]> Split<t>(this t[] bin, t[] splitbits)
        {
            List<t[]> @out = new List<t[]>();
            if (splitbits.Length > bin.Length)
                return new List<t[]>(new[]{bin})
                    ;
            int lastIndex = 0;
            for (int i = 0; i <= bin.Length - 1; i++)
            {
                bool validIndex = false;
                if (bin.Length - i <= splitbits.Length)
                {
                    int start = LastIndexOf(bin, splitbits) + splitbits.Length;
                    if (start + 1 == splitbits.Length)
                        return new List<t[]>(new[] {bin});
                    var len = bin.Length - start;
                    var bar = new t[]{};
                    if (len > 0)
                    {
                        bar = Segment(bin, start, len);
                        @out.Add(bar);
                        break; // TODO: might not be correct. Was : Exit For
                    }
                    else if (len == 0)
                    {
                        bar = Segment(bin, lastIndex, bin.Length - (lastIndex + splitbits.Length));
                        @out.Add(bar);
                        break; // TODO: might not be correct. Was : Exit For
                    }

                    if (bin[i].Equals(splitbits[0]))
                    {
                        for (int iSearch = 0; iSearch <= splitbits.Length - 1; iSearch++)
                        {
                            if ((object) bin[i + iSearch] != (object) splitbits[iSearch])
                                continue;
                        }
                        validIndex = true;
                    }
                    else
                    {
                    }

                    if (validIndex)
                    {
                        t[] tempSegment = Segment(bin, lastIndex, (i) - lastIndex);
                        @out.Add(tempSegment);
                        lastIndex = i + splitbits.Length;
                    }
                }
            }
            return @out;
        }


        public static T[][] Split<T>(this T[] src, Int32 segments)
        {
            T[][] out2 = new T[segments][];
            if (src.Length / 2 < segments)
            {
                out2 = new T[][]{};
                return null;
            }
            int varPI = src.Length / segments;
            int leftover = src.Length % segments;
            for (int curSegment = 1; curSegment <= segments; curSegment++)
            {
                int start = (curSegment * varPI) - varPI;
                int end = curSegment * varPI;
                out2[curSegment - 1] = new T[(end - start)];
                Array.Copy(src, start, out2[curSegment - 1], 0, end - start);
            }
            if (leftover > 0)
            {
                Array.Resize(ref out2, (out2.Length-1) + 2);
                out2[out2.Length - 1] = new T[leftover];
                Array.Copy(src, src.Length - leftover, out2[out2.Length - 1], 0, leftover);
            }
            return out2;
        }


    
//    public static BitmapImage ToImage(this byte[] data)
//    {
//        BitmapImage img = new BitmapImage();
//        using (MemoryStream stream = new MemoryStream(data))
//        {
//            img.CacheOption = BitmapCacheOption.OnLoad;
//            img.BeginInit();
//            img.StreamSource = stream;
//            img.CacheOption = BitmapCacheOption.OnLoad;
//            img.EndInit();
//        }
//        return img;
//    }

    
        public static List<TTarget> DequeueAll<TTarget>(this ConcurrentQueue<TTarget> cQue)
        {
            return cQue.Take(cQue.Count);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="cQueue"></param>
        /// <param name="takeCount"></param>
        /// <param name="actionPerElement"></param>
        /// <returns></returns>
        /// <remarks></remarks>
    
        public static bool TryDequeue<TSource>(this ConcurrentQueue<TSource> cQueue, Int32 takeCount, Action<TSource> actionPerElement)
        {
            dynamic elements = cQueue.Take(takeCount);
            elements.WaitParallel(actionPerElement);
            return cQueue.IsEmpty;
        }
    
        public static List<TSource> Take<TSource>(this ConcurrentQueue<TSource> que, Int32 takeCount)
        {
            List<TSource> resultCollection = new List<TSource>();
            TSource result = default(TSource);
            while (que.TryDequeue(out result) & takeCount > 0)
            {
                resultCollection.Add(result);
                takeCount -= 1;
            }
            return resultCollection;
        }
    
        public static void WaitParallel<TSource>(this IEnumerable<TSource> items, Action<TSource> action)
        {
            dynamic loopResult = Parallel.ForEach(items, (TSource x, ParallelLoopState loopState) => { action(x); });
            while (!loopResult.IsCompleted)
            {
                Thread.Sleep(1);
            }
        }

    
        public static bool And(this IEnumerable<bool> enumerable)
        {
            return enumerable.All(elem => elem);
        }



    
        public static IEnumerable<IEnumerable<T>> SplitGroups<T>(this IEnumerable<T> enumerable, Int32 groupSz)
        {
            return new List<T[]>(Split(enumerable.ToArray(), groupSz));
        }

        public static string Join(this string[] stringArray, string delimiter)
        {
            return String.Join(delimiter, stringArray);
        }



    
        public static bool Contains<TElement>(this IEnumerable<TElement> collection, Func<TElement, bool> predicate)
        {
            if (collection == null)
                return false;
            if (!collection.Any())
                return false;
            return collection.FirstOrDefault(predicate) != null;
        }

    
        public static void PushAll<TT>(this Stack<TT> dest, IEnumerable<TT> src)
        {
            if (src == null)
                return;
            if (dest == null)
                dest = new Stack<TT>();
            foreach (var srcObj in src)
            { 
                try
                {
                    dest.Push(srcObj);
                }
                catch
                {
                }
            }
        }

    
        public static string ExtractObject(this object objs, string split = null, string xMember = null, Delegate xMemLink = null)
        {
            string @out = "";
            Int64 up = 0;
            var type = objs.GetType();
            dynamic dynamicObject = objs;
            ICollection enumerable = objs as ICollection; 
            if (type.IsArray)
            {
                up = ((Array)objs).Length;
            }
            else if (enumerable!=null)
            {
                up = enumerable.Count;
            }
            
            if (up == 0)
                return "";
            if (split == null)
                split = ", ";
            for (Int64 i = 0; i <= up - 1; i++)
            {
                string val = dynamicObject[i].ToString();
                if (xMemLink != null)
                {
                    try
                    {
                        if ((xMember != null) & xMemLink != null)
                        {
                            val = xMemLink.DynamicInvoke(dynamicObject[i], xMember).ToString();
                        }
                    }
                    catch
                    {
                        val = dynamicObject[i].ToString();
                    }
                }
                @out += val;
                if (i < up - 1)
                    @out += split;
            }
            return @out;
        }
    
        public static TT[] Splice<TT>(TT[] arr, Int32 index, Int32 len)
        {
            if (len == -1)
                len = arr.Length - index;
            Array @out = new TT[len];
            Array.Copy(arr, index, @out, 0, len);
            return @out as TT[];
        }

        public static TElement RemoveAt<TElement>(this IList<TElement> @base, ref IEnumerable<TElement> dest, Int32 index)
        {
            if (dest is Stack<TElement>)
            {
                return ((Stack<TElement>)dest).Pop();
                //return default(TElement);
            }
            else if (dest is List<TElement>)
            {
                TElement elem = @base[index];
                ((List<TElement>)dest).RemoveAt(index);
                return elem;
            }
            return default(TElement);
        }

        public static object Join<TElement>(
            this IList<TElement> @base,
            ref IList<TElement> dest,
            IEnumerable<TElement> src)
        {
            if (dest is Stack<TElement>)
            {
                ((Stack<TElement>)dest).PushAll(src);
            }
            else if (dest is List<TElement>)
            {
                ((List<TElement>)dest).AddRange(src);
            }
            return dest;
        }



        /// <summary>
        /// Performs a foreach action on the element collection.
        /// </summary>
        /// <typeparam name="t">The type of the collection</typeparam>
        /// <param name="enum">The collection</param>
        /// <param name="action">The action to perform on each element</param>
        /// <remarks></remarks>
        public static List<object> ForEach<t>(this IEnumerable<t> @enum, Func<t, object> action)
        {
            if (action == null)
                throw new InvalidOperationException();
            return @enum.Select(action).ToList();
        }

        public static void For(ICollection<int> iEnumerable, Action<Int32> p2)
        {
            for (Int32 i = 0; i <= iEnumerable.Count; i++)
            {
                p2(i);
            }
        }

        public static void Append<tKey, tval>(this Dictionary<tKey, List<tval>> @base, Dictionary<tKey, List<tval>> newDict)
        {
            foreach (tKey key in newDict.Keys)
            {
                if (@base.ContainsKey(key))
                {
                    @base[key].AddRange(newDict[key]);
                }
                else
                {
                    @base[key] = newDict[key];
                }
            }
        }

        public static TT[][] Splice<TT>(this TT[] src, Int32 segments)
        {
            var out2 = new TT[segments][];
            if (src.Length / 2 < segments) { out2 = new TT[][] { }; return null; }
            Int64 varPi = src.Length / segments;
            Int64 leftover = src.Length % segments;
            for (var curSegment = 1; curSegment <= segments; curSegment++)
            {
                var start = (curSegment * varPi) - varPi;
                var end = curSegment * varPi;
                out2[curSegment - 1] = new TT[(end - start)];
                Array.Copy(src, start, out2[curSegment - 1], 0, end - start);
            }
            if (leftover > 0)
            {
                Array.Resize(ref out2, out2.Length - 1 + 2);
                out2[out2.Length - 1] = new TT[leftover];
                Array.Copy(src, src.Length - leftover, out2[out2.Length - 1], 0, leftover);
            }
            return out2;
        }

        public static List<List<TT>> ChunkArray<TT>(this IEnumerable<TT> src, Int32 segments)
        {
            return src.ToArray().ChunkArray(segments);
        }
        public static List<List<TT>> ChunkArray<TT>(this TT[] src, Int32 segments)
        {
            var out2 = new TT[segments][];
            if (src.Length / 2 < segments) { out2 = new TT[][] { }; return null; }
            Int64 varPi = src.Length / segments;
            Int64 leftover = src.Length % segments;
            for (var curSegment = 1; curSegment <= segments; curSegment++)
            {
                var start = (curSegment * varPi) - varPi;
                var end = curSegment * varPi;
                out2[curSegment - 1] = new TT[(end - start)];
                Array.Copy(src, start, out2[curSegment - 1], 0, end - start);
            }
            if (leftover > 0)
            {
                Array.Resize(ref out2, out2.Length - 1 + 2);
                out2[out2.Length - 1] = new TT[leftover];
                Array.Copy(src, src.Length - leftover, out2[out2.Length - 1], 0, leftover);
            }
            return out2.Select(x => x.ToList()).ToList();
        }


        public static Dictionary<tKey, List<tVal>> AddEx<tKey, tVal>(this Dictionary<tKey, List<tVal>> dict, tKey key, tVal value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key].Add(value);
            }
            else
            {
                dict.Add(key, new List<tVal>(new tVal[] { value }));
            }
            return dict;
        }
        public static Dictionary<tKey, tVal> AddEx<tKey, tVal>(this Dictionary<tKey, tVal> dict, tKey key, tVal value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = (value);
            }
            else
            {
                dict.Add(key, value);
            }
            return dict;
        }


        public static void Add<tValue>(this tValue[] arr, tValue value)
        {
            Array.Resize(ref arr, arr.Length + 1);
            arr[arr.Length - 1] = value;
        }

        public static void Add<TValue>(ref TValue[] refarr, TValue value)
        {
            Array.Resize(ref refarr, refarr.Length + 1);
            refarr[refarr.Length - 1] = value; 
        }
    }
}