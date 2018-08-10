﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Automation;
using static System.Windows.Automation.AutomationElement;
using System.Threading;
using System.Diagnostics;
using Microsoft.Test.Input;

namespace GUILibrary
{
    public static class GUILibraryClass
    {
        private static Dictionary<string, AutomationProperty> propertyMap = new Dictionary<string, AutomationProperty>(StringComparer.InvariantCultureIgnoreCase)
    {
        {"AutomationId",AutomationIdProperty},
        {"Id",AutomationIdProperty},
        {"Name",NameProperty},
        {"Title",NameProperty},
        {"Class",ClassNameProperty},
        {"ClassName",ClassNameProperty},
        {"AcceleratorKey",AcceleratorKeyProperty},
        {"AccessKey",AccessKeyProperty},
        {"BoundingRectangle",BoundingRectangleProperty},
        {"ClickablePoint",ClickablePointProperty},
        {"ControlType",ControlTypeProperty},
        {"Culture",CultureProperty},
        {"FrameworkId",FrameworkIdProperty},
        {"HasKeyboardFocus",HasKeyboardFocusProperty},
        {"HelpText",HelpTextProperty},
        {"IsContentElement",IsContentElementProperty},
        {"IsControlElement",IsControlElementProperty},
        {"IsDockPatternAvailable",IsDockPatternAvailableProperty},
        {"IsEnabled",IsEnabledProperty},
        {"IsExpandCollapsePatternAvailable",IsExpandCollapsePatternAvailableProperty},
        {"IsGridItemPatternAvailable",IsGridItemPatternAvailableProperty},
        {"IsGridPatternAvailable",IsGridPatternAvailableProperty},
        {"IsInvokePatternAvailable",IsInvokePatternAvailableProperty},
        {"IsItemContainerPatternAvailable",IsItemContainerPatternAvailableProperty},
        {"IsKeyboardFocusable",IsKeyboardFocusableProperty},
        {"IsMultipleViewPatternAvailable",IsMultipleViewPatternAvailableProperty},
        {"IsOffscreen",IsOffscreenProperty},
        {"IsPassword",IsPasswordProperty},
        {"IsRangeValuePatternAvailable",IsRangeValuePatternAvailableProperty},
        {"IsRequiredForForm",IsRequiredForFormProperty},
        {"IsScrollItemPatternAvailable",IsScrollItemPatternAvailableProperty},
        {"IsScrollPatternAvailable",IsScrollPatternAvailableProperty},
        {"IsSelectionItemPatternAvailable",IsSelectionItemPatternAvailableProperty},
        {"IsSelectionPatternAvailable",IsSelectionPatternAvailableProperty},
        {"IsSynchronizedInputPatternAvailable",IsSynchronizedInputPatternAvailableProperty},
        {"IsTableItemPatternAvailable",IsTableItemPatternAvailableProperty},
        {"IsTablePatternAvailable",IsTablePatternAvailableProperty},
        {"IsTextPatternAvailable",IsTextPatternAvailableProperty},
        {"IsTogglePatternAvailable",IsTogglePatternAvailableProperty},
        {"IsTransformPatternAvailable",IsTransformPatternAvailableProperty},
        {"IsValuePatternAvailable",IsValuePatternAvailableProperty},
        {"IsVirtualizedItemPatternAvailable",IsVirtualizedItemPatternAvailableProperty},
        {"IsWindowPatternAvailable",IsWindowPatternAvailableProperty},
        {"ItemStatus",ItemStatusProperty},
        {"ItemType",ItemTypeProperty},
        {"LabeledBy",LabeledByProperty},
        {"LocalizedControlType",LocalizedControlTypeProperty},
        {"ProcessId",ProcessIdProperty},
        {"NativeWindowHandle",NativeWindowHandleProperty},
        {"Orientation",OrientationProperty},
        {"RuntimeId",RuntimeIdProperty},
    };

        private static AutomationElement activeWindow;

        public static string Read(string selector, int child = 0, double timeout = 5)
        {
            AutomationElement control = Search(selector, child);
            return getElementText(control);
        }

        public static void setWindow(string window,double timeout=5)
        {
            activeWindow = RootElement;
            AutomationElement windowElement = Search(window, 0, timeout);
            windowElement.SetFocus();
            activeWindow = windowElement;
        }

