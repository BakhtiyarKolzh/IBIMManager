using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using IBIMTool.Views;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Reflection;


namespace IBIMTool.Authorization
{
    public sealed class AuthentificationViewModel : ObservableObject
    {

        //public bool IsActivated { get; set; } = Properties.Settings.Default.IsActivated;
        public bool IsActivated { get; set; } = false;

        #region Private Fields

        private static string login { get; set; } = Properties.Settings.Default.Email;
        private static string password { get; set; } = Properties.Settings.Default.Password;
        private static string serialNumber { get; set; } = Properties.Settings.Default.SerialNumber;
        
        #endregion


        #region Properties

        private string firstName;
        public string FirstName
        {
            get => firstName;
            set => SetProperty(ref firstName, value);
        }


        private string lastName;
        public string LastName
        {
            get => lastName;
            set => SetProperty(ref lastName, value);
        }

        private string email;
        [StringLength(30, MinimumLength = 5, ErrorMessage = "Email must contain at least 5 characters")]
        [RegularExpression(@"^[-\w.]+@([A-Za-z0-9][-A-Za-z0-9]+\.)+[A-Za-z]{2,4}$",
            ErrorMessage = "Email entered incorrectly")]
        public string Email
        {
            get => email;
            set => SetProperty(ref email, value);
        }


        private string msg;
        public string Message
        {
            get { return msg; }
            set { SetProperty(ref msg, value); }
        }

        #endregion


        public bool StartValidateActivation()
        {
            Properties.Settings.Default.Upgrade();
            if (!IsActivated)
            {
                var window = new AuthorizationWindow();
                window.Show();
                if (window.Activate())
                {
                    if (Properties.Settings.Default.IsActivated)
                    {
                        Properties.Settings.Default.Countdemo = 0;
                        Properties.Settings.Default.Save();
                        IsActivated = true;
                        ControlVersion();
                    }
                }
            }
            return IsActivated;
        }


        public bool ControlVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetName().Version.ToString();
            string queryExpression = $"UPDATE Users SET Version = '{version}' WHERE Email = '{email}'";
            DataTable table = DataBase.UpdateDataTable(queryExpression, out string msg);
            if (table == null) { throw new ArgumentNullException("DataTable"); }
            return table.Rows.Count == 1;
        }


        public bool ValidateActivationInDataBase()
        {
            string queryExpression = $"Select Id, IsActivated, Email, Password, SerialNumber from Users where IsActivated='{IsActivated}' and Email= '{email}' and Password='{password}' and SerialNumber='{serialNumber}'";
            DataTable table = DataBase.UpdateDataTable(queryExpression, out string msg);
            if (table == null) { throw new ArgumentNullException("DataTable"); }
            //Debug.WriteLine("Count: " + table.Rows.Count);
            else if (!string.IsNullOrEmpty(msg))
            {
                Message += msg;
            }
            IsActivated = table.Rows.Count == 1;
            return IsActivated;

        }


        public bool RegistrationCommandHandler()
        {
            bool result = false;
            serialNumber = HardDriveInfo.GetMainHardSerialNumber();
            if (string.IsNullOrEmpty(serialNumber))
            {
                throw new ArgumentNullException("SerialNumber");
            }
            else
            {
                result = RegistrationInDataBase(serialNumber, email);
                if (result)
                {
                    var mail = new MailManager();
                    var password = GetPasswordFromDataBase(email);
                    mail.SendDataToMail(email, password, out string msg);
                    mail.Dispose();
                }
            }
            return result;
        }


        private bool RegistrationInDataBase(string serialNumber, string email)
        {
            bool result = false;
            string queryExpression = $"Insert into Users (FirstName, LastName, Email, SerialNumber) Values ('{firstName}','{lastName}', '{email}', '{serialNumber}')";
            if (DataBase.ExecuteNonQueryHandler(queryExpression, out string msg))
            {
                result = true;
            }
            else if (!string.IsNullOrEmpty(msg))
            {
                Message += msg;
            }
            return result;
        }


        private static string GetPasswordFromDataBase(string email)
        {
            string result = string.Empty;
            string queryExpression = $"SELECT Password FROM Users WHERE Email = '{email}'";
            object item = DataBase.ExecuteScalarHandler(queryExpression);
            if (item is string password)
            {
                result = password;
            }
            return result;
        }


    }
}
