using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebSeqDiag
{
    public static class ApiHelpers
    {
        public static Func<byte[], byte[]> ImageConverter;

        public static byte[] ConvertImage(this byte[] src)
        {
            if (ImageConverter == null)
                return src;
            return ImageConverter(src);
        }

        /// <summary>
        ///     Updates a file with new contents only if they differ
        /// </summary>
        /// <param name="path">full or relative path to the file</param>
        /// <param name="newContents">the new file contents</param>
        /// <returns>Whether the file needed updating</returns>
        public static bool UpdateFileIfChanged(this string path, byte[] newContents)
        {
            byte[] old = File.ReadAllBytes(path);
            if (old.Equals(newContents))
            {
                Console.WriteLine("Not updating identical file {0}", path);
                return false;
            }

            Console.WriteLine("Updating file {0}", path);
            File.WriteAllBytes(path, newContents);
            return true;
        }

        public static bool CreateOrUpdateFileIfChanged(this string path, byte[] newContents)
        {
            if (File.Exists(path))
                return path.UpdateFileIfChanged(newContents);
            Console.WriteLine("Creating new file {0}", path);
            File.WriteAllBytes(path, newContents);
            return true;
        }

        public static bool CreateOrUpdateImageWithConversion(this string path, byte[] newContents)
        {
            Console.WriteLine("Improving future contents of {0}", path);
            byte[] convertedImage = newContents.ConvertImage();
            return CreateOrUpdateFileIfChanged(path, convertedImage);
        }

        public static IEnumerable<T> EnumerateEnumValues<T>()
        {
            return Enum.GetValues(typeof (T))
                .Cast<T>();
        }

        public static async Task<bool> CreateOrUpdateImageForEachStyle(string wsdPath, string fileFormat)
        {
            IEnumerable<Task<bool>> tasks = EnumerateEnumValues<Styles>()
                .Select<Styles, Task<bool>>(st => CreateOrUpdateImageWithStyleInPath(wsdPath, st, fileFormat));
            bool[] results = await Task.WhenAll(tasks);
            return results.Any(bo => bo);
        }

        public static Task<bool> CreateOrUpdateImageAutoStyle(string wsdPath, string fileFormat)
        {
            Styles? style = ReadStyleFromWsd(wsdPath);
            return style.HasValue
                ? CreateOrUpdateImage(wsdPath, fileFormat, style.Value)
                : CreateOrUpdateImageForEachStyle(wsdPath, fileFormat);
        }

        public static T? TryParseEnum<T>(this string value, bool ignoreCase)
            where T : struct
        {
            T val;
            if (Enum.TryParse(value, ignoreCase, out val))
                return val;
            return null;
        }

        public static Styles? ReadStyleFromWsd(string wsdPath)
        {
            string[] wsdLines = File.ReadAllLines(wsdPath);
            return ReadStyleFromWsdLines(wsdLines);
        }

        public static Styles? ReadStyleFromWsdLines(IEnumerable<string> wsdLines)
        {
            return wsdLines
                .Where(l => l.StartsWith("#style", StringComparison.InvariantCultureIgnoreCase))
                .Select(l => l.Split(new[] {' ', '='}, StringSplitOptions.RemoveEmptyEntries))
                .Where(arr => arr.Length > 1)
                .Select(arr => arr[1])
                .Select(styleString => styleString.TryParseEnum<Styles>(true))
                .FirstOrDefault(e => e.HasValue);
        }

        public static Task<bool> CreateOrUpdateImageWithStyleInPath(string wsdPath, Styles style, string fileFormat)
        {
            string imgPath = Path.ChangeExtension(wsdPath, style + "." + fileFormat);
            return CreateOrUpdateImage(wsdPath, style, imgPath);
        }

        public static Task<bool> CreateOrUpdateImage(string wsdPath, Styles style, string imgPath)
        {
            string fileFormat = Path.GetExtension(imgPath).Substring(1);
            string wsd = File.ReadAllText(wsdPath);
            return UpdateImageForWsd(fileFormat, style, wsd, imgPath);
        }

        public static Task<bool> CreateOrUpdateImage(string wsdPath, string fileFormat = "png",
            Styles style = Styles.Default)
        {
            string imgPath = Path.ChangeExtension(wsdPath, "." + fileFormat);
            string wsd = File.ReadAllText(wsdPath);
            return UpdateImageForWsd(fileFormat, style, wsd, imgPath);
        }

        private static async Task<bool> UpdateImageForWsd(string fileFormat, Styles style, string wsd, string imgPath)
        {
            byte[] imgBytes = await ApiClient.GrabSequenceDiagram(wsd, style, fileFormat);
            return imgPath.CreateOrUpdateImageWithConversion(imgBytes);
        }

        public static string ApiString(this Styles style)
        {
            return style.ToString().ToLowerInvariant();
        }
    }

    public enum Styles
    {
        Default,
        Earth,
        Magazine,
        ModernBlue,
        Mscgen,
        Napkin,
        Omegapple,
        Patent,
        Qsd,
        Rose,
        Roundgreen,
    };

    public static class ApiClient
    {
        /// <summary>
        ///     Given a WSD description, produces a sequence diagram PNG.
        /// </summary>
        /// This method uses the WebSequenceDiagrams.com public API to query an image and stored in a local
        /// temporary directory on the file system.
        /// 
        /// You can easily change it to return the stream to the image requested instead of a file.
        /// 
        /// To invoke it:
        /// ..
        /// using System.Web;
        /// ...
        ///   
        /// string fileName = grabSequenceDiagram("a->b: Hello", "qsd", "png");
        /// ..
        ///   
        /// You need to add the assembly "System.Web" to your reference list (that by default is not
        /// added to new projects)
        /// 
        /// Questions / suggestions: fabriziobertocci@gmail.com
        /// <param name="wsd">The web sequence diagram description text</param>
        /// <param name="style">One of the valid styles for the diagram</param>
        /// <param name="fileFormat">The output format requested. Must be one of the valid format supported</param>
        /// <returns>The full path of the downloaded image</returns>
        /// <exception cref="Exception">If an error occurred during the request</exception>
        public static Task<byte[]> GrabSequenceDiagram(String wsd, Styles style, String fileFormat)
        {
            return GrabSequenceDiagram(wsd, style.ApiString(), fileFormat);
        }

        public static async Task<byte[]> GrabSequenceDiagram(String wsd, String style, String fileFormat)
        {
            // Websequence diagram API:
            // prepare a POST body containing the required properties
            var sb = new StringBuilder("style=");
            sb.Append(style).Append("&apiVersion=1&format=").Append(fileFormat).Append("&message=");
            sb.Append(HttpUtility.UrlEncode(wsd));
            byte[] postBytes = Encoding.ASCII.GetBytes(sb.ToString());

            // Typical Microsoft crap here: the HttpWebRequest by default always append the header
            //          "Expect: 100-Continue"
            // to every request. Some web servers (including www.websequencediagrams.com) chockes on that
            // and respond with a 417 error.
            // Disable it permanently:
            ServicePointManager.Expect100Continue = false;

            // set up request object
            HttpWebRequest request;
            // The following command might throw UriFormatException
            request =
                WebRequest.Create("http://www.websequencediagrams.com/index.php") as HttpWebRequest;

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postBytes.Length;

            // add post data to request
            Stream postStream = await request.GetRequestStreamAsync();
            postStream.Write(postBytes, 0, postBytes.Length);
            postStream.Close();

            var response = await request.GetResponseAsync() as HttpWebResponse;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Unexpected HTTP status from server: " + response.StatusCode + ": " +
                                    response.StatusDescription);
            }

            var stream = new StreamReader(response.GetResponseStream());
            String jsonObject = stream.ReadToEnd();
            stream.Close();

            // Expect response like this one: {"img": "?png=mscKTO107", "errors": []}
            // Instead of using a full JSON parser, do a simple parsing of the response
            String[] components = jsonObject.Split('"');
            // Ensure component #1 is 'img':
            if (components[1].Equals("img") == false)
            {
                throw new Exception("Error parsing response from server: " + jsonObject);
            }

            String uri = components[3];

            // Now download the image
            request = WebRequest.Create("http://www.websequencediagrams.com/" + uri) as HttpWebRequest;
            request.Method = "GET";

            response = await request.GetResponseAsync() as HttpWebResponse;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Server reported HTTP error during image fetch: " + response.StatusCode + ": " +
                                    response.StatusDescription);
            }
            try
            {
                using (Stream srcStream = response.GetResponseStream())
                using (var dstStream = new MemoryStream((int) response.ContentLength))
                {
                    await srcStream.CopyToAsync(dstStream);
                    return dstStream.ToArray();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Exception while reading response: " + e.Message, e);
            }
        }
    }
}
