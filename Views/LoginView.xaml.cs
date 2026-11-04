using System.Windows.Controls;
using frontend.ViewModels;

namespace frontend.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            PasswordInput.PasswordChanged += (_, _) =>
            {
                if (DataContext is LoginViewModel vm)
                    vm.Password = PasswordInput.Password;
            };
        }
    }
}
