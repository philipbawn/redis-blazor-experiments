using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyServices;
using MudBlazor;

namespace BlazorServer.Pages
{
    public partial class StackDemo : IDisposable
    {
        [Inject]
        StackManagementService StackManagement { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        private void AddItem()
        {
            string toAdd = Guid.NewGuid().ToString();
            StackManagement.AddItem(toAdd);
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
            Snackbar.Add(toAdd + " added.", Severity.Normal);
        }

        private void RefreshStackMembers()
        {
            InvokeAsync(() => StateHasChanged());
        }

        /// <summary>
        /// When this component is initialized, specify an event handler method and subscribe to an event.
        /// </summary>
        protected override void OnInitialized()
        {
            StackManagement.StackManagerNotification += RefreshStackMembers;
        }

        /// <summary>
        /// Unsubscribe from the stack notification event(s) and dispose of the stack management service
        /// </summary>
        public void Dispose()
        {
            StackManagement.StackManagerNotification -= RefreshStackMembers;
            StackManagement.Dispose();
        }


    }
}
