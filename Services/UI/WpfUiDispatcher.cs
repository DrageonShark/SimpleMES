using System.Windows;
using System.Windows.Threading;

namespace SimpleMES.Services.UI
{
    public class WpfUiDispatcher : IUiDispatcher
    {
        private readonly Dispatcher _dispatcher;

        private WpfUiDispatcher(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public static WpfUiDispatcher CreateDefault()
        {
            if (Application.Current?.Dispatcher is not null)
            {
                return new WpfUiDispatcher(Application.Current.Dispatcher);
            }
            return new WpfUiDispatcher(Dispatcher.CurrentDispatcher);
        }

        public void Invoke(Action action)
        {
            if (_dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                _dispatcher.Invoke(action);
            }
        }
    }
}
