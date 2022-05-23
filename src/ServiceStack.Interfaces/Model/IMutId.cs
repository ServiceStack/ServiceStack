namespace ServiceStack.Model;

public interface IMutId<T>
{
    T Id { get; set; }
}

public interface IMutLongId : IMutId<long> {}
public interface IMutIntId : IMutId<int> {}
public interface IMutStringId : IMutId<string> {}
