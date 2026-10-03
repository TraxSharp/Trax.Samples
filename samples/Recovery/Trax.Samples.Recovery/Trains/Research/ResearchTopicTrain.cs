using LanguageExt;
using Trax.Effect.Attributes;
using Trax.Effect.Services.ServiceTrain;
using Trax.Samples.Recovery.Auth;
using Trax.Samples.Recovery.Trains.Research.Junctions;

namespace Trax.Samples.Recovery.Trains.Research;

/// <summary>
/// A research agent: plan, ask the model where to look, call a tool, ask the model how deep to go,
/// call a second tool, summarize. The second tool call is the one the page can crash. The retry runs
/// the chain again from the top, but both model calls are replayed from the first attempt.
/// </summary>
[TraxBroadcast]
[TraxAuthorize(Roles = RecoveryRoles.Operator + "," + RecoveryRoles.Viewer)]
public class ResearchTopicTrain : ServiceTrain<ResearchInput, ResearchReport>, IResearchTopicTrain
{
    protected override Task<Either<Exception, ResearchReport>> Junctions() =>
        Chain<PlanResearch>()
            .Switch<ResearchBrief, Source>(tracks =>
                tracks
                    .When(Source.Web, t => t.Chain<SearchWeb>())
                    .When(Source.Papers, t => t.Chain<SearchPapers>())
                    .When(Source.Wiki, t => t.Chain<SearchWiki>())
            )
            .Scale<Findings, Depth>(scale =>
                scale
                    .AtLeast(Depth.Skim, t => t.Chain<SkimSources>())
                    .AtLeast(Depth.CrossCheck, t => t.Chain<FetchFullTexts>())
            )
            .Chain<Summarize>()
            .Resolve();
}
