using CopilotDesktop.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CopilotDesktop.Services
{
    public interface IProviderService
    {
        ObservableCollection<ProviderItem> DefaultProviders { get; }
        ObservableCollection<ProviderItem> UserProviders { get; }
        ObservableCollection<ProviderItem> CombinedProviders { get; }

        string SelectedProviderUrl { get; }
        
        // Event raised when the selected provider changes
        event System.Action<ProviderItem>? SelectedProviderChanged;
        Task InitializeAsync();
        Task SetSelectedProviderAsync(ProviderItem provider);
        // Select a provider for the current session without persisting as the default
        Task SelectProviderTransientAsync(ProviderItem provider);
        Task AddUserProviderAsync(ProviderItem provider);
        Task RemoveUserProviderAsync(ProviderItem provider);
    }
}