        private static AutomationElement Search(string selector, int child = 0, double timeout=5)
        {

            List<PropertyCondition> conditionList = new List<PropertyCondition>();
            Dictionary<string, string> valueParameter = new Dictionary<string, string>();

            //if user chooses to search by value then we search by value only. This is to keep code simple
            if (selector.Contains("value"))
            {
                return FindByValue(selector,timeout);
            }

            Dictionary<AutomationProperty, string> searchParameters = ParseSelector(selector);
            foreach (KeyValuePair<AutomationProperty, string> entry in searchParameters)
            {
                conditionList.Add(new PropertyCondition(entry.Key, entry.Value));
            }

            Condition[] conditionsArray = conditionList.ToArray();
            Condition searchConditions = new AndCondition(conditionsArray);

            return FindWithTimeout(selector, searchConditions, child, timeout);

        }
        private static AutomationElement FindWithTimeout(string selector, Condition searchConditions, int child = 0, double timeout = 5)
        {
            AutomationElement searchElement = null;

            Stopwatch searchTime = new Stopwatch();
            searchTime.Start();
            while (searchTime.Elapsed.Seconds < timeout)
            {
                try
                {
                    if (child == 0)
                    {
                        if (activeWindow == RootElement)
                        {
                            searchElement = activeWindow.FindFirst(TreeScope.Children, searchConditions);
                        }
                        else
                        {
                            searchElement = activeWindow.FindFirst(TreeScope.Descendants, searchConditions);
                        }
                    }
                    else
                    {
                        searchElement = activeWindow.FindAll(TreeScope.Descendants, searchConditions)[child];
                    }

                    if (searchElement != null && (bool)searchElement.GetCurrentPropertyValue(IsEnabledProperty))
                    {
                        searchTime.Stop();
                        return searchElement;
                    }
                }
                catch (NullReferenceException)
                {
                }
            }
            searchTime.Stop();
            throw new ElementNotAvailableException("Cound not find element: " + selector + " in " + activeWindow + " in " + timeout + " seconds");
        }

        /// <summary>
        /// Helper method of search() that finds an element if the user wants to select by value. 
        /// </summary>
        /// <param name="selector"></param>
        /// <returns>automation element if successfull</returns>
        private static AutomationElement FindByValue(string selector,double timeout)
        {
            int firstIndex = selector.IndexOf(":") + 1;
            string controlValue = selector.Substring(firstIndex, selector.Length - firstIndex);

            Stopwatch searchTime = new Stopwatch();
            searchTime.Start();
            while (searchTime.Elapsed.Seconds < timeout)
            {
                try
                {
                    // Use ControlViewCondition to retrieve all control elements then manually search each one.
                    AutomationElementCollection elementCollectionControl = activeWindow.FindAll(TreeScope.Subtree, Automation.ControlViewCondition);
                    foreach (AutomationElement autoElement in elementCollectionControl)
                    {
                        if (getElementText(autoElement) == controlValue)
                        {
                            searchTime.Stop();
                            return autoElement;
                        }
                    }
                }catch (NullReferenceException) { }
            }
            searchTime.Stop();
            throw new ElementNotAvailableException("Could not find an element with value: " + controlValue);
        }

        private static string getElementText(AutomationElement element)
        {
            var hasValue = (bool)element.GetCurrentPropertyValue(IsValuePatternAvailableProperty);
            var hasText = (bool)element.GetCurrentPropertyValue(IsTextPatternAvailableProperty);

            if (hasValue)
            {
                ValuePattern valuePattern = (ValuePattern)element.GetCurrentPattern(ValuePattern.Pattern);
                return valuePattern.Current.Value;
            }
            else if (hasText)
            {
                TextPattern textPattern = (TextPattern)element.GetCurrentPattern(ValuePattern.Pattern);
                return textPattern.DocumentRange.GetText(-1).TrimEnd('\r'); // often there is an extra '\r' hanging off the end.
            }

            return null;
        }

        private static Dictionary<AutomationProperty, string> ParseSelector(string selector)
        {
            Dictionary<AutomationProperty, string> selectorDict = new Dictionary<AutomationProperty, string>(); //contains selectors with their values
            string[] props = selector.Split(',');
            foreach (string prop in props)
            {
                string[] propWithValues = prop.Split(':'); //split the property with values the user entered on the :

                //if we cannot split the string then we assume that the user meant to use the name property
                if (propWithValues.Length == 1)
                {
                    selectorDict.Add(NameProperty, propWithValues[0]);
                    return selectorDict;
                }

                AutomationProperty Automationprop = propertyMap[propWithValues[0]]; //map the string name to the AutomationProperty
                selectorDict.Add(Automationprop, propWithValues[1]);
                System.Diagnostics.Debug.WriteLine(propWithValues[1]);
            }
            return selectorDict;
        }

