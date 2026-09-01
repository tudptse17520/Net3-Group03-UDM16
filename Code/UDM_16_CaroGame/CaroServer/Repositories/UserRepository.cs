using CaroServer.Data;
using CaroServer.Models;

namespace CaroServer.Repositories
{
    public class UserRepository
    {
        public long AddUser(string nickname)
        {
            using var connection = DbConnectionFactory.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = """
                INSERT INTO Users (Username)
                VALUES (@nickname);

                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
                """;

            command.Parameters.AddWithValue("@nickname", nickname);

            return (long)(command.ExecuteScalar() ?? 0L);
        }

        public User? GetByNickname(string nickname)
        {
            using var connection = DbConnectionFactory.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT UserId, Username, CreatedAt
                FROM Users
                WHERE Username = @nickname;
                """;

            command.Parameters.AddWithValue("@nickname", nickname);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new User
            {
                Id = reader.GetInt32(0),
                Nickname = reader.GetString(1),
                CreatedAt = reader.GetDateTime(2)
            };
        }
    }
}