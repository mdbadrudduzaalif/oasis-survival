using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Enable CORS for any connecting device / game client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors();

string[] candidateConnections = new string[]
{
    "Server=localhost\\SQLEXPRESS;Database=OasisShooterDB;Integrated Security=True;TrustServerCertificate=True;",
    "Server=localhost;Database=OasisShooterDB;Integrated Security=True;TrustServerCertificate=True;",
    "Server=(localdb)\\MSSQLLocalDB;Database=OasisShooterDB;Integrated Security=True;TrustServerCertificate=True;",
    "Server=127.0.0.1;Database=OasisShooterDB;Integrated Security=True;TrustServerCertificate=True;"
};

string activeConnectionString = candidateConnections[0];

async Task<SqlConnection> GetOpenConnectionAsync()
{
    try
    {
        var conn = new SqlConnection(activeConnectionString);
        await conn.OpenAsync();
        return conn;
    }
    catch
    {
        foreach (var cs in candidateConnections)
        {
            if (cs == activeConnectionString) continue;
            try
            {
                var conn = new SqlConnection(cs);
                await conn.OpenAsync();
                activeConnectionString = cs;
                return conn;
            }
            catch { }
        }
        throw;
    }
}

// ==========================================
// 1. HEALTH CHECK ENDPOINT
// ==========================================
app.MapGet("/api/health", async () =>
{
    try
    {
        using var conn = await GetOpenConnectionAsync();
        return Results.Ok(new { status = "Online", database = "Connected", timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "Degraded", database = "Disconnected", error = ex.Message }, statusCode: 500);
    }
});

// ==========================================
// 2. AUTHENTICATION: REGISTER
// ==========================================
app.MapPost("/api/auth/register", async (AuthRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { success = false, message = "Username and password are required." });
    }

    try
    {
        using var conn = await GetOpenConnectionAsync();

        // Check if username exists
        using var checkCmd = new SqlCommand("SELECT COUNT(1) FROM dbo.Players WHERE Username = @Username", conn);
        checkCmd.Parameters.AddWithValue("@Username", request.Username.Trim());
        int exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

        if (exists > 0)
        {
            return Results.Conflict(new { success = false, message = "Username is already taken." });
        }

        // Insert new player
        using var insertCmd = new SqlCommand(
            "INSERT INTO dbo.Players (Username, PasswordHash) OUTPUT INSERTED.PlayerID VALUES (@Username, @Password);", conn);
        insertCmd.Parameters.AddWithValue("@Username", request.Username.Trim());
        insertCmd.Parameters.AddWithValue("@Password", request.Password);

        int playerId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

        return Results.Ok(new { success = true, playerId, username = request.Username.Trim(), message = "Account created successfully." });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = ex.Message }, statusCode: 500);
    }
});

// ==========================================
// 3. AUTHENTICATION: LOGIN
// ==========================================
app.MapPost("/api/auth/login", async (AuthRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { success = false, message = "Username and password are required." });
    }

    try
    {
        using var conn = await GetOpenConnectionAsync();

        using var cmd = new SqlCommand(
            "SELECT PlayerID, Username, PasswordHash FROM dbo.Players WHERE Username = @Username", conn);
        cmd.Parameters.AddWithValue("@Username", request.Username.Trim());

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            int playerId = reader.GetInt32(0);
            string username = reader.GetString(1);
            string storedPassword = reader.GetString(2);

            if (storedPassword != request.Password)
            {
                return Results.Unauthorized();
            }

            reader.Close();

            // Update LastLogin
            using var updateCmd = new SqlCommand("UPDATE dbo.Players SET LastLogin = GETUTCDATE() WHERE PlayerID = @PlayerID", conn);
            updateCmd.Parameters.AddWithValue("@PlayerID", playerId);
            await updateCmd.ExecuteNonQueryAsync();

            // Fetch player stats
            using var statsCmd = new SqlCommand(
                "SELECT ISNULL(MAX(Score), 0) AS BestScore, ISNULL(MAX(HighestWave), 1) AS MaxWave FROM dbo.MatchSessions WHERE PlayerID = @PlayerID", conn);
            statsCmd.Parameters.AddWithValue("@PlayerID", playerId);

            using var statsReader = await statsCmd.ExecuteReaderAsync();
            int bestScore = 0;
            int maxWave = 1;
            if (await statsReader.ReadAsync())
            {
                bestScore = statsReader.GetInt32(0);
                maxWave = statsReader.GetInt32(1);
            }

            return Results.Ok(new { success = true, playerId, username, bestScore, maxWave });
        }

        return Results.NotFound(new { success = false, message = "Player not found." });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = ex.Message }, statusCode: 500);
    }
});

