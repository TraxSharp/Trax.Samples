using LanguageExt;
using Trax.Effect.Services.ServiceTrain;

namespace Trax.Samples.Api.Rest.Trains.AdminGreet;

public interface IAdminGreetTrain : IServiceTrain<AdminGreetInput, Unit>;
