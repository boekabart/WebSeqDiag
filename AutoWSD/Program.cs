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
      var folder = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("-")) ?? Directory.GetCurrentDirectory());
      var option = args.Any(s => s.Equals("-r")) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
      var tasks =
        Directory.EnumerateFiles(folder, "*.wsd", option)
          .Select(path => path.Replace(Directory.GetCurrentDirectory() + "\\", ""))
          .Select(path => WebSeqDiag.ApiHelpers.CreateOrUpdateImageForEachStyle(path, "png"))
          .ToArray();
      Task.WhenAll(tasks).Wait();
    }
  }
}
