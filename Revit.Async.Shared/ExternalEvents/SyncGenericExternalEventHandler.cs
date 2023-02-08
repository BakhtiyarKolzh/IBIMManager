﻿#region Reference

using Autodesk.Revit.UI;
using Revit.Async.Extensions;
using Revit.Async.Interfaces;

#endregion

namespace Revit.Async.ExternalEvents
{
    /// <summary>
    ///     Generic external event handler to execute sync code
    /// </summary>
    /// <typeparam name="TParameter">The type of the parameter</typeparam>
    /// <typeparam name="TResult">The type of the message</typeparam>
    public abstract class SyncGenericExternalEventHandler<TParameter, TResult> :
        GenericExternalEventHandler<TParameter, TResult>
    {
        #region Others

        /// <inheritdoc />
        protected sealed override void Execute(
            UIApplication                        app,
            TParameter                           parameter,
            IExternalEventResultHandler<TResult> resultHandler)
        {
            resultHandler.Wait(() => Handle(app, parameter));
        }

        /// <summary>
        ///     Override this method to execute sync business code
        /// </summary>
        /// <param name="app">The Revit top-level object, <see cref="UIApplication" /></param>
        /// <param name="parameter">The parameter</param>
        /// <returns>The message</returns>
        protected abstract TResult Handle(UIApplication app, TParameter parameter);

        #endregion
    }
}