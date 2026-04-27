using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Template.Infrastructure.Realtime;

[Authorize]
public sealed class DashboardHub : Hub;
