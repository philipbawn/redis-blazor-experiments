using Microsoft.AspNetCore.Components;
using MudBlazor;
using MyServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorServer.Pages
{
    public partial class QueueOnlySub : IDisposable
    {
        [Inject]
        QueueManagementService QueueManagement { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        private List<string> _queueMembers;

        private async Task AddItem()
        {
            string toAdd = Guid.NewGuid().ToString();
            // If one navigates away from the component before this finishes, we get an exception unless we don't dispose of the connection multiplexer.
            long numberOfItems = await QueueManagement.AddItem(toAdd);
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
            Snackbar.Add(toAdd + " added. There are now " + numberOfItems + " in the list.", Severity.Normal);
        }

        private void RemoveExisting(string existing)
        {
            _queueMembers.Remove(existing);
            InvokeAsync(() => StateHasChanged());
        }

        private void AddNewAdditionFromChannel(string newAddition)
        {
            _queueMembers.Add(newAddition);
            InvokeAsync(() => StateHasChanged());
        }

        /// <summary>
        /// When this component is initialized, specify an event handler method and subscribe to an event.
        /// </summary>
        protected override void OnInitialized()
        {
            _queueMembers = QueueManagement.GetQueuedItems();
            QueueManagement.QueueRemovalTriggered += (string removed) =>
            {
                RemoveExisting(removed);
            };
            QueueManagement.QueueAdditionTriggered += (string added) =>
            {
                AddNewAdditionFromChannel(added);
            };
        }

        /// <summary>
        /// Unsubscribe from the queue notification event(s) and dispose of the queue management service
        /// </summary>
        public void Dispose()
        {
            QueueManagement.QueueRemovalTriggered -= (string removed) =>
            {
                RemoveExisting(removed);
            };
            QueueManagement.QueueAdditionTriggered -= (string added) =>
            {
                AddNewAdditionFromChannel(added);
            };
            QueueManagement.Dispose();
        }

    }
}
