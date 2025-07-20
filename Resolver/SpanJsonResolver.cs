namespace Linh.JsonKit.Json.Resolver;

/// <summary>
/// Custom resolver for SpanJson to specify serialization options.
/// </summary>
public sealed class SpanJsonResolver<TSymbol> : SpanJson.Resolvers.ResolverBase<TSymbol, SpanJsonResolver<TSymbol>>
where TSymbol : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpanJsonResolver{TSymbol}"/> class
    /// with default options including null values, integer enums, and Base64 for byte arrays.
    /// </summary>
    public SpanJsonResolver()
        : base(
            new SpanJson.Resolvers.SpanJsonOptions
            {
                NullOption = SpanJson.Resolvers.NullOptions.IncludeNulls,
                EnumOption = SpanJson.Resolvers.EnumOptions.Integer,
                ByteArrayOption = SpanJson.Resolvers.ByteArrayOptions.Base64,
            }
        )
    { }
}