#nullable enable
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Text.Tests;

public class TestModel
{
    public TestModel()
    {
        var i = 0;
        this.PublicInt = i++;
        this.PublicGetInt = i++;
        this.PublicSetInt = i++;
        this.PublicIntField = i++;
        this.PrivateInt = i++;
        this.ProtectedInt = i++;
    }

    public int PublicInt { get; set; }

    public int PublicGetInt { get; private set; }

    public int PublicSetInt { private get; set; }

    public int PublicIntField;

    private int PrivateInt { get; set; }

    protected int ProtectedInt { get; set; }

    public int IntMethod()
    {
        return this.PublicInt;
    }
}

public class MethodsForReflection
{
    public string Result = String.Empty;

    public void HelloVoid()
    {
        Result = "Hello";
    }

    public void Hello(bool a, int b)
    {
        Result = String.Format($"Hello {a} {b}");
    }
}

public class NullableTest
{
    public int ValueType { get; set; }
    public NullableTest ReferenceType { get; set; } = new();
    public int? NullableValueType { get; set; }
    public NullableTest? NullableReferenceType { get; set; } = new();
}

[TestFixture]
public class ReflectionExtensionTests
    : TestBase
{
    [Test]
    public void Only_serializes_public_readable_properties()
    {
        var model = new TestModel();
        var modelStr = TypeSerializer.SerializeToString(model);

        Assert.That(modelStr, Is.EqualTo("{PublicInt:0,PublicGetInt:1}"));

        Serialize(model);
    }

    [Test]
    public void Can_create_instance_of_string()
    {
        Assert.That(typeof(string).CreateInstance(), Is.EqualTo(String.Empty));
    }

    [Test]
    public void Can_create_instances_of_common_collections()
    {
        Assert.That(typeof(IEnumerable<TestModel>).CreateInstance() as IEnumerable<TestModel>, Is.Not.Null);
        Assert.That(typeof(ICollection<TestModel>).CreateInstance() as ICollection<TestModel>, Is.Not.Null);
        Assert.That(typeof(IList<TestModel>).CreateInstance() as IList<TestModel>, Is.Not.Null);
        Assert.That(typeof(IDictionary<string, TestModel>).CreateInstance() as IDictionary<string, TestModel>, Is.Not.Null);
        Assert.That(typeof(IDictionary<int, TestModel>).CreateInstance() as IDictionary<int, TestModel>, Is.Not.Null);
        Assert.That(typeof(TestModel[]).CreateInstance() as TestModel[], Is.Not.Null);
    }

    [Test]
    public void Can_create_instances_of_generic_types()
    {
        Assert.That(typeof(GenericType<>).CreateInstance(), Is.Not.Null);
        Assert.That(typeof(GenericType<,>).CreateInstance(), Is.Not.Null);
        Assert.That(typeof(GenericType<,,>).CreateInstance(), Is.Not.Null);
        Assert.That(typeof(GenericType<GenericType<object>>).CreateInstance(), Is.Not.Null);
    }

    [Test]
    public void Can_create_instances_of_recursive_generic_type()
    {
        //Assert.That(typeof(GenericType<>).MakeGenericType(new[] { typeof(GenericType<>) }).CreateInstance(), Is.Not.Null);
    }

    [Test]
    public void Can_get_method_from_type()
    {
        var testInstance = new MethodsForReflection();

        var helloVoidMethod = typeof(MethodsForReflection).GetMethodInfo(nameof(MethodsForReflection.HelloVoid));
        Assert.That(helloVoidMethod, Is.Not.Null);
        var helloVoidDelegate = (Action<MethodsForReflection>)helloVoidMethod.MakeDelegate(typeof(Action<MethodsForReflection>));
        Assert.That(helloVoidDelegate, Is.Not.Null);
        helloVoidDelegate(testInstance);
        Assert.That(testInstance.Result, Is.EqualTo("Hello"));

        var helloVoidBoolIntMethod = typeof(MethodsForReflection).GetMethodInfo(nameof(MethodsForReflection.Hello), new Type[] { typeof(bool), typeof(int) });
        Assert.That(helloVoidBoolIntMethod, Is.Not.Null);
        var helloVoidBoolIntDelegate = (Action<MethodsForReflection, bool, int>)helloVoidBoolIntMethod.MakeDelegate(typeof(Action<MethodsForReflection, bool, int>));
        Assert.That(helloVoidBoolIntDelegate, Is.Not.Null);
        helloVoidBoolIntDelegate(testInstance, true, 5);
        Assert.That(testInstance.Result, Is.EqualTo("Hello True 5"));
    }

    [Test]
    public void Does_GetCollectionType()
    {
        Assert.That(new[] { new TestModel() }.GetType().GetCollectionType(), Is.EqualTo(typeof(TestModel)));
        Assert.That(new[] { new TestModel() }.ToList().GetType().GetCollectionType(), Is.EqualTo(typeof(TestModel)));
        Assert.That(new[] { new TestModel() }.Select(x => x).GetType().GetCollectionType(), Is.EqualTo(typeof(TestModel)));
        Assert.That(new[] { "" }.Select(x => new TestModel()).GetType().GetCollectionType(), Is.EqualTo(typeof(TestModel)));
    }

    [EnumAsChar]
    public enum CharEnum : int
    {
        Value1 = 'A', 
        Value2 = 'B', 
        Value3 = 'C', 
        Value4 = 'D'
    }

    [Test]
    public void Can_use_HasAttributeCached()
    {
        Assert.That(typeof(CharEnum).HasAttributeCached<EnumAsCharAttribute>());
        Assert.That(typeof(CharEnum).HasAttribute<EnumAsCharAttribute>());
    }

    [Test]
    public void Can_use_lazy_HasAttributeOf_APIs()
    {
        var props = typeof(DeclarativeValidationTest).GetPublicProperties();
        Assert.That(props.Length, Is.EqualTo(3));

        var locationsProp = props.FirstOrDefault(x =>
            x.PropertyType != typeof(string) && x.PropertyType.GetTypeWithGenericInterfaceOf(typeof(IEnumerable<>)) != null);
            
        Assert.That(locationsProp.Name, Is.EqualTo(nameof(DeclarativeValidationTest.Locations)));

        var genericDef = locationsProp.PropertyType.GetTypeWithGenericInterfaceOf(typeof(IEnumerable<>));
        var elementType = genericDef.GetGenericArguments()[0];
        var elementProps = elementType.GetPublicProperties();
        var hasAnyChildValidators = elementProps 
            .Any(elProp => elProp.HasAttributeOf<ValidateAttribute>());
        Assert.That(hasAnyChildValidators);
    }

    [Test]
    public void Can_clone_Enumerables()
    {
        var source = new[] { "A", "B", "C" };
        AssertClone(source.ShallowClone(), source);
            
        var list = new List<string>(source);
        AssertClone(list.ShallowClone(), list);
            
        var set = new HashSet<string>(source);
        AssertClone(set.ShallowClone(), set);

        var dict = new Dictionary<string, int>();
        source.Each((i,x) => dict[x] = i);
        AssertClone(dict.ShallowClone(), dict);

        var cDict = new ConcurrentDictionary<string, int>();
        source.Each((i,x) => cDict[x] = i);
        AssertClone(cDict.ShallowClone(), cDict);
    }
        
    public void AssertClone(IEnumerable actual, IEnumerable expected)
    {
        Assert.That(ReferenceEquals(actual, expected), Is.False);
        Assert.That(actual.GetType(), Is.EqualTo(expected.GetType()));
        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void IsNullableType_only_true_for_Nullable_generic_class()
    {
        var typeProps = TypeProperties<NullableTest>.Instance;
        Type GetPropType(string propName) => typeProps.PropertyMap[propName].PropertyInfo.PropertyType;

        Assert.That(typeof(int).IsNullableType(), Is.False);
        Assert.That(typeof(int?).IsNullableType(), Is.True);
        Assert.That(typeof(ReflectionExtensionTests).IsNullableType(), Is.False);
        Assert.That(GetPropType(nameof(NullableTest.NullableReferenceType)).IsNullableType(), Is.False);
        
        Assert.That(GetPropType(nameof(NullableTest.ValueType)).IsNullableType(), Is.False);
        Assert.That(GetPropType(nameof(NullableTest.NullableValueType)).IsNullableType(), Is.True);
        Assert.That(GetPropType(nameof(NullableTest.ReferenceType)).IsNullableType(), Is.False);
        Assert.That(GetPropType(nameof(NullableTest.NullableReferenceType)).IsNullableType(), Is.False);
    }

    [Test]
    public void Can_detect_null()
    {
        var typeProps = TypeProperties<NullableTest>.Instance;
        PropertyInfo GetPropInfo(string propName) => typeProps.PropertyMap[propName].PropertyInfo; 
        
        Assert.That(GetPropInfo(nameof(NullableTest.ValueType)).IsAssignableToNull(), Is.False);
        Assert.That(GetPropInfo(nameof(NullableTest.NullableValueType)).IsAssignableToNull(), Is.True);
        Assert.That(GetPropInfo(nameof(NullableTest.ReferenceType)).IsAssignableToNull(), Is.False);
        Assert.That(GetPropInfo(nameof(NullableTest.NullableReferenceType)).IsAssignableToNull(), Is.True);
    }
}

public class GenericType<T> { }
public class GenericType<T1, T2> { }
public class GenericType<T1, T2, T3> { }
    
public class Location
{
    public string Name { get; set; }
    [ValidateMaximumLength(20)]
    public string Value { get; set; }
} 

public class DeclarativeValidationTest : IReturn<EmptyResponse>
{
    [ValidateNotEmpty]
    [ValidateMaximumLength(20)]
    public string Site { get; set; }
    public List<Location> Locations { get; set; } // **** here's the example
    public List<NoValidators> NoValidators { get; set; }
}

public class NoValidators
{
    public string Name { get; set; }
    public string Value { get; set; }
}