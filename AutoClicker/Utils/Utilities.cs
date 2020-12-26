﻿using System;
using System.Reflection;
using System.Windows.Input;

namespace AutoClicker.Utils
{
    public static class Utilities
    {
        private static readonly Assembly assembly = Assembly.GetExecutingAssembly();

        public static AssemblyName GetAssemblyInfo()
            => assembly.GetName();

        public static string GetProjectURL()
            => assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;

        public static Uri GetProjectUri()
            => new Uri(GetProjectURL());

        public static RoutedUICommand CreateCommand(Type windowType, string commandName, KeyGesture keyGesture = null)
            => keyGesture == null
                ? new RoutedUICommand(commandName, commandName, windowType)
                : new RoutedUICommand(commandName, commandName, windowType, new InputGestureCollection() { keyGesture });
    }
}
