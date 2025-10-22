// Source code is taken from CommonAPI (GPL-3.0) - https://github.com/limoka/CommonAPI
namespace YuanAPI;

/// <summary>
/// Static container for logger interface to appease Unit testing assembly...
/// 静态日志类，YuanAPI内部日志都应该使用这个类
/// </summary>
public static class YuanLogger
{
    private static IYuanLogger _logger;

    /// <summary>
    /// 设置日志类，不建议YuanAPI项目以外的项目调用，除非你知道你在干什么
    /// </summary>
    /// <param name="logger">实现了IYuanLogger的日志对象</param>
    public static void SetLogger(IYuanLogger logger)
    {
        _logger = logger;
    }

    public static void LogFatal(object data) => _logger.LogFatal(data);
    public static void LogError(object data) => _logger.LogError(data);
    public static void LogWarning(object data) => _logger.LogWarning(data);
    public static void LogMessage(object data) => _logger.LogMessage(data);
    public static void LogInfo(object data) => _logger.LogInfo(data);
    public static void LogDebug(object data) => _logger.LogDebug(data);

}
