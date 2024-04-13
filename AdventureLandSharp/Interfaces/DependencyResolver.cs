using System.Diagnostics;
using System.Reflection;

namespace AdventureLandSharp.Interfaces;

public interface IDependencyAttribute {
    public int Priority { get; }
}


[AttributeUsage(AttributeTargets.Method)]
public class SessionCoordinatorFactoryAttribute(int priority = 0) : Attribute, IDependencyAttribute {
    public int Priority => priority;
}

public static partial class DependencyResolver {
    public static SessionCoordinatorFactory SessionCoordinator() => Create<SessionCoordinatorFactory, SessionCoordinatorFactoryAttribute>();

    private static TDelegate Create<TDelegate, TAttribute>() 
        where TDelegate : Delegate
        where TAttribute : Attribute, IDependencyAttribute 
    {
        MethodInfo method = Lookup<TAttribute>();

        foreach ((Type del, Type fac) in typeof(CharacterFactory).GenericTypeArguments.Zip(
            method.GetParameters(), 
            (expected, actual) => (expected, actual.ParameterType)))
        {
            Debug.Assert(del == fac, $"Expected {del}, got {fac}.");
        }

        return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), method);
    }
    
    private static MethodInfo Lookup<T>() where T : Attribute, IDependencyAttribute => AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(x => x.GetTypes())
        .SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        .Select(x => (Method: x, Attr: x.GetCustomAttribute<T>()))
        .Where(x => x.Attr != null)
        .OrderByDescending(x => x.Attr!.Priority)
        .Select(x => x.Method)
        .First();
}
