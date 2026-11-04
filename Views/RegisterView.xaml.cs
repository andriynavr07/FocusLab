using System.Windows.Controls;
using frontend.ViewModels;

namespace frontend.Views
{
    public partial class RegisterView : UserControl
    {
        public RegisterView()
        {
            InitializeComponent();

            PasswordInput.PasswordChanged += (_, _) =>
            {
                if (DataContext is RegisterViewModel vm) vm.Password = PasswordInput.Password;
            };
            ConfirmPasswordInput.PasswordChanged += (_, _) =>
            {
                if (DataContext is RegisterViewModel vm) vm.ConfirmPassword = ConfirmPasswordInput.Password;
            };
        }
    }
}