using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using Microsoft.Win32;
using System.Net.Security;

namespace xNetStandard
{
    /// <summary>
    /// Представляет статический класс, предназначенный для помощи в работе с HTTP-протоколом.
    /// </summary>
    public static class Http
    {
        #region Константы (открытые)

        /// <summary>
        /// Обозначает новую строку в HTTP-протоколе.
        /// </summary>
        public const string NewLine = "\r\n";

        /// <summary>
        /// Метод делегата, который принимает все сертификаты SSL.
        /// </summary>
        public static readonly RemoteCertificateValidationCallback AcceptAllCertificationsCallback;

        #endregion


        #region Статические поля (внутренние)

        internal static readonly Dictionary<HttpHeader, string> Headers = new Dictionary<HttpHeader, string>()
        {
            { HttpHeader.Accept, "Accept" },
            { HttpHeader.AcceptCharset, "Accept-Charset" },
            { HttpHeader.AcceptLanguage, "Accept-Language" },
            { HttpHeader.AcceptDatetime, "Accept-Datetime" },
            { HttpHeader.CacheControl, "Cache-Control" },
            { HttpHeader.ContentType, "Content-Type" },
            { HttpHeader.Date, "Date" },
            { HttpHeader.Expect, "Expect" },
            { HttpHeader.From, "From" },
            { HttpHeader.IfMatch, "If-Match" },
            { HttpHeader.IfModifiedSince, "If-Modified-Since" },
            { HttpHeader.IfNoneMatch, "If-None-Match" },
            { HttpHeader.IfRange, "If-Range" },
            { HttpHeader.IfUnmodifiedSince, "If-Unmodified-Since" },
            { HttpHeader.MaxForwards, "Max-Forwards" },
            { HttpHeader.Pragma, "Pragma" },
            { HttpHeader.Range, "Range" },
            { HttpHeader.Referer, "Referer" },
            { HttpHeader.Upgrade, "Upgrade" },
            { HttpHeader.UserAgent, "User-Agent" },
            { HttpHeader.Via, "Via" },
            { HttpHeader.Warning, "Warning" },
            { HttpHeader.DNT, "DNT" },
            { HttpHeader.AccessControlAllowOrigin, "Access-Control-Allow-Origin" },
            { HttpHeader.AcceptRanges, "Accept-Ranges" },
            { HttpHeader.Age, "Age" },
            { HttpHeader.Allow, "Allow" },
            { HttpHeader.ContentEncoding, "Content-Encoding" },
            { HttpHeader.ContentLanguage, "Content-Language" },
            { HttpHeader.ContentLength, "Content-Length" },
            { HttpHeader.ContentLocation, "Content-Location" },
            { HttpHeader.ContentMD5, "Content-MD5" },
            { HttpHeader.ContentDisposition, "Content-Disposition" },
            { HttpHeader.ContentRange, "Content-Range" },
            { HttpHeader.ETag, "ETag" },
            { HttpHeader.Expires, "Expires" },
            { HttpHeader.LastModified, "Last-Modified" },
            { HttpHeader.Link, "Link" },
            { HttpHeader.Location, "Location" },
            { HttpHeader.P3P, "P3P" },
            { HttpHeader.Refresh, "Refresh" },
            { HttpHeader.RetryAfter, "Retry-After" },
            { HttpHeader.Server, "Server" },
            { HttpHeader.TransferEncoding, "Transfer-Encoding" }
        };

        #endregion


        #region Статические поля (закрытые)

        [ThreadStatic] private static Random _rand;
        private static Random Rand
        {
            get
            {
                if (_rand == null)
                    _rand = new Random();
                return _rand;
            }
        }

        #endregion


        static Http()
        {
            AcceptAllCertificationsCallback = new RemoteCertificateValidationCallback(AcceptAllCertifications);
        }


        #region Статические методы (открытые)

