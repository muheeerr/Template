using Template.Modules.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Template.Modules.Modules.Auth;

public sealed class AuthModule : ITemplateModule
{
    public IServiceCollection AddServices(IServiceCollection services, IConfiguration configuration) => services;
}
