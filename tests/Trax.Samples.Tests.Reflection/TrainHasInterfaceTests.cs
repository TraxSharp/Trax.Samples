using System.Reflection;
using Trax.Samples.Bookworm;

namespace Trax.Samples.Tests.Reflection;

/// <summary>
/// Every concrete train must expose a matching <c>I{Name}</c> marker interface deriving
/// <c>IServiceTrain&lt;TIn, TOut&gt;</c>. Code (and the scheduler manifest) depend on the interface,
/// and the interface FullName is the canonical train identity throughout Trax.
/// </summary>
[TestFixture]
public class TrainHasInterfaceTests
{
    private static IEnumerable<Type> TrainTypes() =>
        typeof(AssemblyMarker)
            .Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true } && IsServiceTrain(t));

    private static bool IsServiceTrain(Type type)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
        {
            if (t.IsGenericType && t.Name == "ServiceTrain`2")
                return true;
        }
        return false;
    }

    [Test]
    public void TrainAssembly_ContainsTrains()
    {
        TrainTypes().Should().NotBeEmpty("the guard must find trains to inspect");
    }

    [Test]
    public void EveryTrain_ImplementsAMatchingInterface_DerivingIServiceTrain()
    {
        var offenders = new List<string>();

        foreach (var train in TrainTypes())
        {
            var expected = "I" + train.Name;
            var marker = train
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.Name == expected
                    && i.GetInterfaces().Any(b => b.IsGenericType && b.Name == "IServiceTrain`2")
                );

            if (marker is null)
                offenders.Add(
                    $"{train.FullName} (expected interface {expected} : IServiceTrain<,>)"
                );
        }

        offenders
            .Should()
            .BeEmpty(
                "Every train needs a companion I{Name} interface deriving IServiceTrain<TIn, TOut>. "
                    + "Offenders:\n  "
                    + string.Join("\n  ", offenders)
            );
    }
}
