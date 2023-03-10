using System;
using System.Threading;


namespace IBIMTool.Services
{
    class MutexServise : IDisposable
    {
        bool hasHandle;
        readonly Mutex mutex;
        private bool disposedValue;

        public MutexServise()
        {
            string mutexId = string.Format("Global\\{{{0}}}", "53d0e989-f27a-48be-9287-a7c842d44bf2");
            mutex = new Mutex(false, mutexId);
        }

        public void EnsureUnique()
        {
            try
            {
                hasHandle = mutex.WaitOne(Timeout.Infinite, false);
                if (hasHandle == false)
                    throw new TimeoutException("Копия приложения уже запущена; завершите ее перед запуском новой копии.");
            }
            catch (AbandonedMutexException)
            {
                // Log the fact that the mutex was abandoned in another process,
                // it will still get acquired
                hasHandle = true;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (hasHandle) mutex.ReleaseMutex();
                    mutex.Dispose();
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
                // TODO: установить значение NULL для больших полей
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
