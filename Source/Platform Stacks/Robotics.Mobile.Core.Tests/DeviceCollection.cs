// -----------------------------------------------------------------------
// <copyright file="DeviceCollection.cs" company="Teforia">
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
using Xunit;

namespace Robotics.Mobile.Core.Tests
{
       
    [CollectionDefinition("IntegrationTest")]
    public class DeviceCollection : ICollectionFixture<DeviceFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}

