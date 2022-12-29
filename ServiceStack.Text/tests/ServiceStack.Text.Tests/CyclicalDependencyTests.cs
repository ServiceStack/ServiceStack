using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    public class Module
    {
        public Module()
        {
            ExtendedData = new Dictionary<string, object>();
        }

        public string Name { get; set; }
        public string Version { get; set; }
        public IDictionary<string, object> ExtendedData { get; set; }
    }

    public class StackFrame
    {
        public StackFrame()
        {
            ExtendedData = new Dictionary<string, object>();
            Parameters = new Collection<Parameter>();
        }

        public string FileName { get; set; }
        public int LineNumber { get; set; }
        public int Column { get; set; }
        public IDictionary<string, object> ExtendedData { get; set; }
        public string Type { get; set; }
        public string Namespace { get; set; }
        public Module Module { get; set; }
        public string Method { get; set; }
        public ICollection<Parameter> Parameters { get; set; }
    }

    public class Parameter
    {
        public Parameter()
        {
            ExtendedData = new Dictionary<string, object>();
        }

        public string Name { get; set; }
        public string Type { get; set; }
        public IDictionary<string, object> ExtendedData { get; set; }
    }

    public class Error
    {
        public Error()
        {
            ExtendedData = new Dictionary<string, object>();
            Tags = new HashSet<string>();
            StackTrace = new Collection<StackFrame>();
        }

        public string Id { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public Module Module { get; set; }
        public string Description { get; set; }
        public DateTime OccurrenceDate { get; set; }
        public string Code { get; set; }
        public IDictionary<string, object> ExtendedData { get; set; }
        public HashSet<string> Tags { get; set; }

        public Error Inner { get; set; }

        public ICollection<StackFrame> StackTrace { get; set; }
        public string Contact { get; set; }
        public string Notes { get; set; }
    }


    [TestFixture]
    public class CyclicalDependencyTests : TestBase
    {
        [Test]
        public void Can_serialize_Error()
        {
            var dto = new Error
            {
                Id = "Id",
                Message = "Message",
                Type = "Type",
                Description = "Description",
                OccurrenceDate = new DateTime(2012, 01, 01),
                Code = "Code",
                ExtendedData = new Dictionary<string, object> { { "Key", "Value" } },
                Tags = new HashSet<string> { "C#", "ruby" },
                Inner = new Error
                {
                    Id = "Id2",
                    Message = "Message2",
                    ExtendedData = new Dictionary<string, object> { { "InnerKey", "InnerValue" } },
                    Module = new Module
                    {
                        Name = "Name",
                        Version = "v1.0"
                    },
                    StackTrace = new Collection<StackFrame> {
                        new StackFrame {
                            Column = 1,
                            Module = new Module {
                                Name = "StackTrace.Name",
                                Version = "StackTrace.v1.0"
                            },
                            ExtendedData = new Dictionary<string, object> { { "StackTraceKey", "StackTraceValue" } },
                            FileName = "FileName",
                            Type = "Type",
                            LineNumber = 1,
                            Method = "Method",
                            Namespace = "Namespace",
                            Parameters = new Collection<Parameter> {
                                new Parameter { Name = "Parameter", Type = "ParameterType" },
                            }
                        }
                    }
                },
                Contact = "Contact",
                Notes = "Notes",
            };

            var from = Serialize(dto, includeXml: false);
            Console.WriteLine(from.Dump());

            Assert.That(from.Id, Is.EqualTo(dto.Id));
            Assert.That(from.Message, Is.EqualTo(dto.Message));
            Assert.That(from.Type, Is.EqualTo(dto.Type));
            Assert.That(from.Description, Is.EqualTo(dto.Description));
            Assert.That(from.OccurrenceDate, Is.EqualTo(dto.OccurrenceDate));
            Assert.That(from.Code, Is.EqualTo(dto.Code));

            Assert.That(from.Inner.Id, Is.EqualTo(dto.Inner.Id));
            Assert.That(from.Inner.Message, Is.EqualTo(dto.Inner.Message));
            Assert.That(from.Inner.ExtendedData["InnerKey"], Is.EqualTo(dto.Inner.ExtendedData["InnerKey"]));
            Assert.That(from.Inner.Module.Name, Is.EqualTo(dto.Inner.Module.Name));
            Assert.That(from.Inner.Module.Version, Is.EqualTo(dto.Inner.Module.Version));

            var actualStack = from.Inner.StackTrace.First();
            var expectedStack = dto.Inner.StackTrace.First();
            Assert.That(actualStack.Column, Is.EqualTo(expectedStack.Column));
            Assert.That(actualStack.FileName, Is.EqualTo(expectedStack.FileName));
            Assert.That(actualStack.Type, Is.EqualTo(expectedStack.Type));
            Assert.That(actualStack.LineNumber, Is.EqualTo(expectedStack.LineNumber));
            Assert.That(actualStack.Method, Is.EqualTo(expectedStack.Method));

            Assert.That(from.Contact, Is.EqualTo(dto.Contact));
            Assert.That(from.Notes, Is.EqualTo(dto.Notes));
        }

        class person
        {
            public string name { get; set; }
            public person teacher { get; set; }
        }

        [Test]
        public void Can_limit_cyclical_dependencies()
        {
            using (JsConfig.With(new Config { MaxDepth = 4 }))
            {
                var p = new person();
                p.teacher = new person { name = "sam", teacher = p };
                p.name = "bob";
                p.PrintDump();
                p.ToJsv().Print();
                p.ToJson().Print();
            }
        }

        class Node
        {
            public string Name { get; set; }

            [IgnoreDataMember]
            public Node Parent { get; set; }

            public List<Node> Children { get; set; }
        }

        [Test]
        public void Ignore_Cyclical_dependencies()
        {
            JsConfig<Node>.OnDeserializedFn = (node) =>
            {
                node.Children.Each(child => child.Parent = node);
                return node;
            };

            var parent = new Node
            {
                Name = "Parent",
            };
            parent.Children = new List<Node>
            {
                new Node { Name = "Child", Parent = parent },
            };

            var json = parent.ToJson();
            Assert.That(json,
                Is.EqualTo("{\"Name\":\"Parent\",\"Children\":[{\"Name\":\"Child\"}]}"));

            var fromJson = json.FromJson<Node>();

            Assert.That(fromJson.Children[0].Parent, Is.EqualTo(fromJson));

            JsConfig<Node>.OnDeserializedFn = null;
            JsConfig.Reset();
        }

        public class ReflectionType
        {
            public string Name { get; set; } = "A";
            public Type Type { get; set; }
            public MethodInfo MethodInfo { get; set; }
            public PropertyInfo PropertyInfo { get; set; }
            public FieldInfo FieldInfo;
            public MemberInfo MemberInfo { get; set; }
            
            public void Method() {}
        }

        [Test]
        public void Can_serialize_POCO_with_Type()
        {
            var dto = new ReflectionType {
                Type = typeof(ReflectionType),
                MethodInfo = typeof(ReflectionType).GetMethod(nameof(ReflectionType.Method)),
                PropertyInfo = typeof(ReflectionType).GetProperty(nameof(ReflectionType.PropertyInfo)),
                FieldInfo = typeof(ReflectionType).GetPublicFields().FirstOrDefault(),
                MemberInfo = typeof(ReflectionType).GetMembers().FirstOrDefault(),
            };
           
            dto.Name.Print();
            dto.ToJson().Print();
            dto.ToJsv().Print();
            dto.PrintDump();
        }

    }
}
