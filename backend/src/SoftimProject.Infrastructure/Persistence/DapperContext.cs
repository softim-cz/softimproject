using System.Data;
using Microsoft.Data.SqlClient;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.Persistence;

public sealed class DapperContext(string connectionString) : IDapperContext
{
    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
