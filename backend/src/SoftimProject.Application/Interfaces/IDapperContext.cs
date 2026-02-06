using System.Data;

namespace SoftimProject.Application.Interfaces;

public interface IDapperContext
{
    IDbConnection CreateConnection();
}
