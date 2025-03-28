﻿/*
* This file is part of the libCEC(R) library.
*
* libCEC(R) is Copyright (C) 2011-2020 Pulse-Eight Limited.  All rights reserved.
* libCEC(R) is an original work, containing original code.
*
* libCEC(R) is a trademark of Pulse-Eight Limited.
*
* This program is dual-licensed; you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation; either version 2 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program; if not, write to the Free Software
* Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
*
*
* Alternatively, you can license this library under a commercial license,
* please contact Pulse-Eight Licensing for more information.
*
* For more information contact:
* Pulse-Eight Licensing       <license@pulse-eight.com>
*     http://www.pulse-eight.com/
*     http://www.pulse-eight.net/
*
* Author: Lars Op den Kamp <lars@opdenkamp.eu>
*
*/

using System.Drawing;
using LibCECTray.ui;
using Microsoft.Win32;
using System.Windows.Forms;

namespace LibCECTray.settings
{
  enum CECSettingSerialisationType
  {
    Numeric,
    String
  }

  enum CECSettingType
  {
    Numeric,
    String,
    Bool,
    Byte,
    DeviceType,
    LogicalAddress,
    LogicalAddresses,
    UShort,
    VendorId,
    Button,
    Generic
  }

  /// <summary>
  /// Base class for settings that can be saved in the registry
  /// </summary>
  abstract class CECSetting
  {
    /// <summary>
    /// Create a new setting
    /// </summary>
    /// <param name="type">The type of this setting</param>
    /// <param name="serialisationType">The serialisationType of the setting</param>
    /// <param name="keyName">The name of the key in the registry</param>
    /// <param name="friendlyName">The name of the setting in the UI</param>
    /// <param name="defaultValue">Default value of the setting</param>
    /// <param name="changedHandler">Called when the setting changed</param>
    protected CECSetting(CECSettingType type, CECSettingSerialisationType serialisationType, string keyName, string friendlyName, object defaultValue, SettingChangedHandler changedHandler)
    {
      SettingType = type;
      SettingSerialisationType = serialisationType;
      KeyName = keyName;
      FriendlyName = friendlyName;
      DefaultValue = defaultValue;
      _value = defaultValue;

      if (changedHandler != null)
        SettingChanged += changedHandler;
    }

    #region Serialisation methods
    /// <summary>
    /// Get the value of the setting in a form that can be stored in the registry
    /// </summary>
    /// <returns>The serialised value</returns>
    protected abstract string GetSerialisedValue();

    /// <summary>
    /// Set the value from the serialised form of it.
    /// </summary>
    /// <param name="value">The serialised value</param>
    protected abstract void SetSerialisedValue(string value);

    /// <summary>
    /// Get the default value of the setting in a form that can be stored in the registry
    /// </summary>
    /// <returns>The serialised default value</returns>
    protected abstract string GetSerialisedDefaultValue();

    /// <summary>
    /// Set the default value from the serialised form of it.
    /// </summary>
    /// <param name="value">The serialised default value</param>
    protected abstract void SetSerialisedDefaultValue(string value);
    #endregion

    /// <summary>
    /// Set the value to the default.
    /// </summary>
    public void ResetDefaultValue()
    {
      Value = DefaultValue;
    }

    public abstract void UpdateUI();

