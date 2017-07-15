using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ImageProcessing
{
    class Program
    {
        private static int defaultHeight = 2400;
        private static int defaultWidth = 8000;
        static void Main(string[] args)
        {
           
           
            try {
                Image myImg = Image.FromFile("C:\\Users\\kkdhuiyu\\Desktop\\IMG_4464.jpg"); // the input image path.
                Image afterResize = ResizeImage(myImg, defaultWidth, defaultHeight);
                afterResize.Save("C:\\Users\\kkdhuiyu\\Desktop\\afterResizeIMG_4464.jpg"); // the output  image path.
            }
            catch(FileNotFoundException e)
            {
               Console.WriteLine("Please ensure your file location is valid");
            }



        }
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);
            if (image.Height > defaultHeight || image.Width > defaultWidth)
            {
                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                using (var graphics = Graphics.FromImage(destImage))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using (var wrapMode = new ImageAttributes())
                    {
                        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                        graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                    }
                }

                return destImage;
            }
            else
            {
                return new Bitmap(image);
            }
        }
       
    }
}
