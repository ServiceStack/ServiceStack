using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.Blazor.Components.Tailwind
{
    public partial class Tabs : IDisposable
    {
        string _selectedTab;
        string SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (value != Tab)
                {
                    TabChanged.InvokeAsync(value);
                    _selectedTab = value;
                }
            }
        }

        [Parameter]
        public Dictionary<string,string> TabOptions { get; set; }

        [Parameter]
        public EventCallback<string> TabChanged { get; set; }

        [Parameter]
        public string Tab { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public RenderFragment<KeyValueStrings> TabTemplate { get; set; }

        [Inject] public NavigationManager NavigationManager { get; set; }

        public string QueryStringName = "tab";



        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += HandleLocationChanged;
            await UpdateTab();
            await base.OnInitializedAsync();
        }

        async Task UpdateTab(string location = null)
        {
            var uri = new Uri(location ?? NavigationManager.Uri);
            var queryStrings = System.Web.HttpUtility.ParseQueryString(uri.Query);
            if (queryStrings.AllKeys.Any(x => x == QueryStringName))
            {
                var tab = queryStrings.Get(QueryStringName);
                SelectedTab = tab;
            }
            else
            {
                SelectedTab = TabOptions.Keys.First();
            }
            StateHasChanged();
        }

        async Task ChangeTab(string tab)
        {
            SelectedTab = tab;
            string uri = NavigationManager.Uri.SetQueryParam(QueryStringName, tab);
            if (TabOptions.Keys.First() == tab)
            {
                uri = NavigationManager.Uri.SetQueryParam(QueryStringName, null);
            }
            NavigationManager.NavigateTo(uri);
        }

        void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
        {
            UpdateTab(args.Location).ConfigureAwait(false);
        }

        public void Dispose() => NavigationManager.LocationChanged -= HandleLocationChanged;

        async Task TabSelection(ChangeEventArgs e)
        {
            var tab = e.Value?.ToString();
            SelectedTab = tab;
            string uri = NavigationManager.Uri.SetQueryParam(QueryStringName, tab);
            if (TabOptions.Keys.First() == tab || tab == null)
            {
                uri = NavigationManager.Uri.SetQueryParam(QueryStringName, null);
            }
            NavigationManager.NavigateTo(uri);
        }

    }
}
