using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Netlyt.Data.SQL;

namespace Netlyt.Data
{
    public static partial class Extensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements"></param>
        /// <returns></returns>
        public static int GetUnorderedHashcode<T>(this IEnumerable<T> elements)
        {
            int hash = 0;
            foreach (var elem in elements)
            {
                if (elem == null) continue;
                hash = hash ^ elem.GetHashCode();
            }

            return hash;
        }

        public static int IndexOfAny(this string src, out string delimiter, params String[] values)
        {
            var delimiters = new Stack<String>();
            delimiter = null;
            for (int i = 0; i < values.Length; i++)
            {
                delimiters.Push(values[i]);
            }

            var index = -1;
            while (delimiters.Count > 0)
            {
                var del = delimiters.Pop();
                if (0 <= (index = src.IndexOf(del)))
                {
                    delimiter = del;
                    break;
                }
            }

            ;
            if (index == -1) delimiter = null;
            return index;
        }

        /// <summary>
        /// Deep clone a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listToClone"></param>
        /// <returns></returns>
        public static IList<T> DeepClone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T) item.Clone()).ToList();
        }

        /// <summary>
        /// Clone a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listToClone"></param>
        /// <returns></returns>
        public static IList<T> Clone<T>(this IList<T> listToClone)
        {
            return listToClone.Select(item => (T) item).ToList();
        }

        /// <summary>
        /// Clone a dictionary
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="dictToClone"></param>
        /// <returns></returns>
        public static Dictionary<A, T> Clone<A, T>(this IDictionary<A, T> dictToClone)
        {
            var xdict = new Dictionary<A, T>();
            foreach (var keyValue in dictToClone)
            {
                xdict.Add(keyValue.Key, keyValue.Value);
            }

            return xdict;
        }

        public static IEnumerable<T> CastObj<T>(this Object iEnumObj)
        {
            if (iEnumObj.GetType() == typeof(T[]))
                return (T[]) iEnumObj;
            else if (iEnumObj.GetType() == typeof(IEnumerable<T>)) return (IEnumerable<T>) iEnumObj;
            else return new List<T>(new T[] {(T) iEnumObj});
        }

        public static object CastObj(this Object target, Type totype)
        {
            var typeInstance = Ctor(totype, null);
            typeInstance = target;
            return typeInstance;
        }

        public static bool Contains<T>(this IEnumerable<T> coll, Func<T, bool> predicate, out T value)
        {
            value = (from obj in coll where predicate(obj) select obj).Take(1).FirstOrDefault();
            return value != null;
        }

        public static bool Contains<T>(this IEnumerable<T> coll, Func<T, bool> predicate)
        {
            T vl;
            return coll.Contains<T>(predicate, out vl);
        }

        public static TT[] Copy<TT>(this TT[] arr)
        {
            var arrNew = new TT[arr.Length];
            Buffer.BlockCopy(arr, 0, arrNew, 0, arr.Length);
            return arrNew;
        }

        //public static t[] Shuffle<t>(this t[] arr)
        //{ 
        //    t tmp = default(t);
        //    for (Int32 i = 0; i <= arr.Length - 1; i++)
        //    {
        //        long r = Security.Randomization.rndF(0, arr.Length - 1); 
        //        tmp = arr[(int)r];
        //        arr[(int)r] = arr[i];
        //        arr[i] = tmp;
        //    }
        //    return arr;
        //}

        public static t[] Compose<t>(this t[][] arrays, t[] splitter = null, Func<t[], t[]> decorator = null)
        {
            var output = new t[0]; //TODO:  FIX THIS TO BE BUFFERED 
            foreach (t[] array in arrays)
            {
                if (!HasVal(array))
                {
                    if (HasVal(splitter)) output.Append(ref output, splitter);
                }
                else
                {
                    output.Append(ref output, array);
                    if (HasVal(splitter)) output.Append(ref output, splitter);
                }
            }

            if (decorator != null) output = decorator(output);
            return output;
        }

        public static bool HasVal(Array objects)
        {
            return objects != null && objects.Length > 0;
        }

        public static bool HasVal(object obj)
        {
            if (obj == null)
                return false;
            if (obj is string)
                return HasVal((string) obj);
            if (obj is bool)
                return true;
            Type objt = obj.GetType();
            if (objt.Name == "List`1")
                return ((ICollection) obj).Count != 0;
            if (obj is Array)
                return ((Array) obj).Length != 0;
            while (objt.BaseType != null)
            {
                if (objt.BaseType.Name == "List`1")
                    return ((ICollection) obj).Count != 0;
                objt = objt.BaseType;
            }

            return true;
        }

        public static bool HasVal(string str)
        {
            return !string.IsNullOrEmpty(str);
        }

        public static bool HasVal(byte[] str)
        {
            if (str == null)
                return false;
            if (str.Length == 0)
                return false;
            return true;
        }

        public static void Append<t>(this t[] @base, ref t[] dest, t[] source)
        {
            int totalLength = dest.Length + source.Length;
            if (totalLength <= 0) return;
            Array.Resize(ref @base, totalLength);
            Array.Copy(source, @base, source.Length);
            dest = @base;
        }

        public static void Append<t>(this t[] @base, ref t[] dest, long destOffset, t[] source, long srcOffset,
            long srcLength)
        {
            long totalLength = (dest.Length - destOffset);
            // throw new NotImplementedException();
            totalLength += (srcLength == 0 ? source.Length : srcLength) - srcOffset;
            if (totalLength <= 0) return;
            Array.Resize(ref @base, (int) totalLength);
            if (srcLength <= 0) srcLength = source.Length - srcOffset;

            Array.Copy(source, srcOffset, @base, destOffset, srcLength);
            dest = @base;
        }

        /// <summary>
        /// Packs two byte arrays together. In the following format:
        /// 0x0     - / 32 // processor-bit support
        /// 0x1-0x4 - size of the furst pack
        /// ... pack 1 (of the upper size)
        /// ... pack 2
        /// </summary>
        /// <param name="pack1"></param>
        /// <param name="pack2"></param>
        /// <returns></returns>
        public static byte[] Pack(this byte[] pack1, byte[] pack2)
        {
            byte[] @out = new byte[1];
            @out[0] = Is32Bit() ? (byte) 1 : (byte) 2;
            MemoryStream ms = new MemoryStream();
            ms.Write(@out, 0, @out.Length);
            BinaryWriter br = new BinaryWriter(ms);
            br.Write(ToBytes(pack1.Length));
            br.Write(pack1);
            br.Write(pack2);
            return ms.ToArray();
        }

        public static byte[] ToBytes(Int32 @int)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(@int);
            bw.Flush();
            ms.Close();
            return ms.ToArray();
        }

        public static byte[] ToBytes(Int64 @int)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(@int);
            bw.Flush();
            ms.Close();
            return ms.ToArray();
        }

        public static bool Is64Bit()
        {
            is64BitCached = Marshal.SizeOf(typeof(IntPtr)) == 8 ? true : false;
            return is64BitCached.Value;
        }

        private static bool? is64BitCached = null;

        public static bool Is32Bit()
        {
            return !Is64Bit();
        }

        public static Int16 ToInt16(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return new BinaryReader(ms).ReadInt16();
            }
        }

        public static UInt16 ToUInt16(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return new BinaryReader(ms).ReadUInt16();
            }
        }

        public static Int32 ToInt32(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return new BinaryReader(ms).ReadInt32();
            }
        }

        public static Int64 ToInt64(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return new BinaryReader(ms).ReadInt64();
            }
        }

        public static Int32 ToInt32(Int64 val)
        {
            byte[] bla = ToBytes(val);
            return ToInt32(new byte[] {bla[0], bla[1], bla[2], bla[3]});
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[][] Unpack(this byte[] bytes)
        {
            MemoryStream ms = new MemoryStream();
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;
            BinaryReader br = new BinaryReader(ms);
            byte bitMultiplier = br.ReadByte();
            byte intSize = (byte) (4 * bitMultiplier);
            Int32 offset = ToInt32(br.ReadBytes(intSize));
            byte[] b1 = br.ReadBytes(offset);
            byte[] b2 = br.ReadBytes((int) ms.Length - (int) ms.Position);
            return new byte[][]
            {
                b1, b2
            };
        }


        /// <summary>
        /// Read all bytes from a stream, using a defined offset.
        /// </summary>
        /// <param name="ms">The stream</param>
        /// <param name="offset">The offset</param>
        /// <param name="len">The ammount of bytes to read</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static byte[] ToArrayEx(this MemoryStream ms, long offset, long len = -1, bool resetOffset = false)
        {
            byte[] functionReturnValue = null;
            if (ms == null)
                return null;
            //    If ms.Length = 0 Then Return Nothing
            if (len > ms.Length)
                len = ms.Length - 1;
            if ((ms.Length - offset) < len)
                len = ms.Length - offset;

            BinaryReader mbr = new BinaryReader(ms);
            if (offset >= 0) ms.Position = offset;
            functionReturnValue = (mbr.ReadBytes((int) (len <= 0 ? ms.Length - ms.Position : len)));

            if (resetOffset)
            {
                try
                {
                    ms.Position = 0;
                }
                catch
                {
                }
            }

            return functionReturnValue;
        }

        public static byte[] ToArrayEx(this MemoryStream ms)
        {
            return ms.ToArrayEx(ms.Position);
        }

        public static t[] Replace<t>(this t[] bSrc, ref t[] bDest, t[] bTarget, t[] bReplacement)
        {
            Int32 matchindex = -1;
            for (Int32 i = 0; i <= bSrc.Length - 1; i++)
            {
                if ((bTarget.Length + i) > bSrc.Length)
                    return bSrc;
                //(Current position + target length) is too big for a next match to be found...
                if (bSrc[i].Equals(bTarget[0]))
                {
                    for (Int32 iMatch = 1; iMatch <= bTarget.Length - 1; iMatch++)
                    {
                        if (!bSrc[i + iMatch].Equals(bTarget[iMatch]))
                            continue;
                    }

                    matchindex = i;
                    //ISMATCH
                    if ((bTarget.Length != bReplacement.Length))
                    {
                    }
                    else
                    {
                        bSrc.Shift(ref bSrc, matchindex, bReplacement.Length - bTarget.Length);
                    }

                    bSrc.Overwrite(ref bSrc, matchindex, bReplacement);
                    return bSrc;
                }
            }

            bDest = bSrc;
            return bSrc;
        }

        public static void Shift<t>(this t[] arr, ref t[] dist, int indexKey, int offset = 1)
        {
            if (offset > 0)
                Array.Resize(ref arr, arr.Length - 1 + offset + 1);
            for (int i = arr.Length - 1; i >= indexKey; i--)
            {
                if (i == 0) return;
                arr[i] = arr[Math.Abs(i - offset)];
            }

            dist = arr;
        }

        public static void Overwrite<TT>(this TT[] arr, ref TT[] dest, int index, TT[] vals, int len = -1)
        {
            if (arr.Length - index < vals.Length)
                throw new Exception(string.Format(
                    "Array is not long enough to be overwritten at [{0}] with an array of size {1}!", index,
                    vals.Length));
            if ((index + len) > arr.Length)
                return;
            long total = (arr.Length - index) + (len > 0 ? len : vals.Length);
            dest = new TT[total];
            Array.Copy(arr, dest, arr.Length - index);
            Array.Copy(vals, 0, dest, arr.Length - index, (len > 0 ? len : vals.Length));
        }

        public static t[][] Splice<t>(this t[] bin, t[] splitbits)
        {
            List<t[]> @out = new List<t[]>();
            if (splitbits.Length > bin.Length) return new t[][] {bin};
            int lastIndex = 0;
            for (int i = 0; i < bin.Length; i++)
            {
                bool validIndex = false;
                if (bin.Length - i <= splitbits.Length)
                {
                    int start = bin.LastIndexOf(splitbits) + splitbits.Length;
                    if (start + 1 == splitbits.Length) return new t[][] {bin};
                    int len = bin.Length - start;
                    t[] bar = new t[] { }; //New ArrayList() {}
                    if (len > 0)
                    {
                        bar = bin.Slice(start, len);
                        @out.Add(bar);
                        break;
                    }
                    else if (len == 0)
                    {
                        bar = bin.Slice(lastIndex, bin.Length - (lastIndex + splitbits.Length));
                        @out.Add(bar);
                        break;
                    }
                }

                if (bin[i].Equals(splitbits[0]))
                {
                    for (int iSearch = 0; iSearch < splitbits.Length; iSearch++)
                    {
                        if (!bin[i + iSearch].Equals(splitbits[iSearch])) continue;
                    }

                    validIndex = true;
                }

                if (validIndex)
                {
                    t[] bar = bin.Slice(lastIndex, (i) - lastIndex);
                    @out.Add(bar);
                    lastIndex = i + splitbits.Length;
                }
            }

            return @out.ToArray();
        }

        public static t[] Slice<t>(this t[] arrSrc, int index, Int32 len = -1)
        {
            if (len == -1)
                len = arrSrc.Length - index;
            t[] @out = new t[len];
            Array.Copy(arrSrc, index, @out, 0, len);
            return @out;
        }

        public static void Resize<TT>(this TT[] arr, ref TT[] dest, int newLength)
        {
            Array.Resize(ref arr, newLength);
            dest = arr;
        }

        public static Int32 IndexOf<TT>(this TT[] bin, TT[] search)
        {
            for (int i = 0; i < (bin.Length - search.Length); i++)
            {
                bool validIndex = true;
                int srchEl = 0;
                for (int iSearch = 0; iSearch < search.Length; iSearch++)
                {
                    if (iSearch > i)
                        continue;
                    if (!(bin[i - iSearch].Equals(search[srchEl])))
                    {
                        validIndex = false;
                        break; // TODO: might not be correct. Was : Exit For
                    }

                    srchEl += 1;
                }

                if (validIndex)
                    return i - (search.Length - 1);
            }

            return -1;
        }

        /// <summary>
        /// Finds the index of an array inside an array.
        /// </summary>
        /// <param name="bin">The source array</param>
        /// <param name="search">The array to look for</param>
        /// <returns></returns>
        /// Returns the indexof of the specified array inside the first array. If not contained, -1 is returned.
        /// <remarks></remarks>
        public static Int32 LastIndexOf<TT>(this TT[] bin, TT[] search)
        {
            for (int i = bin.Length - 1; i >= 0; i--)
            {
                bool validIndex = true;
                int srchEl = 0;
                for (int iSearch = search.Length - 1; iSearch >= 0; iSearch--)
                {
                    if (iSearch > i)
                        continue;
                    if (!(bin[i - iSearch].Equals(search[srchEl])))
                    {
                        validIndex = false;
                        break; // TODO: might not be correct. Was : Exit For
                    }

                    srchEl += 1;
                }

                if (validIndex)
                    return i - (search.Length - 1);
            }

            return -1;
        }

        public static String GetGUID()
        {
            try
            {
                return System.Guid.NewGuid().ToString();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return null;
            }
        }

        public static byte[] ToArray(this MemoryStream ms,
            int offset, long len = -1, bool resetOffset = false)
        {
            if (ms == null) return null;
            if (len > ms.Length) len = ms.Length - 1;
            if ((ms.Length - offset) < len) len = ms.Length - offset;
            BinaryReader mbr = new BinaryReader(ms);
            if (offset >= 0) ms.Position = offset;
            byte[] result = mbr.ReadBytes(len <= 0 ? (int) ((int) ms.Length - (int) ms.Position) : (int) len);
            if (resetOffset)
            {
                try
                {
                    ms.Position = 0;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                }
            }

            return result;
        }

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
        public static bool TryDequeue<TSource>(this ConcurrentQueue<TSource> cQueue, Int32 takeCount,
            Action<TSource> actionPerElement)
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
                System.Threading.Thread.Sleep(1);
            }
        }

        public static IList<IList<T>> SplitGroups<T>(this IEnumerable<T> enumerable, Int32 groupSz)
        {
            var collection = enumerable.ChunkArray(groupSz).ToList();
            return collection as IList<IList<T>>;
        }

        /// <summary>   Attempts to create bson value a BsonValue from the given object. </summary>
        ///
        /// <remarks>   Cyb R, 14-Dec-17. </remarks>
        ///
        /// <param name="obj">  The object. </param>
        /// <param name="val">  [in,out] The value. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        public static bool TryCreateBsonValue(object obj, ref BsonValue val)
        {
            if (!IsBasicObject(obj))
                return false;
            try
            {
                val = BsonValue.Create(obj);
                if (val == null)
                    val = BsonNull.Value;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsBasicObject(object arg)
        {
            return arg is string | arg is byte | arg is sbyte | arg is UInt16 | arg is Int16 | arg is UInt32 |
                   arg is Int32 | arg is UInt64 | arg is Int64 | arg is bool;
        }

        public delegate TT1 FuncRef<TT, TT1>(ref TT a);

        public delegate TT2 FuncRef<TT, TT1, TT2>(ref TT a, ref TT1 b);

        public static List<TTelement> WalkNonRecursive<TTelement, TMember>(
            this IEnumerable<TTelement> roots,
            FuncRef<TTelement, bool, bool> @while,
            Func<TTelement, IEnumerable<TTelement>, IEnumerable<TTelement>> fetcher,
            Func<TTelement, TMember> selector)
        {
            IList<TTelement> lst = new List<TTelement>();
            List<TTelement> output = new List<TTelement>();
            WalkNonRecursive<TTelement>(ref lst, roots.ToArray(), false,
                (ref TTelement elem, ref bool valid) =>
                {
                    bool retVal = @while(ref elem, ref valid);
                    dynamic elemCpy = elem;
                    if (retVal && valid && !output.Contains(x => selector(x).Equals(selector(elemCpy))))
                    {
                        output.Add(elem);
                    }

                    return retVal;
                },
                fetcher);
            return output;
        }

        public static T Ctor<T>(this Type target, object data)
        {
            return (T) Ctor(target, data);
        }

        public static object Ctor(this Type target, object data)
        {
            if (data == null)
                return null;
            ConstructorInfo ctr = target.GetConstructor(new Type[] {data.GetType()});
            return ctr?.Invoke(new Object[] {data});
        }

        public static object Ctor(this Type target, params object[] data)
        {
            if (data == null) return null;
            var argumentTypes = new List<Type>();
            foreach (var dataElem in data) argumentTypes.Add(dataElem.GetType());

            ConstructorInfo ctr = target.GetConstructor(argumentTypes.ToArray());
            return ctr?.Invoke(data);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="collection"></param>
        /// <param name="roots"></param>
        /// <param name="while">Do while this true. First argument is a reference to the object to check and 
        /// the second one is the flag for validity.
        /// If the predicate returns false, the object is skipped.
        /// If the predicate returns true but the invalid flag is set, then the object checks for validity.
        /// If the validity flag is set to true, the fetches is invoked, and a findFirstOnly is evaluated, based on arguments.</param>
        /// <param name="fetcher">Fetches subrelatives</param>
        /// <remarks></remarks>
        public static bool WalkNonRecursive<TElement>(
            ref IList<TElement> collection,
            TElement[] roots,
            bool findFirstOnly,
            FuncRef<TElement, bool, bool> @while,
            Func<TElement, IEnumerable<TElement>,
                IEnumerable<TElement>> fetcher)
        {
            if (collection == null)
                collection = Ctor<IList<TElement>>(collection.GetType(), null);
            if (roots != null)
                collection.Join(ref collection, roots);
            //seed the stack 


            while (collection.Count > 0)
            {
                TElement cVal = collection.FirstOrDefault();
                if (collection.Count > 0)
                    collection.RemoveAt(0);
                bool isValid = false;
                if (!@while(ref cVal, ref isValid))
                    continue;
                try
                {
                    if (isValid)
                    {
                        IEnumerable<TElement> fetched = fetcher(cVal, collection);
                        collection.Join(ref collection, fetched);
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }

                if (isValid)
                {
                    if (findFirstOnly)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the OR filter for the given types. Compared with the given matchValue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="types">The types with witch do build the filter.</param>
        /// <param name="matchingValue">The value to check for.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static Func<object, bool> CompileFilter<T>(this Type[] types, T matchingValue)
            where T : class
        {
            return CompileFilter<T>(matchingValue, types);
        }


///// <summary>
///// Gets the OR filter for the given types. Compared with the given matchValue
///// </summary>
///// <typeparam name="T"></typeparam>
///// <param name="types">The types with witch do build the filter.</param>
///// <param name="matchingValue">The value to check for.</param>
///// <returns></returns>
///// <remarks></remarks>
//public static Func<TypedEntity, bool> CompileTypedFilter<T>(this Type[] types, T matchingValue)
//    where T : TypedEntity
//        {
//    return CompileFilter<T>(matchingValue, types);
//}

        public static HashSet<AssemblyWrapper> GetAssembliesToMap(this HashSet<AssemblyWrapper> asms,
            List<string> includedAssemblies)
        {
            if (includedAssemblies != null && includedAssemblies.Count > 0)
            {
                asms = asms.Where(tmpAsm => { return includedAssemblies.Any(x => x == tmpAsm.ShortName); }).ToHashSet();
            }

            return asms;
        }


        /// <summary>
        /// Gets the OR filter for the given types. Compared with the given matchValue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="matchValue"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static Func<object, bool> CompileFilter<T>(T matchValue, params Type[] types)
        {
            return (object item) =>
            {
                var result = new List<Func<object, bool>>();
                foreach (Type type in types)
                {
                    //if (item is User) {
                    //                   if (typeof(T) == typeof(String)) { 
                    //	    result.Add(x =>  ((User)x).Username.Equals(matchValue) );
                    //                   }

                    //} else if (item is Article) {
                    //    if (typeof (T) == typeof (String))
                    //    {
                    //        result.Add(x => ((Article) x).Title.Equals(matchValue));
                    //    }
                    //} else if (item is Course) {
                    //    if (typeof (T) == typeof (String))
                    //    {
                    //        result.Add(x => ((Course) x).Title.Equals(matchValue));
                    //    }
                    //}
                }

                //'OR for all the predicate checkers.
                return result.Any(predicate => predicate(item));
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TT"></typeparam>
        /// <param name="stack"></param>
        /// <param name="roots"></param>
        /// <param name="while">Do while this true. First argument is the object to check and the second one is the flag for validity.</param>
        /// <param name="fetcher">Fetches subrelatives</param>
        /// <remarks></remarks>
        public static bool WalkNonRecursive<TT>(
            this Stack<TT> stack,
            TT[] roots,
            bool findFirstOnly,
            FuncRef<TT, bool, bool> @while,
            Func<TT, IEnumerable<TT>> fetcher)
        {
            if (stack == null)
                stack = new Stack<TT>();
            if (roots != null)
                stack.PushAll(roots);
            //seed the stack 
            while (stack.Count > 0)
            {
                TT cVal = stack.Pop();
                bool isValid = false;
                if (!@while(ref cVal, ref isValid))
                    continue;
                try
                {
                    if (isValid)
                        stack.PushAll(fetcher(cVal));
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                }

                if (isValid)
                {
                    if (findFirstOnly)
                        return true;
                }
            }

            return false;
        }


        /// <summary>
        /// /
        /// </summary>
        /// <returns></returns>
        public static List<AssemblyWrapper> GetEntryAssemblyReferences()
        {
            var entry = Assembly.GetEntryAssembly();
            var entryRefs = entry.GetAssemblyReferences();
            return entryRefs;
        }

        /// <summary>
        /// Get the main assembly from the application domain
        /// </summary>
        /// <returns></returns>
        public static RuntimeLibrary GetApplicationDomainAssembly()
        {
            var myAsmName = Assembly.GetEntryAssembly().GetName().Name;
            var domainAssemblies = GetReferencingAssemblies(myAsmName);
            var friendlyName = myAsmName;

            RuntimeLibrary domainAssembly = domainAssemblies.FirstOrDefault(asm =>
            {
                //var asmName = asm.GetName().Name;
                var asmName = asm.Name;
                return friendlyName.Contains(asmName); //AppDomain.CurrentDomain.FriendlyName.Contains(asmName);
            });
            return domainAssembly;
        }

        public static HashSet<AssemblyWrapper> GetProjectReferences(PersistanceSettings config)
        {
            var allAssemblies = new HashSet<AssemblyWrapper>();
            if (config.LoadLocalAssemblies)
            {
                var locals = GetLocalNonLoadedAssemblies();
                foreach (var local in locals)
                {
                    allAssemblies.Add(local);
                }
            }

            //Get the references of this application assembly.
            var domainLib = GetApplicationDomainAssembly();
            List<AssemblyWrapper> currentAssemblyReferences = domainLib.GetReferencedAssemlyNames();
            foreach (var currentReference in currentAssemblyReferences)
            {
                allAssemblies.Add(currentReference);
            }

            return allAssemblies;
        }

        public static List<AssemblyWrapper> GetAssemblyReferences(this Type type)
        {
            if (type == null)
                return null;
            return type.Assembly.GetAssemblyReferences();
        }

        /// <summary>
        /// Gets a list of the base assembly's refrenced assemblies.
        /// 
        /// </summary>
        /// <param name="baseAssembly"></param>
        /// <returns></returns>
        public static List<AssemblyWrapper> GetReferencedAssemliesList(this Assembly baseAssembly)
        {
            //            Trace.Write(baseAssembly.FullName);
            //Trace.WriteLine("_____________________________________");
            return baseAssembly?.GetReferencedAssemblies()?.Select(asmName =>
            {
                try
                {
                    Assembly reflectedAsm = Assembly.Load(asmName.FullName);
                    return new AssemblyWrapper(reflectedAsm, asmName);
                }
                catch (Exception)
                {
                    return new AssemblyWrapper
                    {
                        Missing = true,
                        Name = asmName
                    };
                }
            })?.ToList();
        }

        public static List<AssemblyWrapper> GetReferencedAssemliesList(this AssemblyWrapper asmxx)
        {
            if (asmxx == null || asmxx.Assembly == null)
                return null;
            return asmxx.Assembly.GetReferencedAssemliesList();
        }


        /// <summary>
        ///Internal recursive class to get all dependent EntryCollection, and all dependent EntryCollection of
        ///     dependent EntryCollection, etc.
        /// </summary> 
        public static List<AssemblyWrapper> GetAssemblyReferences(this Assembly asm, int maxDepth = 1)
        {
            // Load EntryCollection with newest versions first. Omitting the ordering results in false positives on
            // _missingAssemblyList.
            List<AssemblyWrapper> baseAsms = asm.GetReferencedAssemliesList();
            if (baseAsms != null)
            {
                baseAsms.RemoveAll(x => x.GlobalAssemblyCache);
            }

            //baseAsms.RemoveAll(x => runtimeLibs.Contains(r => r.Name == x.Name.Name));
            //            baseAsms = baseAsms.ToArray()
            //                .WalkNonRecursive(validator, fetcher, (AssemblyWrapper assembly) => assembly.FullName);
            //            baseAsms.RemoveAll(x => x.GlobalAssemblyCache);
            return baseAsms;
        }


        /// <summary>
        /// Gets a list of the base assembly's refrenced assemblies.
        /// 
        /// </summary>
        /// <param name="baseAssembly"></param>
        /// <returns></returns>
        public static List<AssemblyWrapper> GetReferencedAssemlyNames(this RuntimeLibrary baseLib)
        {
            var deps = DependencyContext.Default;
            //            Trace.Write(baseAssembly.FullName);
            //Trace.WriteLine("_____________________________________");
            return baseLib?.GetDefaultAssemblyNames(deps)?.Select(asmName =>
            {
                try
                {
                    return new AssemblyWrapper(null, asmName);
                }
                catch (Exception)
                {
                    return new AssemblyWrapper
                    {
                        Missing = true,
                        Name = asmName
                    };
                }
            })?.ToList();
        }

        /// <summary>
        /// Gets referenced assemblies, like AppDomain.GetAssemblies
        /// </summary>
        /// <param name="asmName"></param>
        /// <returns></returns>
        public static IEnumerable<RuntimeLibrary> GetReferencingAssemblies(string asmName)
        {
            var dependancies = DependencyContext.Default.RuntimeLibraries;
            foreach (var library in dependancies)
            {
                if (IsCandidateLibrary(library, asmName))
                {
                    //var assembly = Assembly.Load(new AssemblyName(library.Name));
                    yield return library;
                }
            }
        }

        private static bool IsCandidateLibrary(RuntimeLibrary library, string assemblyName)
        {
            return library.Name == (assemblyName)
                   || library.Dependencies.Any(d => d.Name.StartsWith(assemblyName));
        }


        /// <summary>
        /// Gets the assemblies in the binary's folder, which are not currenly loaded.
        /// </summary>
        /// <returns></returns>
        public static List<AssemblyWrapper> GetLocalNonLoadedAssemblies()
        {
            var myAsmName = Assembly.GetEntryAssembly().GetName().Name;
            var loadedLibs = GetReferencingAssemblies(myAsmName); //AppDomain.CurrentDomain.GetAssemblies().ToList();
            List<String> loadedPaths = new List<String>();
            foreach (var asm in loadedLibs)
            {
                try
                {
                    loadedPaths.Add(asm.Path);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }
            }

            var baseDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var referencedDllPaths = Directory.GetFiles(baseDirectory, "*.dll").Where(dll =>
            {
                var dllName = Path.GetFileName(dll);
                return !dllName.StartsWith("System.");
            });
            var nonLoadedAssemblies = referencedDllPaths.Where(r =>
            {
                return !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase);
            }).ToList();
            return nonLoadedAssemblies.Select(x => { return new AssemblyWrapper(x); }).ToList();
            //nonLoadedAssemblies.ForEach(path => loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path))));
        }


        public static void PushAll<TT>(this Stack<TT> dest, IEnumerable<TT> src)
        {
            if (src == null)
                return;
            if (dest == null)
                dest = new Stack<TT>();
            foreach (TT srcObj in src)
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

        public static string ExtractObject(this object objs, string split = null, string xMember = null,
            Delegate xMemLink = null)
        {
            string @out = "";
            Int64 up = 0;
            string typNm = objs.GetType().Name;
            if (typNm.EndsWith("[]"))
                up = (((Array) objs).Length - 1) + 1;
            else if (objs.GetType().Name.Contains("List`1"))
                up = ((ICollection) objs).Count;
            if (up == 0)
                return "";
            if (split == null)
                split = ", ";
            for (Int32 i = 0; i <= up - 1; i++)
            {
                string val = ((ArrayList) objs)[i].ToString();
                if (xMemLink != null)
                {
                    try
                    {
                        if (HasVal(xMember) & xMemLink != null)
                            val = xMemLink.DynamicInvoke(((ArrayList) objs)[i], xMember).ToString();
                    }
                    catch
                    {
                        val = ((ArrayList) objs)[i].ToString();
                    }
                }

                @out += val;
                if (i < up - 1)
                    @out += split;
            }

            return @out;
        }

        public static string ExtractException(this Exception ex)
        {
            var sb = new StringBuilder();
            ex.WalkException((x) => { sb.AppendLine(x); }, true);
            return sb.ToString();
        }

        public static void WalkException(this Exception exception, Action<string> action, bool recursive = false)
        {
            if (action == null)
                return;
            action(exception.Message);
            if (!recursive)
                return;
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                action(exception.Message);
            }
        }

        /// <summary>
        /// Substrings a string, until it's first comma.
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string SubComma(this string fullName)
        {
            return fullName.Split(',').FirstOrDefault();
        }

        public static object Splice<TT>(this TT[] arr, Int32 index, Int32 len)
        {
            if (len == -1)
                len = arr.Length - index;
            Array @out = new byte[len];
            Array.Copy(arr, index, @out, 0, len);
            return @out;
        }

        /// <summary>
        /// Shows all errors in messageboxes.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="recursive"></param>
        /// <remarks></remarks>
        public static void ShowErrors(Exception exception, bool recursive = false)
        {
            Debug.WriteLine(string.Format("Exception: {0}", exception.Message));
            if (!recursive)
                return;
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                Debug.WriteLine(string.Format("Exception: {0}", exception.Message));
            }
        }
        
          #region "Deprecation Fixes"
        /// <summary>
        /// Fix deprecation issues.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="type"></param>
        /// <returns></returns> 
        public static BsonMemberMap SetRepresentation(this BsonMemberMap map, BsonType type)
        {
            if (map == null)
                return map;
            map.SetSerializer(new StringSerializer(type));
            return map;
        }


        #endregion

        public static DateTime? GetDate(this BsonDocument document, string key)
        {
            if (document == null || !document.Contains(key)) return null;
            if(document[key].GetType() == typeof(BsonDateTime))
            {
                return document[key].ToUniversalTime();
            }
            var str = document[key].AsString;
            DateTime oDate;
            if (DateTime.TryParse(str, out oDate)) return oDate;
            else return null;
        }
        public static int? GetInt(this BsonDocument document, string key)
        {
            if (document == null || !document.Contains(key)) return null;
            if (document[key].IsNumeric)
            {
                return document[key].ToInt32();
            }
            var str = document[key].ToString();
            int oInt;
            if (int.TryParse(str, out oInt)) return oInt;
            else return null;
        }

        public static string GetString(this BsonDocument document, string key)
        {
            if (document == null || !document.Contains(key)) return null;
            var str = document[key].ToString();
            return str;
        }


        public static bool IsNumeric(this BsonDocument document, string key)
        {
            if (document == null || !document.Contains(key)) return false;
            if (document[key].IsNumeric)
            {
                return true;
            }
            {
                var str = document[key].ToString();
                int intx = 0;
                return int.TryParse(str, out intx);
            }
        }

        public static bool IndexExists<TRecord>(this IMongoCollection<TRecord> collection, string indexName)
        {
            var indexes = collection.Indexes.List().ToList();
            foreach (var index in indexes)
            {
                if (index["name"] == indexName)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IndexExists<TRecord>(this IMongoCollection<TRecord> collection, IndexKeysDefinition<TRecord> keys)
        {
            var indexes = collection.Indexes.List().ToList();
            foreach (var index in indexes)
            {
                var asdax = index["keys"];
                return true;
            }
            return false;
        }
        
        public static bool IsMappable(this Type type)
        {
            // If type is .Net return..
            return !type.IsdotNetLib(); 
        } 

        public static bool IsdotNetLib(this Type type)
        {
            return (type.Assembly.FullName.StartsWith("mscorlib") & type.FullName.StartsWith("System"));
        }
 
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSourceType"></typeparam>
        /// <typeparam name="TElementType"></typeparam>
        /// <param name="items"></param>
        /// <param name="actionMap">The relation for a given type, and the action to perform from the given type.</param>
        /// <returns></returns>
        /// <remarks></remarks> 
        public static IEnumerable<TElementType> ForEach<TSourceType, TElementType>(
            ref IEnumerable<TSourceType> items,
            Dictionary<Type, Func<IDbListBase, TElementType>> actionMap)
        {
            List<TElementType> output = new List<TElementType>();
            foreach (TSourceType itm in items)
            { 
                foreach (var kType in 
                    from x in actionMap.Keys
                    where object.ReferenceEquals(itm.GetType(), x)
                    select x) { 

                    output.Add(actionMap[kType].Invoke((IDbListBase)itm));
                }
            }
            return output;
        }
        
        public static FilterDefinition<BsonDocument> AppendAnd(this FilterDefinition<BsonDocument> qr, FilterDefinition<BsonDocument> newQuery)
        {
            if (newQuery == null)
                return qr;
            qr = Builders<BsonDocument>.Filter.And(qr, newQuery); 
            return qr;
        }

        public static async Task<ReplaceOneResult> SaveAsync<TRecord>(this IMongoCollection<TRecord> collection, TRecord newRecord)
            where TRecord : class
        {
            var entityQuery = DbQueryProvider.GetInstance().GetUniqueQuery(newRecord);
            var replaceOneResult = await collection.ReplaceOneAsync(
                entityQuery,
                newRecord,
                new UpdateOptions { IsUpsert = true });
            return replaceOneResult;
        }
        public static ReplaceOneResult SaveOrReplace<TRecord>(this IMongoCollection<TRecord> collection, TRecord newRecord)
            where TRecord : class
        {
            var entityQuery = DbQueryProvider.GetInstance().GetUniqueQuery(newRecord);
            var replaceOneResult = collection.ReplaceOne(
                entityQuery,
                newRecord,
                new UpdateOptions { IsUpsert = true }); 
            return replaceOneResult;
        }
        
    }
}