        /// <summary>
        /// Кодирует строку для надёжной передачи HTTP-серверу.
        /// </summary>
        /// <param name="str">Строка, которая будет закодирована.</param>
        /// <param name="encoding">Кодировка, применяемая для преобразования данных в последовательность байтов. Если значение параметра равно <see langword="null"/>, то будет использовано значение <see cref="System.Text.Encoding.UTF8"/>.</param>
        /// <returns>Закодированная строка.</returns>
        public static string UrlEncode(string str, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            encoding = encoding ?? Encoding.UTF8;

            byte[] bytes = encoding.GetBytes(str);

            int spaceCount = 0;
            int otherCharCount = 0;

            #region Подсчёт символов

            for (int i = 0; i < bytes.Length; i++)
            {
                char c = (char)bytes[i];

                if (c == ' ')
                {
                    ++spaceCount;
                }
                else if (!IsUrlSafeChar(c))
                {
                    ++otherCharCount;
                }
            }

            #endregion

            // Если в строке не присутствуют символы, которые нужно закодировать.
            if ((spaceCount == 0) && (otherCharCount == 0))
            {
                return str;
            }

            int bufferIndex = 0;
            byte[] buffer = new byte[bytes.Length + (otherCharCount * 2)];

            for (int i = 0; i < bytes.Length; i++)
            {
                char c = (char)bytes[i];

                if (IsUrlSafeChar(c))
                {
                    buffer[bufferIndex++] = bytes[i];
                }
                else if (c == ' ')
                {
                    buffer[bufferIndex++] = (byte)'+';
                }
                else
                {
                    buffer[bufferIndex++] = (byte)'%';
                    buffer[bufferIndex++] = (byte)IntToHex((bytes[i] >> 4) & 15);
                    buffer[bufferIndex++] = (byte)IntToHex(bytes[i] & 15);
                }
            }

            return Encoding.ASCII.GetString(buffer);
        }

        /// <summary>
        /// Преобразует параметры в строку запроса.
        /// </summary>
        /// <param name="parameters">Параметры.</param>
        /// <param name="dontEscape">Указывает, нужно ли кодировать значения параметров.</param>
        /// <returns>Строка запроса.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="parameters"/> равно <see langword="null"/>.</exception>
        public static string ToQueryString(IEnumerable<KeyValuePair<string, string>> parameters, bool dontEscape)
        {
            #region Проверка параметров

            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            #endregion

            var queryBuilder = new StringBuilder();

            foreach (var param in parameters)
            {
                if (string.IsNullOrEmpty(param.Key))
                {
                    continue;
                }

                queryBuilder.Append(param.Key);
                queryBuilder.Append('=');

                if (dontEscape)
                {
                    queryBuilder.Append(param.Value);
                }
                else
                {
                    queryBuilder.Append(
                        Uri.EscapeDataString(param.Value ?? string.Empty));
                }

                queryBuilder.Append('&');
            }

            if (queryBuilder.Length != 0)
            {
                // Удаляем '&' в конце.
                queryBuilder.Remove(queryBuilder.Length - 1, 1);
            }

            return queryBuilder.ToString();
        }

