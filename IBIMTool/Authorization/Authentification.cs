using CommunityToolkit.Mvvm.ComponentModel;
using IBIMTool.Views;
using System;
using System.Data;
using System.Diagnostics;
using System.Reflection;


namespace IBIMTool.Authorization
{
    public sealed class AuthentificationViewModel : ObservableObject
    {

        public bool IsActivated { get; set; } = Properties.Settings.Default.IsActivated;


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
            DataTable table = DataBase.UpdateDataTable(queryExpression);
            if (table == null) { throw new ArgumentNullException("DataTable"); }
            return table.Rows.Count == 1;
        }


        public bool ValidateActivationInDataBase()
        {
            string queryExpression = $"Select Id, IsActivated, Email, Password, SerialNumber from Users where IsActivated='{IsActivated}' and Email= '{login}' and Password='{password}' and SerialNumber='{serialNumber}'";
            DataTable table = DataBase.UpdateDataTable(queryExpression);
            if (table == null) { throw new ArgumentNullException("DataTable"); }
            Debug.WriteLine("Count: " + table.Rows.Count);
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
                result = RegistrationInDataBase(serialNumber);
                if (result)
                {
                    var mail = new MailManager();
                    var password = GetPasswordFromDataBase(email);
                    mail.SendDataToMail(email, password);
                    mail.Dispose();
                }
            }
            return result;
        }


        private bool RegistrationInDataBase(string serialNumber)
        {
            bool result = false;
            string queryExpression = $"Insert into Users (FirstName, LastName, Email, SerialNumber) Values ('{firstName}','{lastName}', '{login}', '{serialNumber}')";
            if (DataBase.ExecuteNonQueryHandler(queryExpression))
            {
                Message = " ... password... ";
                result = true;
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
