using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

using Crowdin.Api;
using Crowdin.Api.Translations;

using LibGit2Sharp;

namespace Bloxstrap.Builder
{
    internal class Program
    {
        const string exePath = "bloxstrap\\Bloxstrap\\bin\\Release\\net6.0-windows\\publish\\win-x64\\Bloxstrap.exe";

        static readonly Dictionary<string, string> SupportedLocales = new()
        {
            { "ar", "Arabic" },
            { "bg", "Bulgarian" },
            { "bn", "Bengali" },
            { "bs", "Bosnian" },
            { "cs", "Czech" },
            { "de", "German" },
            { "dk", "Danish" },
            { "es-ES", "Spanish" },
            { "fi", "Finnish" },
            { "fil", "Filipino" },
            { "fr", "French" },
            { "he", "Hebrew" },
            { "hi", "Hindi (Latin)" },
            { "hr", "Croatian" },
            { "hu", "Hungarian" },
            { "id", "Indonesian" },
            { "it", "Italian" },
            { "ja", "Japanese" },
            { "ko", "Korean" },
            { "lt", "Lithuanian" },
            { "no", "Norwegian" },
            { "nl", "Dutch" },
            { "pl", "Polish" },
            { "pt-BR", "Portuguese (Brazil)" },
            { "ro", "Romanian" },
            { "ru", "Russian" },
            { "sv-SE", "Swedish" },
            { "th", "Thai" },
            { "tr", "Turkish" },
            { "uk", "Ukrainian" },
            { "vi", "Vietnamese" },
            { "zh-CN", "Chinese (Simplified)" },
            { "zh-HK", "Chinese (Hong Kong)" },
            { "zh-TW", "Chinese (Traditional)" }
        };

        static bool Running = true;

        static readonly List<string> Cmds = new() { "help", "configure crowdin", "list languages", "set language <language>", "build", "run", "exit" };

        static Config Config = new();

        static bool OriginalConfigValidation = false;

        static HttpClient HttpClient = new();

        static System.Version Version = Assembly.GetExecutingAssembly().GetName().Version!;

