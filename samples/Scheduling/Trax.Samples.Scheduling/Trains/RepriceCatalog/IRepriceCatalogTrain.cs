using LanguageExt;
using Trax.Effect.Services.ServiceTrain;

namespace Trax.Samples.Scheduling.Trains.RepriceCatalog;

public interface IRepriceCatalogTrain : IServiceTrain<RepriceCatalogInput, Unit>;
