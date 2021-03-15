using Microsoft.AspNetCore.Components;
using MudBlazor;
using MyServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorServer.Pages
{
    public partial class QueueDemo : IDisposable
    {
        [Inject]
        QueueManagementService QueueManagement { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        private async Task AddItem()
        {
            string toAdd = Guid.NewGuid().ToString();
            long numberOfItems = await QueueManagement.AddItem(toAdd);
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
            Snackbar.Add(toAdd + " added. There are now " + numberOfItems + " in the list.", Severity.Normal);
        }

        private void RefreshQueueMembers()
        {
            InvokeAsync(() => StateHasChanged());
        }

        /// <summary>
        /// When this component is initialized, specify an event handler method and subscribe to an event.
        /// </summary>
        protected override void OnInitialized()
        {
            QueueManagement.QueueManagerNotification += RefreshQueueMembers;
        }

        /// <summary>
        /// Unsubscribe from the queue notification event(s) and dispose of the queue management service
        /// </summary>
        public void Dispose()
        {
            QueueManagement.QueueManagerNotification -= RefreshQueueMembers;
            QueueManagement.Dispose();
        }
    }
}
