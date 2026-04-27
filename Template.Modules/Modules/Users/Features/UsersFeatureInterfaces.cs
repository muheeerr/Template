using Template.Modules.Common.Abstractions;

namespace Template.Modules.Modules.Users.Features;

[FeatureRouteTemplate("api/v1/users")]
internal interface IUsers : IFeature;

[FeatureRouteTemplate("api/v1/lookups")]
internal interface IUserLookups : IFeature;
