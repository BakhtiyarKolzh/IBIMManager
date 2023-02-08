using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace Authorization
{
    public class Authentification : ObservableValidator
    {

        public static bool IsActivated { get; set; } = Properties.Settings.Default.IsActivated;


        #region Fields

        public static string Login { get; set; } = Properties.Settings.Default.Login;
        public static string Password { get; set; } = Properties.Settings.Default.Password;
        public static string SerialNumber { get; set; } = Properties.Settings.Default.SerialNumber;

        #endregion


        #region Properties

        private string firstName;

        [StringLength(15, MinimumLength = 3, ErrorMessage = "FirstName must contain at least 3 characters")]
        [RegularExpression("^[A-Z][a-zA-Z]*$", ErrorMessage = "FirstName entered incorrectly")]
        public string FirstName
        {
            get => firstName;
            set => SetProperty(ref firstName, value, true);
        }


        private string lastName;

        [StringLength(15, MinimumLength = 3, ErrorMessage = "LastName must contain at least 3 characters")]
        [RegularExpression("^[A-Z][a-zA-Z]*$", ErrorMessage = "LastName entered incorrectly")]
        public string LastName
        {
            get => lastName;
            set => SetProperty(ref lastName, value, true);
        }

        private string email;

        [StringLength(30, MinimumLength = 5, ErrorMessage = "Email must contain at least 5 characters")]
        [RegularExpression(@"^[-\w.]+@([A-Za-z0-9][-A-Za-z0-9]+\.)+[A-Za-z]{2,4}$",
            ErrorMessage = "Email entered incorrectly")]
        public string Email
        {
            get => email;
            set => SetProperty(ref email, value, true);
        }

        #endregion


        #region ValidationResult

        private List<ValidationResult> errors = new List<ValidationResult>();


        public Authentification()
        {
            ErrorsChanged += Suspect_ErrorsChanged;
        }

        private void Suspect_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Errors));
        }
        int counts = 0;

        //public string Errors => string.Join(Environment.NewLine, from ValidationResult e in errors select e.ErrorMessage);
        public string Errors => string.Join(Environment.NewLine, from ValidationResult e in GetErrors(null) select e.ErrorMessage);

        #endregion


        public static bool ValidateActivation()
        {
            Properties.Settings.Default.Upgrade();
            if (!IsActivated)
            {
                var window = new AuthorizationWindow();
                window.Show();
                if (window.Activate())
                {
                    IsActivated = Properties.Settings.Default.IsActivated;
                    if (!IsActivated)
                    {

                    }
                }
            }
            return IsActivated;
        }


        public bool ValidateActivationInDataBase()
        {
            string queryExpression = $"Select id, IsActivated, Email, Password, SerialNumber from Users where IsActivated='{IsActivated}' and Email= '{Login}' and Password='{Password}' and SerialNumber='{SerialNumber}'";
            //$"Insert into Users (FirstName, LastName, Email, SerialNumber) Values ('{firstName}','{lastName}', '{Login}', '{SerialNumber}')"}
            DataTable table = DataBase.UpdateDataTable(queryExpression);
            IsActivated = table.Rows.Contains(email);
            return IsActivated;
        }


        public void RegistrationCommandHandler()
        {
            ValidateAllProperties();
            Debug.Print($"ValidateErrors counts {Errors.Length}");
            if (Errors.Length == counts)
            {
                //Debug.Print("IsValidResult!");
                //var mail = new MailManager();
                //string serialNumber = HardDriveInfo.GetMainHardSerialNumber();
                //if (Authentification.RegistrationInDataBase(firstName, lastName, email, serialNumber))
                //{
                //    var password = PasswordClient.GetPasswordFromDataBase(email);
                //    mail.SendDatatoMail(email, password);
                //}
            }
            else
            {
                counts = Errors.Length;
            }
        }

    }
}
