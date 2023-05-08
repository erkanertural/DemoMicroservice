namespace Core.Utilities;

public static class CommonUtil
{
    public static string Enviromnent => System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "development";
}
