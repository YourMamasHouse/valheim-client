using System.Diagnostics;
using System.Net;

namespace ValheimClient;

public partial class App : Application
{
    private const string WorldRemoteDB = "https://github.com/YourMamasHouse/nate-garrett-dylan-valheim-world/raw/main/Testing.db";
	private const string WorldRemoteFwl = "https://github.com/YourMamasHouse/nate-garrett-dylan-valheim-world/raw/main/Testing.fwl";

    private const string TempDirectoryIncoming = "C:\\valheim-client-temp\\incoming";
	private const string TempDirectoryArchive = "C:\\valheim-client-temp\\archive";
    private const string TempDirectoryOutgoing = "C:\\valheim-client-temp\\outgoing";

    private static string ValheimLocalWorldFolder = $"c:\\Users\\{Environment.UserName}\\AppData\\LocalLow\\IronGate\\Valheim\\worlds_local";

    public App()
	{
		InitializeComponent();

		MainPage = new MainPage();
	}

	public static async void UpdateLocalWorldFiles()
	{
		// Copy current files to backup folder
		if (!Directory.Exists(ValheimLocalWorldFolder))
			throw new Exception("Cannot find existing worlds_local folder");

        // Clean temp archive
        if (Directory.Exists(TempDirectoryArchive))
            Directory.Delete(TempDirectoryArchive, true);

        Directory.CreateDirectory(TempDirectoryArchive);

        // Clean temp incoming
        if (Directory.Exists(TempDirectoryIncoming))
            Directory.Delete(TempDirectoryIncoming, true);

        Directory.CreateDirectory(TempDirectoryIncoming);

        File.Copy($"{ValheimLocalWorldFolder}\\Testing.db", $"{TempDirectoryArchive}\\Testing.db");
        File.Copy($"{ValheimLocalWorldFolder}\\Testing.fwl", $"{TempDirectoryArchive}\\Testing.fwl");

        // Download new files in temporary folder
		using (HttpClient client = new HttpClient())
		{
			try
			{
                await client.DownloadFileTaskAsync(WorldRemoteDB, $"{TempDirectoryIncoming}\\Testing.db");
                await client.DownloadFileTaskAsync(WorldRemoteFwl, $"{TempDirectoryIncoming}\\Testing.fwl");
			}
			catch (Exception ex)
			{
                throw ex; // TODO: Handle?
			}
		}

        // Delete existing files
        File.Delete($"{ValheimLocalWorldFolder}\\Testing.db");
        File.Delete($"{ValheimLocalWorldFolder}\\Testing.fwl");

        // Move temporary files to local worlds server
        File.Copy($"{TempDirectoryIncoming}\\Testing.db", $"{ValheimLocalWorldFolder}\\Testing.db");
        File.Copy($"{TempDirectoryIncoming}\\Testing.fwl", $"{ValheimLocalWorldFolder}\\Testing.fwl");

        // Delete temporary directory files
        File.Delete($"{TempDirectoryArchive}\\Testing.db");
        File.Delete($"{TempDirectoryArchive}\\Testing.fwl");

        File.Delete($"{TempDirectoryIncoming}\\Testing.db");
        File.Delete($"{TempDirectoryIncoming}\\Testing.fwl");
    }

    public static void UpdateRemote()
    {
        // Start process
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                WorkingDirectory = TempDirectoryOutgoing,
                FileName = "cmd",
                UseShellExecute = false,
                RedirectStandardInput = true
            }
        };
        process.Start();

        var now = DateTime.Now;
        var branchName = $"{Environment.UserName}-upload-{now.Day}-{now.Month}-{now.Year}-{now.Hour}-{now.Minute}-{now.Second}";

        // Clean temp outgoing
        if (Directory.Exists(TempDirectoryOutgoing))
            process.StandardInput.WriteLine($"rmdir /s /q {TempDirectoryOutgoing}");

        Directory.CreateDirectory(TempDirectoryOutgoing);

        // Clone repository and checkout new branch
        process.StandardInput.WriteLine("git clone https://github.com/YourMamasHouse/nate-garrett-dylan-valheim-world.git");
        process.StandardInput.WriteLine("cd nate-garrett-dylan-valheim-world");
        process.StandardInput.WriteLine($"git checkout -b {branchName}");

        // Copy changes
        process.StandardInput.WriteLine("del Testing.db");
        process.StandardInput.WriteLine("del Testing.fwl");

        process.StandardInput.WriteLine($"copy /Y {ValheimLocalWorldFolder}\\Testing.db");
        process.StandardInput.WriteLine($"copy /Y {ValheimLocalWorldFolder}\\Testing.fwl");

        // Merge changes to main
        process.StandardInput.WriteLine("git add -A");
        process.StandardInput.WriteLine("git commit -m \"new save\"");
        process.StandardInput.WriteLine("git checkout main");
        process.StandardInput.WriteLine("git pull origin main");
        process.StandardInput.WriteLine($"git merge {branchName}");

        // Push changes to remote
        process.StandardInput.WriteLine("git push origin main");
    }
}

public static class HttpClientUtils
{
    public static async Task DownloadFileTaskAsync(this HttpClient client, string uri, string FileName)
    {
        using (var s = await client.GetStreamAsync(uri))
        {
            using (var fs = new FileStream(FileName, FileMode.CreateNew))
            {
                await s.CopyToAsync(fs);
            }
        }
    }
}
