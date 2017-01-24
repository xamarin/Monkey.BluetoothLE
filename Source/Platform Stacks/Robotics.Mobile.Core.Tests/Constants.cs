// -----------------------------------------------------------------------
// <copyright file="Constants.cs" company="Teforia">
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
using System;
using Robotics.Mobile.Core.Bluetooth.LE;

namespace Robotics.Mobile.Core.Tests
{
    public class Constants
    {
//        public static readonly Guid HEART_RATE_SERVICE_UUID = Extensions.UuidFromPartial(0x180D);
        public static readonly Guid TEST_SERVICE_UUID = new Guid("b313e514-e7a2-c6fe-53c8-2a7e9e7ee589");
        public static readonly Guid TEST_CHARACTERISTIC_UUID =new Guid("8ba16c63-1810-c6d2-1a3d-09eb265d0000");

        public Constants()
        {
        }
    }
}

