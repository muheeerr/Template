using Mediator;

namespace Template.Modules.Common.Results;

public interface IAppCommandBase;

public interface IAppQueryBase;

public interface IAppCommand : IRequest<AppResult>, IAppCommandBase;

public interface IAppCommand<TResponse> : IRequest<AppResult<TResponse>>, IAppCommandBase;

public interface IAppQuery<TResponse> : IRequest<AppResult<TResponse>>, IAppQueryBase;
