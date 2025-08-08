using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NexusSales.UI.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Make this method protected and virtual so derived classes can override it
        protected virtual async Task UnpinNotificationAsync(NotificationItem item)
        {
            // This method should be overridden in derived classes where _repository, _userEmail, and Bookmarks are defined.
            throw new NotImplementedException("UnpinNotificationAsync must be implemented in the derived ViewModel.");
        }
    }
}