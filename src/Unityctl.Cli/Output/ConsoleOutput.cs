using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Output;

public static class ConsoleOutput
{
    public static void PrintResponse(CommandResponse response)
    {
        if (response.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("OK");
            Console.ResetColor();
            if (!string.IsNullOrEmpty(response.Message))
                Console.Write($" — {response.Message}");
            Console.WriteLine();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"FAIL [{response.StatusCode}]");
            Console.ResetColor();
            if (!string.IsNullOrEmpty(response.Message))
                Console.Write($" — {response.Message}");
            Console.WriteLine();
        }

        if (response.Data != null)
        {
            foreach (var (key, value) in response.Data)
            {
                Console.WriteLine($"  {key}: {value}");
            }
        }

        if (response.Errors is { Count: > 0 })
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            foreach (var error in response.Errors)
            {
                Console.Error.WriteLine($"  ! {error}");
            }
            Console.ResetColor();
        }
    }

    public static void PrintRecovery(StatusCode code)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        var hint = code switch
        {
            StatusCode.NotFound => "Tip: Is Unity installed? Run 'unityctl editor list' to check.",
            StatusCode.ProjectLocked => "Tip: Close the running Unity Editor or wait for it to finish.",
            StatusCode.LicenseError => "Tip: Activate your Unity license via Unity Hub.",
            StatusCode.PluginNotInstalled => "Tip: Run 'unityctl init --project <path>' to install the plugin.",
            _ => null
        };
        if (hint != null) Console.Error.WriteLine(hint);
        Console.ResetColor();
    }
}
