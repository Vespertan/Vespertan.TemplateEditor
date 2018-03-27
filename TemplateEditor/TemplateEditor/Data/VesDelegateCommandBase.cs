﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Windows.Input;
using System.Threading;


namespace TemplateEditor
{
    /// <summary>
    /// An <see cref="ICommand"/> whose delegates can be attached for <see cref="Execute"/> and <see cref="CanExecute"/>.
    /// </summary>
    public abstract class VesDelegateCommandBase : VesBindableBase, ICommand
    {
        private bool _isActive;
        private bool _isRunning;

        public bool RaiseCanExecuteChangedOnIsRunningChanged { get; set; }
        public bool RaiseCanExecuteChangedOnIsActiveChanged { get; set; }

        private SynchronizationContext _synchronizationContext;
        private readonly HashSet<string> _observedPropertiesExpressions = new HashSet<string>();

        /// <summary>
        /// Creates a new instance of a <see cref="VesDelegateCommandBase"/>, specifying both the execute action and the can execute function.
        /// </summary>
        /// <param name="executeMethod">The <see cref="Action"/> to execute when <see cref="ICommand.Execute"/> is invoked.</param>
        /// <param name="canExecuteMethod">The <see cref="Func{Object,Bool}"/> to invoked when <see cref="ICommand.CanExecute"/> is invoked.</param>
        protected VesDelegateCommandBase()
        {
            _synchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public virtual event EventHandler CanExecuteChanged;

        /// <summary>
        /// Raises <see cref="ICommand.CanExecuteChanged"/> so every 
        /// command invoker can requery <see cref="ICommand.CanExecute"/>.
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
            {
                if (_synchronizationContext != null && _synchronizationContext != SynchronizationContext.Current)
                    _synchronizationContext.Post((o) => handler.Invoke(this, EventArgs.Empty), null);
                else
                    handler.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raises <see cref="VesDelegateCommandBase.CanExecuteChanged"/> so every command invoker
        /// can requery to check if the command can execute.
        /// <remarks>Note that this will trigger the execution of <see cref="VesDelegateCommandBase.InvokeCanExecute"/> once for each invoker.</remarks>
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        void ICommand.Execute(object parameter)
        {
            Execute(parameter);
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute(parameter);
        }

        protected abstract void Execute(object parameter);

        protected abstract bool CanExecute(object parameter);

        /// <summary>
        /// Observes a property that implements INotifyPropertyChanged, and automatically calls DelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
        /// </summary>
        /// <typeparam name="T">The object type containing the property specified in the expression.</typeparam>
        /// <param name="propertyExpression">The property expression. Example: ObservesProperty(() => PropertyName).</param>
        protected internal void ObservesPropertyInternal<T>(Expression<Func<T>> propertyExpression)
        {
            if (_observedPropertiesExpressions.Contains(propertyExpression.ToString()))
            {
                throw new ArgumentException($"{propertyExpression.ToString()} is already being observed.",
                    nameof(propertyExpression));
            }
            else
            {
                _observedPropertiesExpressions.Add(propertyExpression.ToString());
                PropertyObserver.Observes(propertyExpression, RaiseCanExecuteChanged);
            }
        }

        #region IsActive

        /// <summary>
        /// Gets or sets a value indicating whether the object is active.
        /// </summary>
        /// <value><see langword="true" /> if the object is active; otherwise <see langword="false" />.</value>
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                    OnIsActiveChanged();
                    if (RaiseCanExecuteChangedOnIsActiveChanged)
                    {
                        RaiseCanExecuteChanged();
                    }
                }
            }
        }

        private string _description;
        public string Description { get { return _description; } set { SetProperty(ref _description, value); } }

        /// <summary>
        /// Fired if the <see cref="IsActive"/> property changes.
        /// </summary>
        public virtual event EventHandler IsActiveChanged;


        /// <summary>
        /// This raises the <see cref="DelegateCommandBase.IsActiveChanged"/> event.
        /// </summary>
        protected virtual void OnIsActiveChanged()
        {
            IsActiveChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region IsRunning

        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged();
                    OnIsRunningChanged();
                    if (RaiseCanExecuteChangedOnIsRunningChanged)
                    {
                        RaiseCanExecuteChanged();
                    }
                }
            }
        }

        public virtual event EventHandler IsRunningChanged;

        private void OnIsRunningChanged()
        {
            IsRunningChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}