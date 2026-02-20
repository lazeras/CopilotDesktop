using CopilotDesktop.Models;
using CopilotDesktop.Contracts.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CopilotDesktop.Services
{
    public class ProviderService : IProviderService
    {
        private const string EntriesFileName = "entries.json";
        private const string SelectedProviderKey = "DefaultProviderUrl";

        private readonly ILocalSettingsService _localSettingsService;

        public ObservableCollection<ProviderItem> DefaultProviders { get; } = new();
        public ObservableCollection<ProviderItem> UserProviders { get; } = new();
        public ObservableCollection<ProviderItem> CombinedProviders { get; } = new();

        public string SelectedProviderUrl { get; private set; }

        public event System.Action<ProviderItem>? SelectedProviderChanged;

        public ProviderService(ILocalSettingsService localSettingsService)
        {
            _localSettingsService = localSettingsService;

            // populate defaults
            DefaultProviders.Add(new ProviderItem { Name = "Copilot", Url = "https://copilot.microsoft.com" });
            DefaultProviders.Add(new ProviderItem { Name = "ChatGPT", Url = "https://chat.openai.com" });
            DefaultProviders.Add(new ProviderItem { Name = "Gemini", Url = "https://gemini.google.com" });
            DefaultProviders.Add(new ProviderItem { Name = "DuckDuckGo", Url = "https://duck.ai/chat?duckai=1" });

            // combined will be built during InitializeAsync
        }

        public async Task InitializeAsync()
        {
            // load user entries from %LocalAppData%\CopilotDesktop\entries.json
            try
            {
                var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                var dir = Path.Combine(appData, "CopilotDesktop");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var path = Path.Combine(dir, EntriesFileName);
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var list = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<ProviderItem>>(json);
                        if (list != null)
                        {
                            // Clear existing user providers before adding from file to avoid duplicates
                            UserProviders.Clear();
                            foreach (var p in list)
                            {
                                // normalize URLs to avoid mismatches caused by trailing spaces/casing
                                if (p.Url != null) p.Url = p.Url.Trim();
                                UserProviders.Add(p);
                            }
                        }
                    }
                }

                BuildCombined();

                SelectedProviderUrl = await _localSettingsService.ReadSettingAsync<string>(SelectedProviderKey) ?? DefaultProviders.First().Url;
            }
            catch
            {
                BuildCombined();
                SelectedProviderUrl = DefaultProviders.First().Url;
            }
        }

        private void BuildCombined()
        {
            CombinedProviders.Clear();
            foreach (var p in DefaultProviders) CombinedProviders.Add(p);
            foreach (var p in UserProviders) CombinedProviders.Add(p);
        }

        public async Task SetSelectedProviderAsync(ProviderItem provider)
        {
            if (provider == null) return;
            SelectedProviderUrl = provider.Url?.Trim();
            await _localSettingsService.SaveSettingAsync(SelectedProviderKey, SelectedProviderUrl);
            SelectedProviderChanged?.Invoke(provider);
        }

        public async Task SelectProviderTransientAsync(ProviderItem provider)
        {
            if (provider == null) return;
            // Update current selection for this run but do not persist as the default for first-run
            SelectedProviderUrl = provider.Url?.Trim();
            SelectedProviderChanged?.Invoke(provider);
            await Task.CompletedTask;
        }

        public async Task AddUserProviderAsync(ProviderItem provider)
        {
            if (provider == null) return;
            UserProviders.Add(provider);
            BuildCombined();
            await SaveUserProvidersToFileAsync();
        }

        public async Task RemoveUserProviderAsync(ProviderItem provider)
        {
            if (provider == null) return;
            if (UserProviders.Contains(provider))
            {
                UserProviders.Remove(provider);
                BuildCombined();
                await SaveUserProvidersToFileAsync();
            }
        }

        private async Task SaveUserProvidersToFileAsync()
        {
            try
            {
                var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                var dir = Path.Combine(appData, "CopilotDesktop");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var path = Path.Combine(dir, EntriesFileName);
                var json = System.Text.Json.JsonSerializer.Serialize(UserProviders.ToList());
                await File.WriteAllTextAsync(path, json);
            }
            catch { }
        }
    }
}