        ///--------------------------------------------------------------------
        /// <summary>
        /// Clicks on the control of interest.
        /// </summary>
        /// <param name="selector">expression to match a control.</param>
        /// <param name="child">Which child to choose if multiple controls are chosen</param>
        ///--------------------------------------------------
        public static void Click(string selector, int child = 0)
        {
            AutomationElement control = Search(selector, child);

            var isInvokable = (bool)control.GetCurrentPropertyValue(IsInvokePatternAvailableProperty);
            if (isInvokable)
            {
                var invokePattern = control.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                invokePattern.Invoke();
            }
            else
            {
                //click manually by moving mouse and clicking left mouse button
                System.Drawing.Point p = control.GetClickablePoint();
                Mouse.MoveTo(p);
                Mouse.Click(MouseButton.Left);
            }
        }

        public static void SendKey(string keys)
        {
            SendKeys.SendWait(keys);
        }

        public static void RightClick(string selector,int child=0)
        {
            AutomationElement control = Search(selector, child);
            Point p = control.GetClickablePoint();
            Mouse.MoveTo(p);
            Mouse.Click(MouseButton.Right);
        }



        public static void Append(string value,string selector,int child=0,string mode = "append")
        {
            Write(value, selector, child, mode);
        }
        ///--------------------------------------------------------------------
        /// <summary>
        /// Inserts a string into each text control of interest.
        /// </summary>
        /// <param name="value">String to be inserted</param>
        /// <param name="selector">selector to find the text control</param>
        /// <param name="child">which child to choose if multiple controls are chosen</param>
        ///--------------------------------------------------------------------
        public static void Write(string value, string selector, int child = 0, string mode="overwrite")
        {
            AutomationElement element = Search(selector, child);  //finds the element to write to

            try
            {
                // Validate arguments / initial setup
                if (value == null)
                    throw new ArgumentNullException(
                        "String parameter must not be null.");

                if (element == null)
                    throw new ArgumentNullException(
                        "AutomationElement parameter must not be null");


                // A series of basic checks prior to attempting an insertion.
                //
                // Check #1: Is control enabled?
                // An alternative to testing for static or read-only controls 
                // is to filter using 
                // PropertyCondition(AutomationElement.IsEnabledProperty, true) 
                // and exclude all read-only text controls from the collection.
                if (!element.Current.IsEnabled)
                {
                    throw new InvalidOperationException(
                        "The control with an AutomationID of "
                        + element.Current.AutomationId.ToString()
                        + " is not enabled.\n\n");
                }

                // Check #2: Are there styles that prohibit us 
                //           from sending text to this control?
                if (!element.Current.IsKeyboardFocusable)
                {
                    throw new InvalidOperationException(
                        "The control with an AutomationID of "
                        + element.Current.AutomationId.ToString()
                        + "is read-only.\n\n");
                }


                // Once you have an instance of an AutomationElement,  
                // check if it supports the ValuePattern pattern.
                object valuePattern = null;

                // Control does not support the ValuePattern pattern 
                // so use keyboard input to insert content.
                //
                // NOTE: Elements that support TextPattern 
                //       do not support ValuePattern and TextPattern
                //       does not support setting the text of 
                //       multi-line edit or document controls.
                //       For this reason, text input must be simulated
                //       using one of the following methods.
                //       
                if (!element.TryGetCurrentPattern(
                    ValuePattern.Pattern, out valuePattern))
                {
                    Debug.WriteLine("The control with an AutomationID of ");
                    Debug.Write(element.Current.AutomationId.ToString());
                    Debug.Write(" does not support ValuePattern.");
                    Debug.Write(" Using keyboard input.\n");

                    // Set focus for input functionality and begin.
                    element.SetFocus();

                    // Pause before sending keyboard input.
                    Thread.Sleep(100);

                    if (mode == "overwrite")
                    {
                        //Delete existing content in the control
                        SendKeys.SendWait("^{HOME}");   // Move to start of control
                        SendKeys.SendWait("^+{END}");   // Select everything
                        SendKeys.SendWait("{DEL}");     // Delete selection
                    }

                    //insert new content.
                    SendKeys.SendWait(value);
                }
                // Control supports the ValuePattern pattern so we can 
                // use the SetValue method to insert content.
                else
                {
                    Debug.WriteLine("The control with an AutomationID of ");
                    Debug.Write(element.Current.AutomationId.ToString());
                    Debug.Write((" supports ValuePattern."));
                    Debug.Write(" Using ValuePattern.SetValue().\n");

                    // Set focus for input functionality and begin.
                    element.SetFocus();

                    ((ValuePattern)valuePattern).SetValue(value);
                }
            }
            catch (ArgumentNullException exc)
            {
                Console.WriteLine(exc.Message);
            }
            catch (InvalidOperationException exc)
            {
                Console.WriteLine(exc.Message);
            }
        }
    }
}
