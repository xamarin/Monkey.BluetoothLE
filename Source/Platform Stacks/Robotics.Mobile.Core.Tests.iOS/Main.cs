// -----------------------------------------------------------------------
// <copyright file="Main.cs" company="Teforia">
//    Copyright (c) 2015 Teforia Company and/or its subsidiary(-ies).
//    All rights reserved.
//
//    This software, including documentation, is protected by copyright
//    controlled by Teforia Company. All rights are reserved. Copying,
//    including reproducing, storing, adapting or translating, any or all
//    of this material requires the prior written consent of Teforia Company.
//    This material also contains confidential information which may not be
//    disclosed to others without the prior written consent of Teforia.
// </copyright>
// -----------------------------------------------------------------------
using UIKit;

namespace Robotics.Mobile.Core.Tests.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}