    #region Read/Write the corresponding registry key
    /// <summary>
    /// Load the value from the registry
    /// </summary>
    public void Load()
    {
      if (!StoreInRegistry)
      {
        return;
      }
      using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyName, true))
      {
        if (key != null)
        {
          _value = key.GetValue(KeyName) ?? DefaultValue;
          Changed = false;
        }
      }
    }

    public bool Save()
    {
      if (!StoreInRegistry)
      {
        return true;
      }
      if (!CreateRegistryKey())
      {
        return false;
      }
      using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyName, true))
      {
        if (key != null)
        {
          key.SetValue(KeyName, _value);
          Changed = false;
        }
        key.Close();
      }
      return true;
    }

    /// <summary>
    /// Create the registry key that holds all settings.
    /// </summary>
    /// <returns>True when created (or already existing), false otherwise</returns>
    private static bool CreateRegistryKey()
    {
      using (var regKey = Registry.CurrentUser.OpenSubKey("Software", true))
      {
        if (regKey != null)
        {
          regKey.CreateSubKey(RegistryCompanyName);
          regKey.Close();
        }
        else
        {
          return false;
        }
      }
      using (var regKey = Registry.CurrentUser.OpenSubKey("Software\\" + CECSetting.RegistryCompanyName, true))
      {
        if (regKey != null)
        {
          regKey.CreateSubKey(RegistryApplicationName);
          regKey.Close();
        }
        else
        {
          return false;
        }
      }
      return true;
    }
    #endregion

    #region GUI control replacement
    /// <summary>
    /// Replaces the controls in the form that was generated by the gui designer
    /// </summary>
    /// <param name="form">The form which contains the controls that are to be replaced</param>
    /// <param name="controls">The controls container which contains the controls that are to be replaced</param>
    /// <param name="labelControl">The label control to replace</param>
    /// <param name="valueControl">The value control to replace</param>
    public void ReplaceControls(IAsyncControls form, Control.ControlCollection controls, Control labelControl, Control valueControl)
    {
      Form = form;
      ReplaceControl(controls, labelControl, Label);
      ReplaceControl(controls, valueControl, ValueControl);
    }

    /// <summary>
    /// Replaces the controls in the form that was generated by the gui designer
    /// </summary>
    /// <param name="form">The form which contains the controls that are to be replaced</param>
    /// <param name="controls">The controls container which contains the controls that are to be replaced</param>
    /// <param name="valueControl">The value control to replace</param>
    public void ReplaceControls(AsyncForm form, Control.ControlCollection controls, Control valueControl)
    {
      Form = form;
      ReplaceControl(controls, valueControl, ValueControl);
    }

    /// <summary>
    /// Replaces the controls in the form that was generated by the gui designer
    /// </summary>
    /// <param name="controls">The controls container which contains the controls that are to be replaced</param>
    /// <param name="originalControl">The control to replace</param>
    /// <param name="replacement">The replacement</param>
    protected static void ReplaceControl(Control.ControlCollection controls, Control originalControl, Control replacement)
    {
      if (originalControl == null)
        return;

      var location = originalControl.Location;
      var originalSize = originalControl.Size;
      var tabIndex = originalControl.TabIndex;

      controls.Remove(originalControl);

      if (replacement != null)
      {
        controls.Add(replacement);
        replacement.Location = location;
        replacement.Size = originalSize;
        replacement.TabIndex = tabIndex;
      }
    }
    #endregion

    /// <summary>
    /// A setting changed
    /// </summary>
    /// <param name="setting">The setting that changed</param>
    /// <param name="oldValue">The old value</param>
    /// <param name="newValue">The new value</param>
    public delegate void SettingChangedHandler(CECSetting setting, object oldValue, object newValue);

    /// <summary>
    /// Checks if a setting may be enabled
    /// </summary>
    /// <param name="setting">The setting</param>
    /// <param name="value">The value that the controller wants to set</param>
    /// <returns>The Enabled value that will be used</returns>
    public delegate bool EnableSettingHandler(CECSetting setting, bool value);

    #region Convenience methods
    public CECSettingBool AsSettingBool
    {
      get { return this as CECSettingBool; }
    }
    public CECSettingByte AsSettingByte
    {
      get { return this as CECSettingByte; }
    }
    public CECSettingDeviceType AsSettingDeviceType
    {
      get { return this as CECSettingDeviceType; }
    }
    public CECSettingLogicalAddress AsSettingLogicalAddress
    {
      get { return this as CECSettingLogicalAddress; }
    }
    public CECSettingLogicalAddresses AsSettingLogicalAddresses
    {
      get { return this as CECSettingLogicalAddresses; }
    }
    public CECSettingNumeric AsSettingNumeric
    {
      get { return this as CECSettingNumeric; }
    }
    public CECSettingIdleTime AsSettingIdleTime {
      get { return this as CECSettingIdleTime; }
    }
    public CECSettingString AsSettingString
    {
      get { return this as CECSettingString; }
    }
    public CECSettingUShort AsSettingUShort
    {
      get { return this as CECSettingUShort; }
    }
    public CECSettingVendorId AsSettingVendorId
    {
      get { return this as CECSettingVendorId; }
    }
    #endregion

    #region Members
    public bool StoreInRegistry = true;

    /// <summary>
    /// Name of the key in the registry
    /// </summary>
    public string KeyName { protected set; get; }

    /// <summary>
    /// Name of the setting in the UI
    /// </summary>
    public string FriendlyName { protected set; get; }

    /// <summary>
    /// The current value of the setting
    /// </summary>
    public object Value {
      get { return _value; }
      set
      {
        if (_value == value) return;
        Changed = true;
        var oldValue = _value;
        _value = value;
        if (SettingChanged != null)
          SettingChanged(this, oldValue, value);
      }
    }
    private object _value;

    /// <summary>
    /// The default value of the setting
    /// </summary>
    public object DefaultValue { protected set; get; }

    /// <summary>
    /// The serialisationType of this setting
    /// </summary>
    public CECSettingSerialisationType SettingSerialisationType { private set; get; }

    /// <summary>
    /// The type of this setting
    /// </summary>
    public CECSettingType SettingType { private set; get; }

    /// <summary>
    /// True when changed and changes have not been persisted yet, false otherwise.
    /// </summary>
    public bool Changed { protected set; get; }

    /// <summary>
    /// The gui Control that contains the value
    /// </summary>
    public virtual Control ValueControl { get { return BaseValueControl; } }

    /// <summary>
    /// The value control to use in the gui
    /// </summary>
    protected Control BaseValueControl;

    /// <summary>
    /// True when changing the value of the ValueControl requires an invoke, false otherwise
    /// </summary>
    public virtual bool InvokeRequired
    {
      get { return BaseValueControl != null && BaseValueControl.InvokeRequired; }
    }

    /// <summary>
    /// The label with the description for this setting
    /// </summary>
    public Label Label
    {
      get {
        return _label ??
               (_label = new Label {AutoSize = true, Size = new Size(100, 13), Text = FriendlyName});
      }
    }
    private Label _label;

    private const string RegistryCompanyName = "Pulse-Eight";
    private const string RegistryApplicationName = "libCECTray";
    private static string RegistryKeyName {
      get { return string.Format("Software\\{0}\\{1}", RegistryCompanyName, RegistryApplicationName); }
    }

    /// <summary>
    /// Setting changed
    /// </summary>
    public event SettingChangedHandler SettingChanged;

    /// <summary>
    /// Setting will be enabled
    /// </summary>
    public EnableSettingHandler EnableSetting;

    /// <summary>
    /// The initial enabled state
    /// </summary>
    protected bool InitialEnabledValue { get; private set; } = true;

    /// <summary>
    /// The enabled state of the gui control
    /// </summary>
    public virtual bool Enabled
    {
      set
      {
        var newValue = value;
        if (EnableSetting != null)
          newValue = EnableSetting(this, value);

        InitialEnabledValue = newValue;
        if (Form != null && ValueControl != null)
          Form.SetControlEnabled(ValueControl, newValue);
      }
      get
      {
        return ValueControl != null ? ValueControl.Enabled : InitialEnabledValue;
      }
    }

    /// <summary>
    /// The for that contains the gui controls
    /// </summary>
    protected IAsyncControls Form;
    #endregion
  }
}
