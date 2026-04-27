using Template.Modules.Common.Abstractions;

namespace Template.Modules.Modules.Auth;

[FeatureRouteTemplate("api/v1/auth")]
internal interface IAuth : IFeature;

[FeatureRouteTemplate("api/v1/roles")]
internal interface IRoles : IFeature;

[FeatureRouteTemplate("api/v1/actions")]
internal interface IActions : IFeature;

