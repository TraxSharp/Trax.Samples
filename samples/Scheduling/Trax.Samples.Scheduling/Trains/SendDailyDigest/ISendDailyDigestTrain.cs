using LanguageExt;
using Trax.Effect.Services.ServiceTrain;

namespace Trax.Samples.Scheduling.Trains.SendDailyDigest;

public interface ISendDailyDigestTrain : IServiceTrain<SendDailyDigestInput, Unit>;
