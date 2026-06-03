using Microsoft.Extensions.DependencyInjection;

namespace LabsApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new NavigationPage(new AppShell()));
            //return new Window(new AppShell());
        }
    }
}