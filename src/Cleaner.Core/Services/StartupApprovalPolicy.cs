/*
 * Adapted from BCUninstaller NewStartupDisable.cs
 * Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
 * Licensed under the Apache License, Version 2.0.
 * https://www.apache.org/licenses/LICENSE-2.0
 * Upstream commit 30da609384c98ba6e35c6530129541ea4ff3970a.
 * QuietCare changes: enum-based mapping, pure testable state encoding, no key creation on reads.
 */
namespace Cleaner.Core.Services;

public static class StartupApprovalPolicy
{
    public static string SubKey(AutostartSource source)
    {
        const string root=@"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\";
        return root+(source switch
        {
            AutostartSource.StartupFolderUser or AutostartSource.StartupFolderCommon=>"StartupFolder",
            AutostartSource.HklmRun32=>"Run32",
            AutostartSource.HklmRunOnce or AutostartSource.HkcuRunOnce=>"RunOnce",
            AutostartSource.HklmRun or AutostartSource.HkcuRun or AutostartSource.HkcuRun32=>"Run",
            _=>throw new ArgumentOutOfRangeException(nameof(source))
        });
    }
    public static byte[] Encode(bool enabled)=>[enabled?(byte)0x02:(byte)0x03,0,0,0,0,0,0,0,0,0,0,0];
    public static bool IsEnabled(byte[]? value)=>value is null || value.Length==0 || value[0]!=0x03;
}
