using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Netlyt.Data
{
    /// <summary>
    /// A wrapper that handles assembly names, EntryCollection and missing EntryCollection.
    /// </summary>
    /// <remarks></remarks>
    public class AssemblyWrapper : IComparable<AssemblyWrapper>
    {
        public bool Missing { get; set; }
        private string _path;
        public Assembly Assembly { get; set; }
        public AssemblyName Name { get; set; }
        public string ShortName
        {
            get
            {
                string nm = FullName;
                if (String.IsNullOrEmpty(nm))
                    return null;
                return nm.SubComma();
            }
        }
        public string FullName
        {
            get
            {
                if (Assembly == null & Name == null)
                    return null;
                if (Assembly != null)
                    return Assembly.FullName;
                if (Name != null)
                    return Name.FullName;
                return null;
            }
        }


        public AssemblyWrapper()
        {
        }

        public AssemblyWrapper(string path) : this()
        {
            _path = path;
            try
            {
                Assembly = System.Reflection.Assembly.LoadFile(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not load assembly wrapper for: {path}\n{ex.Message}");
            }
        }
        public AssemblyWrapper(Assembly asm, AssemblyName name = null)
        {
            _path = asm.Location;
            this.Name = name;
            Missing = false;
            this.Assembly = asm;
        }

        public bool GlobalAssemblyCache
        {
            get
            {
                if (Assembly == null)  return false;
                return Assembly.GlobalAssemblyCache;
            }
        }

        public void LoadAssembly()
        {
            if(Assembly==null) Assembly = System.Reflection.Assembly.LoadFile(_path);
        }

        //public bool ReflectionOnly { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(_path)) sb.Append(_path);
            return !string.IsNullOrEmpty(FullName) ? (FullName + ": " + sb ) : sb.ToString();  
        }

        public Assembly Load()
        {
            if (Assembly.ReflectionOnly)
            {
                var targetName = Assembly.GetName();
                Assembly = System.Reflection.Assembly.Load(targetName); 
                return Assembly;
            }
            else
            {    
                return Assembly;
            }
        }

        public Type GetLoadedType(Type type)
        {
            if (Assembly.ReflectionOnly)
                throw new InvalidOperationException("The assembly must not be loaded with ReferenceOnly!");
            Type output = Assembly.GetType(type.FullName);
            return output;
        }

        public int CompareTo(AssemblyWrapper other)
        {
            return other.Equals(this) ? 0 : (string.Compare(Name.Name, other.Name.Name));
        }

        public override bool Equals(object obj)
        {
            if (typeof(AssemblyWrapper) == obj.GetType())
            {
                return _path == (obj as AssemblyWrapper)._path;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            if (_path == null && Name == null)
            {
                return base.GetHashCode();
            }
            else
            {
                return string.IsNullOrEmpty(_path) ? Name.GetHashCode() : _path.GetHashCode();
            }
        }
    }
}