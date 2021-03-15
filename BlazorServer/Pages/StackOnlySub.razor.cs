using Microsoft.AspNetCore.Components;
using MudBlazor;
using MyServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorServer.Pages
{
    public partial class StackOnlySub : IDisposable
    {
        [Inject]
        StackManagementService StackManagement { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        private List<string> _stackMembers;

        private async Task AddItem()
        {
            string toAdd = Guid.NewGuid().ToString();
            long numberOfItems = await StackManagement.AddItem(toAdd);
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
            Snackbar.Add(toAdd + " added. There are now " + numberOfItems + " in the list.", Severity.Normal);
        }

        private void RemoveExisting(string existing)
        {
            _stackMembers.Remove(existing);
            InvokeAsync(() => StateHasChanged());
        }

        private void AddNewAdditionFromChannel(string newAddition)
        {
            _stackMembers.Insert(0, newAddition);
            InvokeAsync(() => StateHasChanged());
        }

        /// <summary>
        /// When this component is initialized, specify an event handler method and subscribe to an event.
        /// </summary>
        protected override void OnInitialized()
        {
            _stackMembers = StackManagement.GetStackItems();
            StackManagement.StackRemovalTriggered += (string removed) =>
            {
                RemoveExisting(removed);
            };
            StackManagement.StackAdditionTriggered += (string added) =>
            {
                AddNewAdditionFromChannel(added);
            };
        }

        /// <summary>
        /// Unsubscribe from the stack notification event(s) and dispose of the stack management service
        /// </summary>
        public void Dispose()
        {
            StackManagement.StackRemovalTriggered -= (string removed) =>
            {
                RemoveExisting(removed);
            };
            StackManagement.StackAdditionTriggered -= (string added) =>
            {
                AddNewAdditionFromChannel(added);
            };
            StackManagement.Dispose();
        }
    }

}