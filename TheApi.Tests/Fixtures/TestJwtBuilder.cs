namespace TheApi.Tests.Fixtures;

public static class TestJwtBuilder
{
    private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    public static async Task<(string output, string error)> GetTestJwt(string email, Dictionary<string, string> otherClaims)
    {
        string jwt = "";
        string error = "";

        string claims = string.Join(" ", (
            from o in otherClaims
            select $"--claim \"{o.Key}={o.Value}\""
            ).ToList());

        string cmd = "dotnet";
        string cwd = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../../TheApi"));
        string args = $"user-jwts create --output token {claims} --name \"{email}\"";
        await semaphoreSlim.WaitAsync();
        try
        {
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = cmd,
                    WorkingDirectory = cwd,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            proc.OutputDataReceived += (sender, args) => jwt += args.Data;
            proc.ErrorDataReceived += (sender, args) => error += args.Data;

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();
        }
        finally
        {
            semaphoreSlim.Release();
        }

        int pieces = string.IsNullOrWhiteSpace(jwt) ? 0 : jwt.Split(['.'], StringSplitOptions.RemoveEmptyEntries).Length;
        if (pieces != 3 || jwt.Contains(' '))
        {
            error += jwt;
            jwt = "";
        }

        return (jwt, error);
    }

}
