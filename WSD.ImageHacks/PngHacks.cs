using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace WSD.ImageHacks
{
  public static class PngHacks
  {
    public static Image AsImage(this byte[] src)
    {
      using (var ms = new MemoryStream(src))
        return Image.FromStream(ms);
    }

    public static byte[] AsBytes(this Image src)
    {
      using (var ms = new MemoryStream())
      {
        src.Save(ms, ImageFormat.Png);
        src.Dispose();
        return ms.ToArray();
      }
    }

    public static byte[] ClipBottom(this byte[] src)
    {
      return src.AsImage().ClipBottom().AsBytes();
    }

    public static byte[] ClipBottomRemoveWhite(this byte[] src)
    {
      return src.AsImage().ClipBottom().RemoveWhite().AsBytes();
    }

    public static Bitmap ClipBottom(this Image src)
    {
      var cropRect = new Rectangle(0, 0, src.Width, src.Height - 16);
      var target = new Bitmap(cropRect.Width, cropRect.Height);

      using (var g = Graphics.FromImage(target))
      {
        g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height),
          cropRect,
          GraphicsUnit.Pixel);
      }
      return target;
    }

    public static Bitmap RemoveWhite(this Image src)
    {
      var target = new Bitmap(src);
      target.MakeTransparent(Color.White);
      return target;
    }
  }
}
