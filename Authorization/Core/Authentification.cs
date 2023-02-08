using Authorization.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Authorization.Core
{
    public class Authentification : ObservableValidator
    {

        public static bool IsActivated { get; set; } = Properties.Settings.Default.IsActivated;


        #region Private Fields

        private static string login { get; set; } = Properties.Settings.Default.Login;
        private static string password { get; set; } = Properties.Settings.Default.Password;
        private static string serialNumber { get; set; } = Properties.Settings.Default.SerialNumber;

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


        private string msg;

        public string Message
        {
            get { return msg; }
            set { SetProperty(ref msg, value); }
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
                }
            }
            return IsActivated;
        }


        public static bool ControlVersion(string email)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetName().Version.ToString();
            string queryExpression = $"UPDATE Users SET Version = '{version}' WHERE Email = '{email}'";
            DataTable table = DataBase.UpdateDataTable(queryExpression);
            return table.Rows.Contains("Version");
        }


        public bool ValidateActivationInDataBase()
        {
            string queryExpression = $"Select id, IsActivated, Email, password, serialNumber from Users where IsActivated='{IsActivated}' and Email= '{login}' and password='{password}' and serialNumber='{serialNumber}'";
            DataTable table = DataBase.UpdateDataTable(queryExpression);
            IsActivated = table.Rows.Contains(email);
            return IsActivated;
        }


        public bool RegistrationCommandHandler()
        {
            bool result = false;
            ValidateAllProperties();
            if (Errors.Length == counts)
            {
                serialNumber = HardDriveInfo.GetMainHardSerialNumber();
                if (string.IsNullOrEmpty(serialNumber))
                {
                    throw new ArgumentNullException("SerialNumber");
                }
                else
                {
                    result = RegistrationInDataBase(serialNumber);
                    if (result)
                    {
                        var mail = new MailManager();
                        var password = GetPasswordFromDataBase(email);
                        mail.SendDataToMail(email, password);
                        mail.Dispose();
                    }
                }
            }
            else
            {
                counts = Errors.Length;
            }
            return result;
        }


        private bool RegistrationInDataBase(string serialNumber)
        {
            bool result = false;
            string queryExpression = $"Insert into Users (FirstName, LastName, Email, serialNumber) Values ('{firstName}','{lastName}', '{login}', '{serialNumber}')";
            if (DataBase.ExecuteNonQueryHandler(queryExpression))
            {
                Message = " ... password... ";
                result = true;
            }
            return result;
        }


        public static string GetPasswordFromDataBase(string email)
        {
            string result = string.Empty;
            string queryExpression = $"SELECT password FROM Users WHERE Email = '{email}'";
            object item = DataBase.ExecuteScalarHandler(queryExpression);
            if (item is string password)
            {
                result = password;
            }
            return result;
        }


    }
}
