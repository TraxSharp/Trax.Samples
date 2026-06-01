namespace Trax.Samples.Shared.Api.CrossSchema;

/// <summary>
/// One declared cross-schema GraphQL edge: a field on <see cref="Source"/> that resolves an
/// entity owned by a different context.
/// </summary>
/// <param name="Source">The GraphQL-exposed entity the edge field hangs off.</param>
/// <param name="Fk">The integer foreign-key property on <see cref="Source"/> (e.g. <c>BookId</c>).</param>
/// <param name="Target">The target entity type the edge resolves to.</param>
/// <param name="TargetContext">The context that owns <see cref="Target"/>.</param>
/// <param name="FieldName">The camelCase GraphQL field name the edge surfaces (e.g. <c>book</c>).</param>
/// <remarks>
/// A sample declares all of its edges in a single static manifest (<c>CrossSchemaEdges.All</c>).
/// That manifest is the single source of truth: meta-tests reflect over it to verify the FK exists,
/// the target is owned by the declared context, the field name is camelCase, and the field appears
/// in the checked-in GraphQL schema. Adding an edge means adding a manifest entry, a resolver, and a
/// loader registration, all of which the tests cross-check.
/// </remarks>
public sealed record CrossSchemaEdge(
    Type Source,
    string Fk,
    Type Target,
    Type TargetContext,
    string FieldName
);
