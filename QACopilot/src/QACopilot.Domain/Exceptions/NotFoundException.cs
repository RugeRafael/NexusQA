namespace QACopilot.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object id)
        : base($"{entity} with id '{id}' was not found.") { }
}