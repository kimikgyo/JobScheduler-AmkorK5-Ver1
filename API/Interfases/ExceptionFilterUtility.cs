using System;

internal static class ExceptionFilterUtility
{

    // log-and-handle
    // -로깅후, 예외 캐치블럭 실행 o
    // -예외 로깅 하고, 예외캐치블럭 실행하므로, 예외 발생하지 않음
    public static bool True(Action action)
    {
        action();
        return true;
    }

    // log-and-propagete
    // -로깅후, 예외 캐치블럭 실행 x
    // -예외 로깅 하고, 예외캐치블럭 실행하지 않으므로, 캐치되지않으므로 예외 그대로 발생한다
    public static bool False(Action action)
    {
        action();
        return false;
    }

}


public class AcceptFilterUtility
{
    private static Func<bool> _action = () => true;

    public static bool WriteAccepted
    {
        get
        {
            if (_action == null) return false;
            return _action();
        }
    }

    public static void SetAcceptFilterFunction(Func<bool> action)
    {
        _action = action;
    }

}