        /// <summary>
        /// Преобразует параметры в строку POST-запроса.
        /// </summary>
        /// <param name="parameters">Параметры.</param>
        /// <param name="dontEscape">Указывает, нужно ли кодировать значения параметров.</param>
        /// <param name="encoding">Кодировка, применяемая для преобразования параметров запроса. Если значение параметра равно <see langword="null"/>, то будет использовано значение <see cref="System.Text.Encoding.UTF8"/>.</param>
        /// <returns>Строка запроса.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="parameters"/> равно <see langword="null"/>.</exception>
        public static string ToPostQueryString(IEnumerable<KeyValuePair<string, string>> parameters, bool dontEscape, Encoding encoding = null)
        {
            #region Проверка параметров

            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            #endregion

            var queryBuilder = new StringBuilder();

            foreach (var param in parameters)
            {
                if (string.IsNullOrEmpty(param.Key))
                {
                    continue;
                }

                queryBuilder.Append(param.Key);
                queryBuilder.Append('=');

                if (dontEscape)
                {
                    queryBuilder.Append(param.Value);
                }
                else
                {
                    queryBuilder.Append(
                        UrlEncode(param.Value ?? string.Empty, encoding));
                }

                queryBuilder.Append('&');
            }

            if (queryBuilder.Length != 0)
            {
                // Удаляем '&' в конце.
                queryBuilder.Remove(queryBuilder.Length - 1, 1);
            }

            return queryBuilder.ToString();
        }
        private static readonly Dictionary<string, string> MIMETypesDictionary = new Dictionary<string, string>
  {
    {"ai", "application/postscript"},
    {"aif", "audio/x-aiff"},
    {"aifc", "audio/x-aiff"},
    {"aiff", "audio/x-aiff"},
    {"asc", "text/plain"},
    {"atom", "application/atom+xml"},
    {"au", "audio/basic"},
    {"avi", "video/x-msvideo"},
    {"bcpio", "application/x-bcpio"},
    {"bin", "application/octet-stream"},
    {"bmp", "image/bmp"},
    {"cdf", "application/x-netcdf"},
    {"cgm", "image/cgm"},
    {"class", "application/octet-stream"},
    {"cpio", "application/x-cpio"},
    {"cpt", "application/mac-compactpro"},
    {"csh", "application/x-csh"},
    {"css", "text/css"},
    {"dcr", "application/x-director"},
    {"dif", "video/x-dv"},
    {"dir", "application/x-director"},
    {"djv", "image/vnd.djvu"},
    {"djvu", "image/vnd.djvu"},
    {"dll", "application/octet-stream"},
    {"dmg", "application/octet-stream"},
    {"dms", "application/octet-stream"},
    {"doc", "application/msword"},
    {"docx","application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
    {"dotx", "application/vnd.openxmlformats-officedocument.wordprocessingml.template"},
    {"docm","application/vnd.ms-word.document.macroEnabled.12"},
    {"dotm","application/vnd.ms-word.template.macroEnabled.12"},
    {"dtd", "application/xml-dtd"},
    {"dv", "video/x-dv"},
    {"dvi", "application/x-dvi"},
    {"dxr", "application/x-director"},
    {"eps", "application/postscript"},
    {"etx", "text/x-setext"},
    {"exe", "application/octet-stream"},
    {"ez", "application/andrew-inset"},
    {"gif", "image/gif"},
    {"gram", "application/srgs"},
    {"grxml", "application/srgs+xml"},
    {"gtar", "application/x-gtar"},
    {"hdf", "application/x-hdf"},
    {"hqx", "application/mac-binhex40"},
    {"htm", "text/html"},
    {"html", "text/html"},
    {"ice", "x-conference/x-cooltalk"},
    {"ico", "image/x-icon"},
    {"ics", "text/calendar"},
    {"ief", "image/ief"},
    {"ifb", "text/calendar"},
    {"iges", "model/iges"},
    {"igs", "model/iges"},
    {"jnlp", "application/x-java-jnlp-file"},
    {"jp2", "image/jp2"},
    {"jpe", "image/jpeg"},
    {"jpeg", "image/jpeg"},
    {"jpg", "image/jpeg"},
    {"js", "application/x-javascript"},
    {"kar", "audio/midi"},
    {"latex", "application/x-latex"},
    {"lha", "application/octet-stream"},
    {"lzh", "application/octet-stream"},
    {"m3u", "audio/x-mpegurl"},
    {"m4a", "audio/mp4a-latm"},
    {"m4b", "audio/mp4a-latm"},
    {"m4p", "audio/mp4a-latm"},
    {"m4u", "video/vnd.mpegurl"},
    {"m4v", "video/x-m4v"},
    {"mac", "image/x-macpaint"},
    {"man", "application/x-troff-man"},
    {"mathml", "application/mathml+xml"},
    {"me", "application/x-troff-me"},
    {"mesh", "model/mesh"},
    {"mid", "audio/midi"},
    {"midi", "audio/midi"},
    {"mif", "application/vnd.mif"},
    {"mov", "video/quicktime"},
    {"movie", "video/x-sgi-movie"},
    {"mp2", "audio/mpeg"},
    {"mp3", "audio/mpeg"},
    {"mp4", "video/mp4"},
    {"mpe", "video/mpeg"},
    {"mpeg", "video/mpeg"},
    {"mpg", "video/mpeg"},
    {"mpga", "audio/mpeg"},
    {"ms", "application/x-troff-ms"},
    {"msh", "model/mesh"},
    {"mxu", "video/vnd.mpegurl"},
    {"nc", "application/x-netcdf"},
    {"oda", "application/oda"},
    {"ogg", "application/ogg"},
    {"pbm", "image/x-portable-bitmap"},
    {"pct", "image/pict"},
    {"pdb", "chemical/x-pdb"},
    {"pdf", "application/pdf"},
    {"pgm", "image/x-portable-graymap"},
    {"pgn", "application/x-chess-pgn"},
    {"pic", "image/pict"},
    {"pict", "image/pict"},
    {"png", "image/png"},
    {"pnm", "image/x-portable-anymap"},
    {"pnt", "image/x-macpaint"},
    {"pntg", "image/x-macpaint"},
    {"ppm", "image/x-portable-pixmap"},
    {"ppt", "application/vnd.ms-powerpoint"},
    {"pptx","application/vnd.openxmlformats-officedocument.presentationml.presentation"},
    {"potx","application/vnd.openxmlformats-officedocument.presentationml.template"},
    {"ppsx","application/vnd.openxmlformats-officedocument.presentationml.slideshow"},
    {"ppam","application/vnd.ms-powerpoint.addin.macroEnabled.12"},
    {"pptm","application/vnd.ms-powerpoint.presentation.macroEnabled.12"},
    {"potm","application/vnd.ms-powerpoint.template.macroEnabled.12"},
    {"ppsm","application/vnd.ms-powerpoint.slideshow.macroEnabled.12"},
    {"ps", "application/postscript"},
    {"qt", "video/quicktime"},
    {"qti", "image/x-quicktime"},
    {"qtif", "image/x-quicktime"},
    {"ra", "audio/x-pn-realaudio"},
    {"ram", "audio/x-pn-realaudio"},
    {"ras", "image/x-cmu-raster"},
    {"rdf", "application/rdf+xml"},
    {"rgb", "image/x-rgb"},
    {"rm", "application/vnd.rn-realmedia"},
    {"roff", "application/x-troff"},
    {"rtf", "text/rtf"},
    {"rtx", "text/richtext"},
    {"sgm", "text/sgml"},
    {"sgml", "text/sgml"},
    {"sh", "application/x-sh"},
    {"shar", "application/x-shar"},
    {"silo", "model/mesh"},
    {"sit", "application/x-stuffit"},
    {"skd", "application/x-koan"},
    {"skm", "application/x-koan"},
    {"skp", "application/x-koan"},
    {"skt", "application/x-koan"},
    {"smi", "application/smil"},
    {"smil", "application/smil"},
    {"snd", "audio/basic"},
    {"so", "application/octet-stream"},
    {"spl", "application/x-futuresplash"},
    {"src", "application/x-wais-source"},
    {"sv4cpio", "application/x-sv4cpio"},
    {"sv4crc", "application/x-sv4crc"},
    {"svg", "image/svg+xml"},
    {"swf", "application/x-shockwave-flash"},
    {"t", "application/x-troff"},
    {"tar", "application/x-tar"},
    {"tcl", "application/x-tcl"},
    {"tex", "application/x-tex"},
    {"texi", "application/x-texinfo"},
    {"texinfo", "application/x-texinfo"},
    {"tif", "image/tiff"},
    {"tiff", "image/tiff"},
    {"tr", "application/x-troff"},
    {"tsv", "text/tab-separated-values"},
    {"txt", "text/plain"},
    {"ustar", "application/x-ustar"},
    {"vcd", "application/x-cdlink"},
    {"vrml", "model/vrml"},
    {"vxml", "application/voicexml+xml"},
    {"wav", "audio/x-wav"},
    {"wbmp", "image/vnd.wap.wbmp"},
    {"wbmxl", "application/vnd.wap.wbxml"},
    {"wml", "text/vnd.wap.wml"},
    {"wmlc", "application/vnd.wap.wmlc"},
    {"wmls", "text/vnd.wap.wmlscript"},
    {"wmlsc", "application/vnd.wap.wmlscriptc"},
    {"wrl", "model/vrml"},
    {"xbm", "image/x-xbitmap"},
    {"xht", "application/xhtml+xml"},
    {"xhtml", "application/xhtml+xml"},
    {"xls", "application/vnd.ms-excel"},
    {"xml", "application/xml"},
    {"xpm", "image/x-xpixmap"},
    {"xsl", "application/xml"},
    {"xlsx","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
    {"xltx","application/vnd.openxmlformats-officedocument.spreadsheetml.template"},
    {"xlsm","application/vnd.ms-excel.sheet.macroEnabled.12"},
    {"xltm","application/vnd.ms-excel.template.macroEnabled.12"},
    {"xlam","application/vnd.ms-excel.addin.macroEnabled.12"},
    {"xlsb","application/vnd.ms-excel.sheet.binary.macroEnabled.12"},
    {"xslt", "application/xslt+xml"},
    {"xul", "application/vnd.mozilla.xul+xml"},
    {"xwd", "image/x-xwindowdump"},
    {"xyz", "chemical/x-xyz"},
    {"zip", "application/zip"}
  };
        /// <summary>
        /// Определяет и возвращает MIME-тип на основе расширения файла.
        /// </summary>
        /// <param name="extension">Расширение файла.</param>
        /// <returns>MIME-тип.</returns>
        public static string DetermineMediaType(string extension)
        {
            extension = extension.ToLowerInvariant();
            if (extension.StartsWith("."))
            {
                extension = extension.Remove(0, 1);
            }
            string mediaType = "application/octet-stream";
            if (extension.Length > 0 &&
                                        MIMETypesDictionary.ContainsKey(extension))
            {
                return MIMETypesDictionary[extension];
            }
            /* try
             {
                 using (var regKey = Registry.ClassesRoot.OpenSubKey(extension))
                 {
                     if (regKey != null)
                     {
                         object keyValue = regKey.GetValue("Content Type");

                         if (keyValue != null)
                         {
                             mediaType = keyValue.ToString();
                         }
                     }
                 }
             }
             #region Catch's

             catch (IOException) { }
             catch (ObjectDisposedException) { }
             catch (UnauthorizedAccessException) { }
             catch (SecurityException) { }

             #endregion
             */
            return mediaType;
        }
        
        #region User Agent

        /// <summary>
        /// Генерирует случайный User-Agent от браузера IE.
        /// </summary>
        /// <returns>Случайный User-Agent от браузера IE.</returns>
        public static string IEUserAgent()
        {
            string windowsVersion = RandomWindowsVersion();

            string version = null;
            string mozillaVersion = null;
            string trident = null;
            string otherParams = null;

            #region Генерация случайной версии

            if (windowsVersion.Contains("NT 5.1"))
            {
                version = "9.0";
                mozillaVersion = "5.0";
                trident = "5.0";
                otherParams = ".NET CLR 2.0.50727; .NET CLR 3.5.30729";
            }
            else if (windowsVersion.Contains("NT 6.0"))
            {
                version = "9.0";
                mozillaVersion = "5.0";
                trident = "5.0";
                otherParams = ".NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.5.30729";
            }
            else
            {
                switch (Rand.Next(3))
                {
                    case 0:
                        version = "10.0";
                        trident = "6.0";
                        mozillaVersion = "5.0";
                        break;

                    case 1:
                        version = "10.6";
                        trident = "6.0";
                        mozillaVersion = "5.0";
                        break;

                    case 2:
                        version = "11.0";
                        trident = "7.0";
                        mozillaVersion = "5.0";
                        break;
                }

                otherParams = ".NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; .NET4.0C; .NET4.0E";
            }

            #endregion

            return string.Format(
                "Mozilla/{0} (compatible; MSIE {1}; {2}; Trident/{3}; {4})",
                mozillaVersion, version, windowsVersion, trident, otherParams);
        }

        /// <summary>
        /// Генерирует случайный User-Agent от браузера Opera.
        /// </summary>
        /// <returns>Случайный User-Agent от браузера Opera.</returns>
        public static string OperaUserAgent()
        {
            string version = null;
            string presto = null;

            #region Генерация случайной версии

            switch (Rand.Next(4))
            {
                case 0:
                    version = "12.16";
                    presto = "2.12.388";
                    break;

                case 1:
                    version = "12.14";
                    presto = "2.12.388";
                    break;

                case 2:
                    version = "12.02";
                    presto = "2.10.289";
                    break;

                case 3:
                    version = "12.00";
                    presto = "2.10.181";
                    break;
            }

            #endregion

            return string.Format(
                "Opera/9.80 ({0}); U) Presto/{1} Version/{2}",
                RandomWindowsVersion(), presto, version);
        }

        /// <summary>
        /// Генерирует случайный User-Agent от браузера Chrome.
        /// </summary>
        /// <returns>Случайный User-Agent от браузера Chrome.</returns>
        public static string ChromeUserAgent()
        {
            string version = null;
            string safari = null;

            #region Генерация случайной версии

            switch (Rand.Next(5))
            {
                case 0:
                    version = "41.0.2228.0";
                    safari = "537.36";
                    break;

                case 1:
                    version = "41.0.2227.1";
                    safari = "537.36";
                    break;

                case 2:
                    version = "41.0.2224.3";
                    safari = "537.36";
                    break;

                case 3:
                    version = "41.0.2225.0";
                    safari = "537.36";
                    break;

                case 4:
                    version = "41.0.2226.0";
                    safari = "537.36";
                    break;
            }

            #endregion

            return string.Format(
                "Mozilla/5.0 ({0}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{1} Safari/{2}",
                RandomWindowsVersion(), version, safari);
        }

        /// <summary>
        /// Генерирует случайный User-Agent от браузера Firefox.
        /// </summary>
        /// <returns>Случайный User-Agent от браузера Firefox.</returns>
        public static string FirefoxUserAgent()
        {
            string gecko = null;
            string version = null;

            #region Генерация случайной версии

            switch (Rand.Next(5))
            {
                case 0:
                    version = "36.0";
                    gecko = "20100101";
                    break;

                case 1:
                    version = "33.0";
                    gecko = "20100101";
                    break;

                case 2:
                    version = "31.0";
                    gecko = "20100101";
                    break;

                case 3:
                    version = "29.0";
                    gecko = "20120101";
                    break;

                case 4:
                    version = "28.0";
                    gecko = "20100101";
                    break;
            }

            #endregion

            return string.Format(
                "Mozilla/5.0 ({0}; rv:{1}) Gecko/{2} Firefox/{1}",
                RandomWindowsVersion(), version, gecko);
        }

        /// <summary>
        /// Генерирует случайный User-Agent от мобильного браузера Opera.
        /// </summary>
        /// <returns>Случайный User-Agent от мобильного браузера Opera.</returns>
        public static string OperaMiniUserAgent()
        {
            string os = null;
            string miniVersion = null;
            string version = null;
            string presto = null;

            #region Генерация случайной версии

            switch (Rand.Next(3))
            {
                case 0:
                    os = "iOS";
                    miniVersion = "7.0.73345";
                    version = "11.62";
                    presto = "2.10.229";
                    break;

                case 1:
                    os = "J2ME/MIDP";
                    miniVersion = "7.1.23511";
                    version = "12.00";
                    presto = "2.10.181";
                    break;

                case 2:
                    os = "Android";
                    miniVersion = "7.5.54678";
                    version = "12.02";
                    presto = "2.10.289";
                    break;
            }

            #endregion

            return string.Format(
                "Opera/9.80 ({0}; Opera Mini/{1}/28.2555; U; ru) Presto/{2} Version/{3}",
                os, miniVersion, presto, version);
        }

        #endregion

        #endregion


        #region Статические методы (закрытые)

        private static bool AcceptAllCertifications(object sender,
            System.Security.Cryptography.X509Certificates.X509Certificate certification,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static bool IsUrlSafeChar(char c)
        {
            if ((((c >= 'a') && (c <= 'z')) ||
                ((c >= 'A') && (c <= 'Z'))) ||
                ((c >= '0') && (c <= '9')))
            {
                return true;
            }

            switch (c)
            {
                case '(':
                case ')':
                case '*':
                case '-':
                case '.':
                case '_':
                case '!':
                    return true;
            }

            return false;
        }

        private static char IntToHex(int i)
        {
            if (i <= 9)
            {
                return (char)(i + 48);
            }

            return (char)((i - 10) + 65);
        }

        private static string RandomWindowsVersion()
        {
            string windowsVersion = "Windows NT ";

            switch (Rand.Next(4))
            {
                case 0:
                    windowsVersion += "5.1"; // Windows XP
                    break;

                case 1:
                    windowsVersion += "6.0"; // Windows Vista
                    break;

                case 2:
                    windowsVersion += "6.1"; // Windows 7
                    break;

                case 3:
                    windowsVersion += "6.2"; // Windows 8
                    break;
            }

            if (Rand.NextDouble() < 0.2)
            {
                windowsVersion += "; WOW64"; // 64-битная версия.
            }

            return windowsVersion;
        }

        #endregion
    }
}