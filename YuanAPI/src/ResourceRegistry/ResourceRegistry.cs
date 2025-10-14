using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YuanAPI;

/// <summary>
/// Indicates that loading something has failed
/// </summary>
public class LoadException(string message) : Exception(message) { }

[Submodule]
public class ResourceRegistry
{
    internal static void SetHooks()
    {
        throw new NotImplementedException();
    }
}
