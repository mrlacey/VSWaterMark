// <copyright file="VSWaterMarkPackage.cs" company="Matt Lacey Limited">
// Copyright (c) Matt Lacey Limited. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace VSWaterMark
{
    public class SponsorDetector
    {
        // This might be the code you see, but it's not what I compile into the extensions when built ;)
        public static async Task<bool> IsSponsorAsync()
        {
            return await Task.FromResult(false);
        }
    }
}
