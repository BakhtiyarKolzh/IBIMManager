using Autodesk.Revit.DB;
using IBIMTool.Services;
using System;


namespace IBIMTool.RevitUtils
{
    public static class TransactionManager
    {
        private static readonly object SingleLocker = new object();
        private static TransactionStatus status = TransactionStatus.Uninitialized;

        /// <summary> The method used to create a single strx </summary>
        public static void CreateTransaction(Document document, string trxName, Action action)
        {
            lock (SingleLocker)
            {
                using (Transaction trx = new Transaction(document))
                {
                    status = trx.Start(trxName);
                    if (status == TransactionStatus.Started)
                    {
                        try
                        {
                            action?.Invoke();
                            status = trx.Commit();
                        }
                        catch (Exception ex)
                        {
                            if (!trx.HasEnded())
                            {
                                status = trx.RollBack();
                                string msg = $"Transaction: {trxName}\n";
                                IBIMLogger.Error(msg + ex.ToString());
                            }
                        }
                    }
                };
            }
        }
    }
}