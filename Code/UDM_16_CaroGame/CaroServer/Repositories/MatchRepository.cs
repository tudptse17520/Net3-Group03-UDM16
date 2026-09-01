using CaroServer.Data;
using CaroServer.Models;

namespace CaroServer.Repositories
{
    public class MatchRepository
    {
        public long AddMatch(
            long playerXId,
            long playerOId,
            long? winnerId,
            string result,
            DateTime startedAt,
            DateTime endedAt)
        {
            using var connection = DbConnectionFactory.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = """
                INSERT INTO Matches
                (
                    PlayerXId,
                    PlayerOId,
                    WinnerId,
                    Result,
                    StartTime,
                    EndTime
                )
                VALUES
                (
                    @playerXId,
                    @playerOId,
                    @winnerId,
                    @result,
                    @startTime,
                    @endTime
                );

                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
                """;

            command.Parameters.AddWithValue("@playerXId", playerXId);
            command.Parameters.AddWithValue("@playerOId", playerOId);
            command.Parameters.AddWithValue(
                "@winnerId",
                (object?)winnerId ?? DBNull.Value);

            command.Parameters.AddWithValue("@result", result);
            command.Parameters.AddWithValue("@startTime", startedAt);
            command.Parameters.AddWithValue("@endTime", endedAt);

            return (long)(command.ExecuteScalar() ?? 0L);
        }

        public Match? GetById(long id)
        {
            using var connection = DbConnectionFactory.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT
                    MatchId,
                    PlayerXId,
                    PlayerOId,
                    WinnerId,
                    Result,
                    StartTime,
                    EndTime
                FROM Matches
                WHERE MatchId = @id;
                """;

            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Match
            {
                Id = reader.GetInt32(0),
                PlayerXId = reader.GetInt32(1),
                PlayerOId = reader.GetInt32(2),
                WinnerId = reader.IsDBNull(3)
                    ? null
                    : reader.GetInt32(3),
                Result = reader.GetString(4),
                StartedAt = reader.GetDateTime(5),
                EndedAt = reader.GetDateTime(6)
            };
        }
    }
}