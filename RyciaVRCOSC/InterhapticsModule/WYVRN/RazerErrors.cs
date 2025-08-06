namespace RyciaVRCOSC.InterhapticsModule.Haptics.WYVRN
{
    public class RazerErrors // Error codes
    {
        public const int RZRESULT_INVALID = -1; //! Invalid
        public const int RZRESULT_SUCCESS = 0; //! Success
        public const int RZRESULT_ACCESS_DENIED = 5; //! Access denied
        public const int RZRESULT_INVALID_HANDLE = 6; //! Invalid handle
        public const int RZRESULT_NOT_SUPPORTED = 50; //! Not supported
        public const int RZRESULT_INVALID_PARAMETER = 87; //! Invalid parameter.
        public const int RZRESULT_SERVICE_NOT_ACTIVE = 1062; //! The service has not been started
        public const int RZRESULT_SINGLE_INSTANCE_APP = 1152; //! Cannot start more than one instance of the specified program.
        public const int RZRESULT_DEVICE_NOT_CONNECTED = 1167; //! Device not connected
        public const int RZRESULT_NOT_FOUND = 1168; //! Element not found.
        public const int RZRESULT_REQUEST_ABORTED = 1235; //! Request aborted.
        public const int RZRESULT_ALREADY_INITIALIZED = 1247; //! An attempt was made to perform an initialization operation when initialization has already been completed.
        public const int RZRESULT_RESOURCE_DISABLED = 4309; //! Resource not available or disabled
        public const int RZRESULT_DEVICE_NOT_AVAILABLE = 4319; //! Device not available or supported
        public const int RZRESULT_NOT_VALID_STATE = 5023; //! The group or resource is not in the correct state to perform the requested operation.
        public const int RZRESULT_NO_MORE_ITEMS = 259; //! No more items
        public const int RZRESULT_DLL_NOT_FOUND = 6023; //! DLL not found
        public const int RZRESULT_DLL_INVALID_SIGNATURE = 6033; //! Invalid signature
        public const int RZRESULT_FAILED = unchecked((int)2147500037); //! General failure.
        public static string GetResultString(int result)
        {
            switch (result)
            {
                case RZRESULT_INVALID: return "RZRESULT_INVALID";
                case RZRESULT_SUCCESS: return "RZRESULT_SUCCESS";
                case RZRESULT_ACCESS_DENIED: return "RZRESULT_ACCESS_DENIED";
                case RZRESULT_INVALID_HANDLE: return "RZRESULT_INVALID_HANDLE";
                case RZRESULT_NOT_SUPPORTED: return "RZRESULT_NOT_SUPPORTED";
                case RZRESULT_INVALID_PARAMETER: return "RZRESULT_INVALID_PARAMETER";
                case RZRESULT_SERVICE_NOT_ACTIVE: return "RZRESULT_SERVICE_NOT_ACTIVE";
                case RZRESULT_SINGLE_INSTANCE_APP: return "RZRESULT_SINGLE_INSTANCE_APP";
                case RZRESULT_DEVICE_NOT_CONNECTED: return "RZRESULT_DEVICE_NOT_CONNECTED";
                case RZRESULT_NOT_FOUND: return "RZRESULT_NOT_FOUND";
                case RZRESULT_REQUEST_ABORTED: return "RZRESULT_REQUEST_ABORTED";
                case RZRESULT_ALREADY_INITIALIZED: return "RZRESULT_ALREADY_INITIALIZED";
                case RZRESULT_RESOURCE_DISABLED: return "RZRESULT_RESOURCE_DISABLED";
                case RZRESULT_DEVICE_NOT_AVAILABLE: return "RZRESULT_DEVICE_NOT_AVAILABLE";
                case RZRESULT_NOT_VALID_STATE: return "RZRESULT_NOT_VALID_STATE";
                case RZRESULT_NO_MORE_ITEMS: return "RZRESULT_NO_MORE_ITEMS";
                case RZRESULT_DLL_NOT_FOUND: return "RZRESULT_DLL_NOT_FOUND";
                case RZRESULT_DLL_INVALID_SIGNATURE: return "RZRESULT_DLL_INVALID_SIGNATURE";
                case RZRESULT_FAILED: return "RZRESULT_FAILED";
            }
            return result.ToString();
        }
        #region VRCOSC Logger
        private static void Log(string message) // Shorten 
        {
            RyciaVRCOSC.InterhapticsModule.InterhapticsModule.ExternalLogger?.Invoke(message);
        }
        private static void LogDebug(string message) // Shorten 
        {
            RyciaVRCOSC.InterhapticsModule.InterhapticsModule.ExternalLoggerDebug?.Invoke(message);
        }
        #endregion
    }
}
