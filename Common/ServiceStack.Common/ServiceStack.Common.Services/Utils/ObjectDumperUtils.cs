using System;
using System.Collections;
using System.Reflection;
using System.Text;

namespace ServiceStack.Common.Services.Utils
{
    public class ObjectDumperUtils
    {
        private readonly StringBuilder builder = new StringBuilder();

        int pos;
        int level;
        readonly int depth;

        public static void Write(object o)
        {
            Write(o, 0);
        }
    
        public static void Write(object o, int depth) {
            var dumper = new ObjectDumperUtils(depth);
            dumper.WriteObject(null, o);
            Console.Write(dumper.ToString());
        }
    
        public override string ToString()
        {
            return builder.ToString();
        }

        private ObjectDumperUtils(int depth)
        {
            this.depth = depth;
        }
    
        private void Write(string s) {
            if (s != null) {
                builder.Append(s);
                pos += s.Length;
            }
        }
    
        private void WriteIndent() {
            builder.Append(' ', level * 3);
        }
    
        private void WriteLine() {
            builder.AppendLine();
            pos = 0;
        }
    
        private void WriteTab() {
            Write("\t");
        }
    
        private void WriteObject(string prefix, object o) {
        
            if (o == null || o is ValueType || o is string) {
                WriteIndent();
                Write(prefix);
                WriteValue(o);
                WriteLine();
            }
            else if (o is IEnumerable) {
                foreach (object element in (IEnumerable)o) {
                    if (element is IEnumerable && !(element is string)) {
                        WriteIndent();
                        Write(prefix);
                        Write("…");
                        WriteLine();
                        if (level < depth) {
                            level++;
                            WriteObject(prefix, element);
                            level--;
                        }
                    }
                    else {
                        WriteObject(prefix, element);
                    }
                }
            }
            else {
                MemberInfo[] members = o.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                WriteIndent();
                Write(prefix);
                bool propWritten = false;
                foreach (MemberInfo m in members) {
                    var f = m as FieldInfo;
                    var p = m as PropertyInfo;
                    if (f != null || p != null) {
                        if (propWritten) {
                            WriteTab();
                        }
                        else {
                            propWritten = true;
                        }
                        Write(m.Name);
                        Write("=");
                        Type t = f != null ? f.FieldType : p.PropertyType;
                        if (t.IsValueType || t == typeof(string)) {
                            WriteValue(f != null ? f.GetValue(o) : p.GetValue(o, null));
                        }
                        else {
                            if (typeof(IEnumerable).IsAssignableFrom(t)) {
                                Write("…");
                            }
                            else {
                                Write("{ }");
                            }
                        }
                    }
                }
                if (propWritten) WriteLine();
                if (level < depth) {
                    foreach (MemberInfo m in members) {
                        var f = m as FieldInfo;
                        var p = m as PropertyInfo;
                        if (f != null || p != null) {
                            Type t = f != null ? f.FieldType : p.PropertyType;
                            if (!(t.IsValueType || t == typeof(string))) {
                                object value = f != null ? f.GetValue(o) : p.GetValue(o, null);
                                if (value != null) {
                                    level++;
                                    WriteObject(m.Name + ": ", value);
                                    level--;
                                }
                            }
                        }
                    }
                }
            }
        }
    
        private void WriteValue(object o) {
            if (o == null) {
                Write("null");
            }
            else if (o is DateTime) {
                Write(((DateTime)o).ToShortDateString());
            }
            else if (o is ValueType || o is string) {
                Write(o.ToString());
            }
            else if (o is IEnumerable) {
                Write("…");
            }
            else {
                Write("{ }");
            }
        }
    }
}