#region Reference

using System;

#endregion

namespace Revit.Async.Interfaces
{
    /// <summary>
    ///     Interface to handle the external event message
    /// </summary>
    /// <typeparam name="TResult">The type of the message</typeparam>
    public interface IExternalEventResultHandler<in TResult>
    {
        #region Others

        /// <summary>
        ///     Cancel the task
        /// </summary>
        void Cancel();

        /// <summary>
        ///     Set some the message when the handler is done
        /// </summary>
        /// <param name="result">The message object</param>
        void SetResult(TResult result);

        /// <summary>
        ///     Set an <see cref="Exception" /> to the task
        /// </summary>
        /// <param name="exception">The <see cref="Exception" /> object</param>
        void ThrowException(Exception exception);

        #endregion
    }
}