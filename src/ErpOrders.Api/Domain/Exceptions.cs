namespace ErpOrders.Api.Domain;

/// <summary>A business rule was violated (mapped to HTTP 422).</summary>
public class DomainException(string message) : Exception(message);

/// <summary>A referenced resource does not exist (mapped to HTTP 404).</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>The request conflicts with existing data, e.g. a duplicate key (mapped to HTTP 409).</summary>
public class ConflictException(string message) : Exception(message);
