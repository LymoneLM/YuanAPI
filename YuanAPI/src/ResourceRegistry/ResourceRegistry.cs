using System;

namespace YuanAPI;

/// <summary>
/// Indicates that loading something has failed
/// </summary>
public class LoadException(string message) : Exception(message) { }

[Submodule]
public class ResourceRegistry
{
    public static void SetHooks()
    {
        
    }
}
