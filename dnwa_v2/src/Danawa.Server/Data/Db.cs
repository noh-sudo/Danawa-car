using MySqlConnector;

namespace Danawa.Server.Data;

/// <summary>DB 접속 문자열 보관 및 연결 생성, Program.cs에서 Configure로 1회 초기화 — 풀은 MySqlConnector가 접속 문자열 기준(Pooling=true;Maximum Pool Size=5)으로 관리, 5개 모두 사용 중이면 반납 대기(Connection Timeout 15초 초과 시 예외 → SERVER_ERROR)</summary>
public static class Db
{
    private static string? _connectionString;

    public static void Configure(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static MySqlConnection CreateConnection()
    {
        if (_connectionString is null)
        {
            throw new InvalidOperationException("Db.Configure를 먼저 호출해야 한다");
        }

        return new MySqlConnection(_connectionString);
    }
}
