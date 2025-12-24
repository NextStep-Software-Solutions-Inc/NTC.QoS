using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace NTC.QoS.Infrastructure.Exceptions;
public static class DatabaseExceptionHandler
{
    public static Exception HandleForeignKeyConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException postgresException && postgresException.SqlState == "23503")
        {
            string errorMessage = !string.IsNullOrEmpty(postgresException.Message)
                ? $"{char.ToUpper(postgresException.MessageText[0])}{postgresException.MessageText.Substring(1)}"
                : "One or more related entities do not exist.";

            // Preserve the original exception as the inner exception so callers/logs can inspect full details
            return new Exception(errorMessage, ex);
        }

        return ex; // Return the original exception for other cases
    }

}

