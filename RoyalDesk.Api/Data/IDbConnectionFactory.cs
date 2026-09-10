using System.Data.Common;

namespace RoyalDesk.Api.Data;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
