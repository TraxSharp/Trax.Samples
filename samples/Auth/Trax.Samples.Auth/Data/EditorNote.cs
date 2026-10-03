using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace Trax.Samples.Auth.Data;

/// <summary>
/// An internal note attached to an article. Not a query model of its own, but a query model
/// reaches it through <see cref="Article.EditorNote"/>, so it must declare a posture too.
/// </summary>
/// <remarks>
/// Two stacked role attributes union: an editor <b>or</b> an auditor may read it. Roles OR,
/// within one attribute's list and across attributes; only policies AND.
/// </remarks>
[TraxAuthorize(Roles = Auth.AuthRoles.Editor)]
[TraxAuthorize(Roles = Auth.AuthRoles.Auditor)]
[Table("editor_notes")]
public class EditorNote
{
    public long Id { get; set; }

    public string Text { get; set; } = "";
}