        static void WriteLineColor(string line, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void Main(string[] args)
        {
            HttpClient.DefaultRequestHeaders.Add("User-Agent", $"Bloxstrap.Builder/{Version}");

            Console.ForegroundColor = ConsoleColor.White;
            
            Console.Title = "Bloxstrap Builder v1.0.2";
            Console.WriteLine(Console.Title);

            var info = new DirectoryInfo("C:\\Program Files\\dotnet\\sdk");

            if (!info.Exists || info.GetDirectories().Length == 0)
            {
                WriteLineColor("The .NET SDK must be installed. Download it from https://dotnet.microsoft.com/en-us/download.", ConsoleColor.Red);
                return;
            }

            try
            {
                Config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"))!;
            }
            catch (Exception)
            {

            }

            OriginalConfigValidation = ValidateConfig(true, true);

            CheckForUpdate().Wait();
            
            Console.WriteLine("Type \"help\" to see a list of commands.");

            while (Running)
            {
                Console.WriteLine("");
                
                string? prompt = Prompt();

                if (String.IsNullOrEmpty(prompt))
                    continue;
                else if (prompt == "exit")
                    Running = false;
                else if (prompt == "help")
                    ShowHelp();
                else if (prompt == "list languages")
                    ListLanguages();
                else if (prompt.StartsWith("set language"))
                    SetLanguage(prompt);
                else if (prompt == "configure crowdin")
                    ConfigureCrowdin();
                else if (prompt == "build")
                    Build();
                else if (prompt == "run")
                    Run();
                else
                    Console.WriteLine("Invalid command. Type \"help\" to see a list of commands.");
            }
        }

        static bool ValidateConfig(bool showErrors, bool showLanguage)
        {
            bool errors = false;

            if (String.IsNullOrEmpty(Config.CrowdinToken))
            {
                errors = true;

                if (showErrors)
                    WriteLineColor("A Crowdin API token is required. To (learn how to) configure one, type \"configure crowdin\".", ConsoleColor.Yellow);
            }

            if (String.IsNullOrEmpty(Config.ChosenLocale) || !SupportedLocales.TryGetValue(Config.ChosenLocale, out string? languageName))
            {
                errors = true;

                if (showErrors)
                    WriteLineColor("No language been set. List the available languages by typing \"list languages\".", ConsoleColor.Yellow);
            }
            else if (showLanguage)
                Console.WriteLine($"Language has been set to {languageName} ({Config.ChosenLocale}).");

            return !errors;
        }

        static string? Prompt()
        {
            Console.Write("> ");
            return Console.ReadLine()!.ToLowerInvariant();
        }

        static void ShowHelp()
        {
            Console.WriteLine("List of commands:");

            foreach (string command in Cmds)
                Console.WriteLine(command);
        }

        static void ListLanguages()
        {
            Console.WriteLine("Here are the available languages:");

            foreach (var entry in SupportedLocales)
                Console.WriteLine($"[{entry.Key}] {entry.Value}");

            Console.WriteLine("");

            Console.WriteLine("You may need to scroll up to see all languages.");
            Console.WriteLine("To set your language, type \"set language <language>\".");
            Console.WriteLine("For example, to set your language as Swedish, type \"set language swedish\" or \"set language sv-se\".");
        }

        static void SetLanguage(string prompt)
        {
            var parts = prompt.Split(' ');

            if (parts.Length != 3 || parts[2] is string language && String.IsNullOrEmpty(language))
            {
                Console.WriteLine("To set your language, type \"set language <language>\".");
                Console.WriteLine("For example, to set your language as Swedish, type \"set language swedish\" or \"set language sv-se\".");
                return;
            }

            language = parts[2];

            var query = SupportedLocales.Where(x => String.Compare(x.Key, language, StringComparison.OrdinalIgnoreCase) == 0 || x.Value.StartsWith(language, StringComparison.OrdinalIgnoreCase));

            if (!query.Any())
            {
                Console.WriteLine("Invalid language specified.");
                return;
            }

            var result = query.First();

            Config.ChosenLocale = result.Key;

            Console.WriteLine($"Language has been set to {result.Value} ({result.Key}).");

            if (!OriginalConfigValidation && ValidateConfig(false, false))
                Console.WriteLine("You can now create a build by typing \"build\".");
            else if (String.IsNullOrEmpty(Config.CrowdinToken))
                Console.WriteLine("You still need to configure a Crowdin API token, which you can do by typing \"configure crowdin\".");

            Save();
        }

        static void Save() => File.WriteAllText("config.json", JsonSerializer.Serialize(Config));

        static void ConfigureCrowdin()
        {
            Console.WriteLine("To build translations, an API token for your Crowdin account is needed.");
            Console.WriteLine("Create one by going to your Crowdin settings -> API -> Personal Access Tokens.");
            Console.WriteLine("You must grant a Read and Write scope for Projects.");

            string? token = "";
            while (String.IsNullOrEmpty(token))
            {
                Console.Write("Enter token: ");
                token = Console.ReadLine();
            }

            Config.CrowdinToken = token;

            Save();

            if (!OriginalConfigValidation && ValidateConfig(false, false))
                Console.WriteLine("You can now create a build by typing \"build\".");
            else if (String.IsNullOrEmpty(Config.ChosenLocale))
                Console.WriteLine("You still need to set a language. List the available languages by typing \"list languages\".");
        }

        static void Build()
        {
            if (!ValidateConfig(true, false))
                return;

            bool exists = Directory.Exists("bloxstrap");

            if (!exists)
            {
                Console.WriteLine("Cloning repository...");
                Repository.Clone("https://github.com/bloxstraplabs/bloxstrap.git", "bloxstrap", new CloneOptions() { RecurseSubmodules = true });
            }

            using var repo = new Repository("bloxstrap");

            if (exists)
            {
                Console.WriteLine("Pulling code...");
                Commands.Pull(repo, new Signature("", "", DateTimeOffset.Now), new PullOptions());
            }

            Console.WriteLine("Requesting translation build...");
            var client = new CrowdinApiClient(new CrowdinCredentials { AccessToken = Config.CrowdinToken! });

            var request = new BuildProjectFileTranslationRequest
            {
                TargetLanguageId = Config.ChosenLocale!,
                ExportApprovedOnly = false
            };

            var response = client.Translations.BuildProjectFileTranslation(613561, 8, request).Result;

            Console.WriteLine("Downloading translations...");

            {
                using var httpStream = HttpClient.GetStreamAsync(response.Link!.Url).Result;
                using var fileStream = File.Create($"bloxstrap\\Bloxstrap\\Resources\\Strings.{Config.ChosenLocale!}.resx");
                httpStream.CopyTo(fileStream);
            }

            Console.WriteLine("Building...");

            var buildProcess = Process.Start("dotnet", "publish bloxstrap/Bloxstrap /p:PublishProfile=Publish-x64 /p:DefineConstants=QA_BUILD");

            buildProcess.WaitForExit();

            Console.WriteLine("");

            // TODO: don't do this
            if (File.Exists(exePath))
                Console.WriteLine("Build succeeded. Type \"run\" to run it.");
            else
                WriteLineColor("Build failed.", ConsoleColor.Red);
        }

        static async Task CheckForUpdate()
        {
            var releaseInfo = await HttpClient.GetFromJsonAsync<GitHubRelease>("https://api.github.com/repos/bloxstraplabs/builder/releases/latest");

            if (releaseInfo is null)
                return;

            var latestVersion = new System.Version(releaseInfo.TagName[1..]);

            if (Version < latestVersion)
                WriteLineColor($"A new version of Bloxstrap Builder is available (v{Version.ToString()[..^2]} -> v{latestVersion}).\r\nDownload it from https://github.com/bloxstraplabs/builder/releases/latest.", ConsoleColor.Blue);
        }

        static void Run() => Process.Start(exePath);
    }
}
