using Microsoft.AspNetCore.Components;
using MudBlazor;
using MyServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorServer.Pages
{
    public partial class SortedSetDemo : IDisposable
    {

        [Inject]
        SortedSetManagementService SortedSetManagement { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        double value = 50.0;

        private List<string> _setMembers;

        private async Task AddItem()
        {
            string toAdd = Guid.NewGuid().ToString();
            await SortedSetManagement.AddItem(toAdd, value);
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
            Snackbar.Add(toAdd + " added.", Severity.Normal);
        }

        private async Task EmptySet()
        {
            await SortedSetManagement.Delete();
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
            Snackbar.Add("Deleted", Severity.Success);
        }

        private void RefreshMembers()
        {
            _setMembers = SortedSetManagement.GetTopTenSetItems();
            InvokeAsync(() => StateHasChanged());
        }

        /// <summary>
        /// When this component is initialized, specify an event handler method and subscribe to an event.
        /// </summary>
        protected override void OnInitialized()
        {
            RefreshMembers();
            SortedSetManagement.SortedSetManagementDemoNotification += RefreshMembers;
        }

        /// <summary>
        /// Unsubscribe from the queue notification event(s) and dispose of the queue management service
        /// </summary>
        public void Dispose()
        {
            SortedSetManagement.SortedSetManagementDemoNotification -= RefreshMembers;
            SortedSetManagement.Dispose();
        }

    }
}
