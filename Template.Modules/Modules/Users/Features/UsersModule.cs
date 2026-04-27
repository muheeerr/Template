using Template.Modules.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Template.Modules.Modules.Users.Features;

public sealed class UsersModule : ITemplateModule
{
    public IServiceCollection AddServices(IServiceCollection services, IConfiguration configuration) => services;
}
