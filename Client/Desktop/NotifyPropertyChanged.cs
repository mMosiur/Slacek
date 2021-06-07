using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Slacek.Client.Desktop
{
    public abstract class NotifyPropertyChanged : INotifyPropertyChanged
    {
        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public virtual void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real, public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                if (ThrowOnInvalidPropertyName)
                {
                    throw new Exception(msg);
                }
                else
                {
                    Debug.Fail(msg);
                }
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        /// <summary>
        /// Raised when a property on this object has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            VerifyPropertyName(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets this object's property to given value and raises <see cref="OnPropertyChanged(string)"/>
        /// of given property if the value has changed;
        /// </summary>
        /// <typeparam name="T">The type of the property</typeparam>
        /// <param name="storage">The storage of the property</param>
        /// <param name="value">The value, to which we want to set the property</param>
        /// <param name="propertyName">The property of which the value we set</param>
        /// <returns></returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Sets this object's property to given value and raises <see cref="OnPropertyChanged(string)"/>
        /// of given property even if the value has not changed;
        /// </summary>
        /// <typeparam name="T">The type of the property</typeparam>
        /// <param name="storage">The storage of the property</param>
        /// <param name="value">The value, to which we want to set the property</param>
        /// <param name="propertyName">The property of which the value we set</param>
        /// <returns></returns>
        protected virtual bool ForceSetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
