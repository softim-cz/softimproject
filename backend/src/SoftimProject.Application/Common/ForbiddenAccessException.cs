namespace SoftimProject.Application.Common;

public sealed class ForbiddenAccessException()
    : Exception("You do not have permission to perform this action.");
