﻿/*
MIT License

Copyright(c) 2020 Evgeny Pereguda

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions :

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Input;
using System.Windows.Interop;

namespace WPFAreaScreenRecorder
{
    public class HotKeyEventArgs : EventArgs
    {
        public HotKey HotKey { get; private set; }

        public HotKeyEventArgs(HotKey hotKey)
        {
            HotKey = hotKey;
        }
    }

    [Serializable]
    public class HotKeyAlreadyRegisteredException : Exception
    {
        public HotKey HotKey { get; private set; }
        public HotKeyAlreadyRegisteredException(string message, HotKey hotKey) : base(message) { HotKey = hotKey; }
        public HotKeyAlreadyRegisteredException(string message, HotKey hotKey, Exception inner) : base(message, inner) { HotKey = hotKey; }
        protected HotKeyAlreadyRegisteredException(
          SerializationInfo info,
          StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class HotKey : INotifyPropertyChanged, ISerializable, IEquatable<HotKey>
    {
        /// <summary>
        /// Creates an HotKey object. This instance has to be registered in an HotKeyHost.
        /// </summary>
        public HotKey() { }

        /// <summary>
        /// Creates an HotKey object. This instance has to be registered in an HotKeyHost.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="modifiers">The modifier. Multiple modifiers can be combined with or.</param>
        public HotKey(Key key, ModifierKeys modifiers) : this(key, modifiers, true) { }

        /// <summary>
        /// Creates an HotKey object. This instance has to be registered in an HotKeyHost.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="modifiers">The modifier. Multiple modifiers can be combined with or.</param>
        /// <param name="enabled">Specifies whether the HotKey will be enabled when registered to an HotKeyHost</param>
        public HotKey(Key key, ModifierKeys modifiers, bool enabled)
        {
            Key = key;
            Modifiers = modifiers;
            Enabled = enabled;
        }


        private Key key;
        /// <summary>
        /// The Key. Must not be null when registering to an HotKeyHost.
        /// </summary>
        public Key Key
        {
            get { return key; }
            set
            {
                if (key != value)
                {
                    key = value;
                    OnPropertyChanged("Key");
                }
            }
        }

        private ModifierKeys modifiers;
        /// <summary>
        /// The modifier. Multiple modifiers can be combined with or.
        /// </summary>
        public ModifierKeys Modifiers
        {
            get { return modifiers; }
            set
            {

                if (modifiers != value)
                {
                    modifiers = value;

                    if (!mInnerModify)
                        fillModifiersCollcection(modifiers);

                    OnPropertyChanged("Modifiers");
                }
            }
        }


        private void fillModifiersCollcection(ModifierKeys aModifiers)
        {
            modifiersCollection.Clear();

            int lKeyCount = 0;

            do
            {

                ModifierKeys lModifierKey = ModifierKeys.None;
                
                if (aModifiers.HasFlag(ModifierKeys.Alt))
                {
                    lModifierKey = ModifierKeys.Alt;
                }
                else if (aModifiers.HasFlag(ModifierKeys.Control))
                {
                    lModifierKey = ModifierKeys.Control;
                }
                else if (aModifiers.HasFlag(ModifierKeys.Shift))
                {
                    lModifierKey = ModifierKeys.Shift;
                }
                else if (aModifiers.HasFlag(ModifierKeys.Windows))
                {
                    lModifierKey = ModifierKeys.Windows;
                }

                if (lModifierKey != ModifierKeys.None)
                {
                    aModifiers = aModifiers & (~lModifierKey);

                    modifiersCollection[lKeyCount++] = lModifierKey;
                }

            } while (aModifiers != ModifierKeys.None);

            mInnerModify = false;
        }

        private bool mInnerModify = false;

        private IDictionary<int, ModifierKeys> modifiersCollection = new Dictionary<int, ModifierKeys>();
        /// <summary>
        /// The modifier. Multiple modifiers can be combined with or.
        /// </summary>
        public IDictionary<int, ModifierKeys> ModifiersCollection
        {

            get { return modifiersCollection; }
            set
            {
                foreach (var item in value)
	            {
                    modifiersCollection[item.Key] = item.Value;
                }

                ModifierKeys lModifierKeys = ModifierKeys.None;
                                
                foreach (var item in modifiersCollection)
	            {
                    lModifierKeys |= item.Value;
                }

                mInnerModify = true;

                Modifiers = lModifierKeys;

                OnPropertyChanged("ModifiersCollection");
            }
        }

        private bool enabled;
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (value != enabled)
                {
                    enabled = value;
                    OnPropertyChanged("Enabled");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }


        public override bool Equals(object obj)
        {
            HotKey hotKey = obj as HotKey;
            if (hotKey != null)
                return Equals(hotKey);
            else
                return false;
        }

        public bool Equals(HotKey other)
        {
            return (Key == other.Key && Modifiers == other.Modifiers);
        }

        public override int GetHashCode()
        {
            return (int)Modifiers + 10 * (int)Key;
        }

        public override string ToString()
        {
            return string.Format("{0} + {1} ({2}Enabled)", Key, Modifiers, Enabled ? "" : "Not ");
        }

        /// <summary>
        /// Will be raised if the hotkey is pressed (works only if registed in HotKeyHost)
        /// </summary>
        public event EventHandler<HotKeyEventArgs> HotKeyPressed;

        protected virtual void OnHotKeyPress()
        {
            if (HotKeyPressed != null)
                HotKeyPressed(this, new HotKeyEventArgs(this));
        }

        internal void RaiseOnHotKeyPressed()
        {
            OnHotKeyPress();
        }


        protected HotKey(SerializationInfo info, StreamingContext context)
        {
            Key = (Key)info.GetValue("Key", typeof(Key));
            Modifiers = (ModifierKeys)info.GetValue("Modifiers", typeof(ModifierKeys));
            Enabled = info.GetBoolean("Enabled");
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Key", Key, typeof(Key));
            info.AddValue("Modifiers", Modifiers, typeof(ModifierKeys));
            info.AddValue("Enabled", Enabled);
        }
    }

    /// <summary>
    /// The HotKeyHost needed for working with hotKeys.
    /// </summary>
    public sealed class HotKeyHost : IDisposable
    {
        /// <summary>
        /// Creates a new HotKeyHost
        /// </summary>
        /// <param name="hwndSource">The handle of the window. Must not be null.</param>
        public HotKeyHost(HwndSource hwndSource)
        {
            if (hwndSource == null)
                throw new ArgumentNullException("hwndSource");

            this.hook = new HwndSourceHook(WndProc);
            this.hwndSource = hwndSource;
            hwndSource.AddHook(hook);
        }

        #region HotKey Interop

        private const int WM_HotKey = 786;

        [DllImport("user32", CharSet = CharSet.Ansi,
                   SetLastError = true, ExactSpelling = true)]
        private static extern int RegisterHotKey(IntPtr hwnd,
                int id, int modifiers, int key);

        [DllImport("user32", CharSet = CharSet.Ansi,
                   SetLastError = true, ExactSpelling = true)]
        private static extern int UnregisterHotKey(IntPtr hwnd, int id);

        #endregion

        #region Interop-Encapsulation

        private HwndSourceHook hook;
        private HwndSource hwndSource;

        private void RegisterHotKey(int id, HotKey hotKey)
        {
            if ((int)hwndSource.Handle != 0)
            {
                RegisterHotKey(hwndSource.Handle, id, (int)hotKey.Modifiers, KeyInterop.VirtualKeyFromKey(hotKey.Key));
                int error = Marshal.GetLastWin32Error();
                if (error != 0)
                {
                    Exception e = new Win32Exception(error);

                    if (error == 1409)
                        throw new HotKeyAlreadyRegisteredException(e.Message, hotKey, e);
                    else
                        throw e;
                }
            }
            else
                throw new InvalidOperationException("Handle is invalid");
        }

        private void UnregisterHotKey(int id)
        {
            if ((int)hwndSource.Handle != 0)
            {
                UnregisterHotKey(hwndSource.Handle, id);
                int error = Marshal.GetLastWin32Error();
                if (error != 0)
                    throw new Win32Exception(error);
            }
        }

        #endregion

        /// <summary>
        /// Will be raised if any registered hotKey is pressed
        /// </summary>
        public event EventHandler<HotKeyEventArgs> HotKeyPressed;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HotKey)
            {
                if (hotKeys.ContainsKey((int)wParam))
                {
                    HotKey h = hotKeys[(int)wParam];
                    h.RaiseOnHotKeyPressed();
                    if (HotKeyPressed != null)
                        HotKeyPressed(this, new HotKeyEventArgs(h));
                }
            }

            return new IntPtr(0);
        }


        void hotKey_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var kvPair = hotKeys.FirstOrDefault(h => h.Value == sender);
            if (kvPair.Value != null)
            {
                if (e.PropertyName == "Enabled")
                {
                    if (kvPair.Value.Enabled)
                        RegisterHotKey(kvPair.Key, kvPair.Value);
                    else
                        UnregisterHotKey(kvPair.Key);
                }
                else if (e.PropertyName == "Key" || e.PropertyName == "Modifiers")
                {
                    if (kvPair.Value.Enabled)
                    {
                        UnregisterHotKey(kvPair.Key);
                        RegisterHotKey(kvPair.Key, kvPair.Value);
                    }
                }
            }
        }


        private Dictionary<int, HotKey> hotKeys = new Dictionary<int, HotKey>();


        public class SerialCounter
        {
            public SerialCounter(int start)
            {
                Current = start;
            }

            public int Current { get; private set; }

            public int Next()
            {
                return ++Current;
            }
        }

        /// <summary>
        /// All registered hotKeys
        /// </summary>
        public IEnumerable<HotKey> HotKeys { get { return hotKeys.Values; } }


        private static readonly SerialCounter idGen = new SerialCounter(1); //Annotation: Can be replaced with "Random"-class

        /// <summary>
        /// Adds an hotKey.
        /// </summary>
        /// <param name="hotKey">The hotKey which will be added. Must not be null and can be registed only once.</param>
        public void AddHotKey(HotKey hotKey)
        {
            if (hotKey == null)
                throw new ArgumentNullException("value");
            if (hotKey.Key == 0)
                throw new ArgumentNullException("value.Key");
            if (hotKeys.ContainsValue(hotKey))
                throw new HotKeyAlreadyRegisteredException("HotKey already registered!", hotKey);

            int id = idGen.Next();
            if (hotKey.Enabled)
                RegisterHotKey(id, hotKey);
            hotKey.PropertyChanged += hotKey_PropertyChanged;
            hotKeys[id] = hotKey;
        }

        /// <summary>
        /// Removes an hotKey
        /// </summary>
        /// <param name="hotKey">The hotKey to be removed</param>
        /// <returns>True if success, otherwise false</returns>
        public bool RemoveHotKey(HotKey hotKey)
        {
            var kvPair = hotKeys.FirstOrDefault(h => h.Value == hotKey);
            if (kvPair.Value != null)
            {
                kvPair.Value.PropertyChanged -= hotKey_PropertyChanged;
                if (kvPair.Value.Enabled)
                    UnregisterHotKey(kvPair.Key);
                return hotKeys.Remove(kvPair.Key);
            }
            return false;
        }


        #region Destructor

        private bool disposed;

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                hwndSource.RemoveHook(hook);
            }

            for (int i = hotKeys.Count - 1; i >= 0; i--)
            {
                RemoveHotKey(hotKeys.Values.ElementAt(i));
            }


            disposed = true;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~HotKeyHost()
        {
            this.Dispose(false);
        }

        #endregion
    }

    [Serializable]
    public class CustomHotKey : HotKey
    {

        private Action mAction;

        public CustomHotKey(string name, Action aAction, Key key, ModifierKeys modifiers, bool enabled)
            : base(key, modifiers, enabled)
        {
            mAction = aAction;

            Name = name;
        }

        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                if (value != name)
                {
                    name = value;
                    OnPropertyChanged(name);
                }
            }
        }

        public override string ToString()
        {
            return name;
        }

        protected override void OnHotKeyPress()
        {
            if (mAction != null)
                mAction();

          //  System.Windows.MessageBox.Show(string.Format("'{0}' has been pressed ({1})", Name, this));

            base.OnHotKeyPress();
        }


        protected CustomHotKey(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            Name = info.GetString("Name");
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("Name", Name);
        }
    }

}
