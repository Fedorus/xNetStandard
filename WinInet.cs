using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace xNetStandart
{
    /// <summary>
    /// Представляет класс для взаимодействия с настройками сети операционной системы Windows.
    /// </summary>
    public static class WinInet
    {
        #region Статические свойства (открытые)

        /// <summary>
        /// Возвращает значение, указывающие, установлено ли подключение к интернету.
        /// </summary>
        public static bool InternetConnected
        {
            get
            {
                SafeNativeMethods.InternetConnectionState state = 0;
                return SafeNativeMethods.InternetGetConnectedState(ref state, 0);
            }
        }

        /// <summary>
        /// Возвращает значение, указывающие, установлено ли подключение к интернету через модем.
        /// </summary>
        public static bool InternetThroughModem
        {
            get
            {
                return EqualConnectedState(
                    SafeNativeMethods.InternetConnectionState.INTERNET_CONNECTION_MODEM);
            }
        }

        /// <summary>
        /// Возвращает значение, указывающие, установлено ли подключение к интернету через локальную сеть.
        /// </summary>
        public static bool InternetThroughLan
        {
            get
            {
                return EqualConnectedState(
                    SafeNativeMethods.InternetConnectionState.INTERNET_CONNECTION_LAN);
            }
        }

        /// <summary>
        /// Возвращает значение, указывающие, установлено ли подключение к интернету через прокси-сервер.
        /// </summary>
        public static bool InternetThroughProxy
        {
            get
            {
                return EqualConnectedState(
                    SafeNativeMethods.InternetConnectionState.INTERNET_CONNECTION_PROXY);
            }
        }

        /// <summary>
        /// Возвращает или задаёт прокси-сервер Internet Explorer'а.
        /// </summary>
        /// <value>Если прокси-сервер Internet Explorer'а не задан или ошибочен, то будет возвращён <see langword="null"/>. Если задать <see langword="null"/>, то прокси-сервер Internet Explorer'а будет стёрт.</value>
        public static HttpProxyClient IEProxy
        {
            get
            {
                return null ;
            }
            set
            {
                
            }
        }

        #endregion



        private static bool EqualConnectedState(SafeNativeMethods.InternetConnectionState expected)
        {
            SafeNativeMethods.InternetConnectionState state = 0;
            SafeNativeMethods.InternetGetConnectedState(ref state, 0);

            return (state & expected) != 0;
        }
    }
}