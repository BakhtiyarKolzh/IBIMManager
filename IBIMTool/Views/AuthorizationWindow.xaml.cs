using IBIMTool.Authorization;
using MaterialDesignThemes.Wpf;
using System;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Threading;

namespace IBIMTool.Views
{
    public partial class AuthorizationWindow : Window
    {
        public static bool IsActivated { get; set; }
        private readonly AuthentificationViewModel viewmodel;

        public AuthorizationWindow()
        {
            InitializeComponent();
            ShadowAssist.SetDarken(this, true);
            viewmodel = new AuthentificationViewModel();
            DataContext = viewmodel;
        }


        private void SignIN_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                MessageText.Text = string.Empty;
                IsActivated = viewmodel.ValidateActivationInDataBase();
                Properties.Settings.Default.IsActivated = IsActivated;
                Properties.Settings.Default.Save();
                if (!IsActivated)
                {
                    if (string.IsNullOrEmpty(MessageText.Text))
                    {
                        MessageText.Text = "Something wrong";
                    }
                }
                else
                {
                    viewmodel.ControlVersion();
                    this.Close();
                }
            });
        }


        private void Registration_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                IsActivated = viewmodel.RegistrationCommandHandler();
                Properties.Settings.Default.IsActivated = IsActivated;
                Properties.Settings.Default.Save();
                if (!IsActivated)
                {
                    if (string.IsNullOrEmpty(MessageText.Text))
                    {
                        MessageText.Text = "Something wrong";
                    }
                }
                else
                {
                    this.Close();
                }
            });
        }


        private void ChageWindow_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                MessageText.Text = string.Empty;

                LabelName.Content = "REGISTRATION";

                Signin.Visibility = Visibility.Collapsed;
                RegTextLink.Visibility = Visibility.Collapsed;
                PasswordField.Visibility = Visibility.Collapsed;

                FirstName.Visibility = Visibility.Visible;
                LastName.Visibility = Visibility.Visible;
                Regin.Visibility = Visibility.Visible;
                 
            });
        }
    }
}
