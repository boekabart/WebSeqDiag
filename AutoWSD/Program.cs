using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWSD
{
  class Program
  {
    private static void Main(string[] args)
    {
      WebSeqDiag.ApiHelpers.ImageConverter = WSD.ImageHacks.PngHacks.ClipBottomRemoveWhite;
      var folderOrFile = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("-")) ?? Directory.GetCurrentDirectory());
      var option = args.Any(s => s.Equals("-r")) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var files = File.Exists(folderOrFile)
            ? new[] {folderOrFile}
            : Directory.Exists(folderOrFile)
                ? Directory.EnumerateFiles(folderOrFile, "*.wsd", option)
                : new string[0];

        var tasks = files
          .Select(path => path.Replace(Directory.GetCurrentDirectory() + "\\", ""))
          .Select(path => WebSeqDiag.ApiHelpers.CreateOrUpdateImageAutoStyle(path, "png"))
          .ToArray();

      Task.WhenAll(tasks).Wait();
    }
  }
}
