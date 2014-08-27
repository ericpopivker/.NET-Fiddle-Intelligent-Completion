// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal static partial class SpecializedCollections
    {
        public static readonly byte[] EmptyBytes = EmptyArray<byte>();
        public static readonly object[] EmptyObjects = EmptyArray<object>();

        public static T[] EmptyArray<T>()
        {
            return Empty.Array<T>.Instance;
        }
    }
}