// ==========================================
// 4. GAMEPLAY: SUBMIT MATCH RESULT
// ==========================================
app.MapPost("/api/game/match-result", async (MatchResultRequest request) =>
{
    try
    {
        using var conn = await GetOpenConnectionAsync();

        using var cmd = new SqlCommand(@"
            INSERT INTO dbo.MatchSessions 
            (PlayerID, Score, HighestWave, TotalKills, Headshots, DurationSeconds, IsVictory, PlayedAt)
            VALUES (@PlayerID, @Score, @HighestWave, @TotalKills, @Headshots, @DurationSeconds, @IsVictory, GETUTCDATE());", conn);

        cmd.Parameters.AddWithValue("@PlayerID", request.PlayerId);
        cmd.Parameters.AddWithValue("@Score", request.Score);
        cmd.Parameters.AddWithValue("@HighestWave", request.HighestWave);
        cmd.Parameters.AddWithValue("@TotalKills", request.TotalKills);
        cmd.Parameters.AddWithValue("@Headshots", request.Headshots);
        cmd.Parameters.AddWithValue("@DurationSeconds", request.DurationSeconds);
        cmd.Parameters.AddWithValue("@IsVictory", request.IsVictory);

        await cmd.ExecuteNonQueryAsync();

        // Check if this score is the new personal high score
        using var checkCmd = new SqlCommand("SELECT ISNULL(MAX(Score), 0) FROM dbo.MatchSessions WHERE PlayerID = @PlayerID", conn);
        checkCmd.Parameters.AddWithValue("@PlayerID", request.PlayerId);
        int maxScore = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

        bool isNewHighScore = (request.Score >= maxScore);

        return Results.Ok(new { success = true, isNewHighScore, highestScore = maxScore });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = ex.Message }, statusCode: 500);
    }
});

// ==========================================
// 5. LEADERBOARD: GET GLOBAL TOP 10
// ==========================================
app.MapGet("/api/game/leaderboard", async () =>
{
    try
    {
        using var conn = await GetOpenConnectionAsync();

        using var cmd = new SqlCommand("SELECT * FROM dbo.v_Leaderboard ORDER BY BestScore DESC", conn);
        using var reader = await cmd.ExecuteReaderAsync();

        var list = new List<LeaderboardEntry>();
        int rank = 1;

        while (await reader.ReadAsync())
        {
            list.Add(new LeaderboardEntry(
                Rank: rank++,
                PlayerId: reader.GetInt32(reader.GetOrdinal("PlayerID")),
                Username: reader.GetString(reader.GetOrdinal("Username")),
                BestScore: reader.GetInt32(reader.GetOrdinal("BestScore")),
                MaxWave: reader.GetInt32(reader.GetOrdinal("MaxWaveReached")),
                LifetimeKills: reader.GetInt32(reader.GetOrdinal("LifetimeKills")),
                LifetimeHeadshots: reader.GetInt32(reader.GetOrdinal("LifetimeHeadshots")),
                MatchesPlayed: reader.GetInt32(reader.GetOrdinal("MatchesPlayed"))
            ));
        }

        return Results.Ok(list);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 500);
    }
});

// Listen on all network adapters on port 5000 so external devices on Wi-Fi can connect
app.Urls.Add("http://0.0.0.0:5000");

Console.WriteLine("=================================================");
Console.WriteLine(" OASIS SHOOTER 3-TIER WEB API SERVER RUNNING");
Console.WriteLine(" Listening on: http://localhost:5000 & Local IP:5000");
Console.WriteLine("=================================================");

app.Run();

// ==========================================
// DATA MODELS
// ==========================================
public record AuthRequest(string Username, string Password);
public record MatchResultRequest(int PlayerId, int Score, int HighestWave, int TotalKills, int Headshots, int DurationSeconds, bool IsVictory);
public record LeaderboardEntry(int Rank, int PlayerId, string Username, int BestScore, int MaxWave, int LifetimeKills, int LifetimeHeadshots, int MatchesPlayed);
