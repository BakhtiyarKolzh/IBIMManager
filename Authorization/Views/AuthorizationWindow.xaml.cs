﻿using Authorization.Core;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Authorization
{
    public partial class AuthorizationWindow : Window
    {
        public static bool IsActivated { get; set; }
        private readonly Authentification viewmodel;

        public AuthorizationWindow()
        {
            InitializeComponent();
            viewmodel = new Authentification();
            DataContext = viewmodel;
        }


        private void SignIN_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                IsActivated = viewmodel.ValidateActivationInDataBase();
                Properties.Settings.Default.IsActivated = IsActivated;
                Properties.Settings.Default.Save();
                if (!IsActivated)
                {
                    throw new Exception("Не удалось найти");
                }
                else
                {
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
                    throw new Exception("Что то не так");
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
                LabelName.Content = "REGISTRATION";
                RegTextLink.Visibility = Visibility.Collapsed;
                PasswordField.Visibility = Visibility.Collapsed;
            });
        }



    }
}
