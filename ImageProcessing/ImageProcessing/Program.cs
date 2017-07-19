using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ImageProcessing
{
    class Program
    {
        private static int defaultHeight = 500;
        private static int defaultWidth = 500;
        private static Image img;
        private static int[][] finalMeans;
        static void Main(string[] args)
        {
            Boolean hasImage = false;

            try
            {
                Image myImg = Image.FromFile("C:\\Users\\kkdhuiyu\\Desktop\\IMG_2408.jpg"); // the input image path.
                Image afterResize = ResizeImage(myImg, defaultWidth, defaultHeight);
                afterResize.Save("C:\\Users\\kkdhuiyu\\Desktop\\afterResizeIMG_4466.jpg"); // the output  image path.
                hasImage = true;
                img = myImg;
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("Please ensure your file location is valid");
            }
            if (hasImage)
            {

                int numClusters = 4;
                double[][] rawData = getDataFromImage();
                finalMeans = new int[numClusters][];

                Console.WriteLine("Setting numClusters to " + numClusters);

                int[] clustering = Cluster(rawData, numClusters);
                calculateFinalMean(rawData, clustering, numClusters);
                Console.WriteLine("K-means clustering complete");

                Console.WriteLine("Raw data by cluster:");
                ShowClustered(rawData, clustering, numClusters, 1);
                generateSegmImg(clustering);
                Console.WriteLine("Image generated");
                List<double[]> colorList = mostSignificantColors(rawData);
                for (int i = 0; i < colorList.Count && i < 10; i++)
                {
                    Console.WriteLine(String.Join(",", colorList.ElementAt(i).Select(p => p.ToString()).ToArray()));
                }
                Console.WriteLine("Number of diff colors: "+ colorList.Count);
                Console.ReadLine();
               

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
        private static int[] InitClustering(int numTuples, int numClusters, int seed)
        {
            Random random = new Random(seed);
            int[] clustering = new int[numTuples];
            for (int i = 0; i < numClusters; ++i)
                clustering[i] = i;
            for (int i = numClusters; i < clustering.Length; ++i)
                clustering[i] = random.Next(0, numClusters);
            return clustering;
        }

        public static int[] Cluster(double[][] rawData, int numClusters)
        {
            double[][] data = Normalized(rawData);
            bool changed = true; bool success = true;
            int[] clustering = InitClustering(data.Length, numClusters, 0);
            double[][] means = Allocate(numClusters, data[0].Length);
            int maxCount = data.Length * 10;
            int ct = 0;
            while (changed == true && success == true && ct < maxCount)
            {
                ++ct;
                success = UpdateMeans(data, clustering, means);
                changed = UpdateClustering(data, clustering, means);
            }

            return clustering;
        }
        private static double[][] Normalized(double[][] rawData)
        {
            double[][] result = new double[rawData.Length][];
            for (int i = 0; i < rawData.Length; ++i)
            {
                result[i] = new double[rawData[i].Length];
                Array.Copy(rawData[i], result[i], rawData[i].Length);
            }

            for (int j = 0; j < result[0].Length; ++j)
            {
                double colSum = 0.0;
                for (int i = 0; i < result.Length; ++i)
                    colSum += result[i][j];
                double mean = colSum / result.Length;
                double sum = 0.0;
                for (int i = 0; i < result.Length; ++i)
                    sum += (result[i][j] - mean) * (result[i][j] - mean);
                double sd = sum / result.Length;
                for (int i = 0; i < result.Length; ++i)
                    result[i][j] = (result[i][j] - mean) / sd;
            }
            return result;
        }

        private static bool UpdateMeans(double[][] data, int[] clustering, double[][] means)
        {
            int numClusters = means.Length;
            int[] clusterCounts = new int[numClusters];

            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false;

            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                {
                    means[k][j] = 0.0;
                }

            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                for (int j = 0; j < data[i].Length; ++j)
                    means[cluster][j] += data[i][j]; // accumulate sum
            }

            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                {
                    means[k][j] /= clusterCounts[k]; // danger of div by 0

                }
            return true;
        }
        private static bool UpdateClustering(double[][] data, int[] clustering, double[][] means)
        {
            int numClusters = means.Length;
            bool changed = false;

            int[] newClustering = new int[clustering.Length];
            Array.Copy(clustering, newClustering, clustering.Length);

            double[] distances = new double[numClusters];

            for (int i = 0; i < data.Length; ++i)
            {
                for (int k = 0; k < numClusters; ++k)
                    distances[k] = Distance(data[i], means[k]);

                int newClusterID = MinIndex(distances);
                if (newClusterID != newClustering[i])
                {
                    changed = true;
                    newClustering[i] = newClusterID;
                }
            }

            if (changed == false)
                return false;

            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = newClustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false;

            Array.Copy(newClustering, clustering, newClustering.Length);
            return true; // no zero-counts and at least one change
        }

        private static double Distance(double[] tuple, double[] mean)
        {
            double sumSquaredDiffs = 0.0;
            double deltaR = tuple[0] - mean[0];
            double deltaG = tuple[1] - mean[1];
            double deltaB = tuple[2] - mean[2];
            double avgR = (tuple[0] + mean[0]) / 2;
            sumSquaredDiffs += (2 + avgR / 256) * Math.Pow((deltaR), 2);
            sumSquaredDiffs += 4 * Math.Pow((deltaG), 2);
            sumSquaredDiffs += (2 + (255 - avgR)) * Math.Pow((deltaB), 2);
            // return getColorDiff(c1,c2);
            if (sumSquaredDiffs == 0)
            {
                return 0;
            }
            return Math.Sqrt(sumSquaredDiffs);
        }

        private static int MinIndex(double[] distances)
        {
            int indexOfMin = 0;
            double smallDist = distances[0];
            for (int k = 0; k < distances.Length; ++k)
            {
                if (distances[k] < smallDist)
                {
                    smallDist = distances[k];
                    indexOfMin = k;
                }
            }
            return indexOfMin;
        }
        static void ShowData(double[][] data, int decimals,
  bool indices, bool newLine)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                if (indices) Console.Write(i.ToString().PadLeft(3) + " ");
                for (int j = 0; j < data[i].Length; ++j)
                {
                    if (data[i][j] >= 0.0) Console.Write(" ");
                    Console.Write(data[i][j].ToString("F" + decimals) + " ");
                }
                Console.WriteLine("");
            }
            if (newLine) Console.WriteLine("");
        }

        static void ShowClustered(double[][] data, int[] clustering,
          int numClusters, int decimals)
        {
            for (int k = 0; k < numClusters; ++k)
            {
                Console.WriteLine("===================" + String.Join(",", finalMeans[k]));
                int printControl = 0;
                for (int i = 0; i < data.Length - 100; i++)
                {
                    int clusterID = clustering[i];
                    if (clusterID != k) continue;
                    Console.Write(i.ToString().PadLeft(3) + " ");
                    printControl++;
                    for (int j = 0; j < data[i].Length; j++)
                    {
                        if (data[i][j] >= 0.0) Console.Write(" ");
                        Console.Write(data[i][j].ToString("F" + decimals) + " ");


                    }
                    Console.WriteLine("");
                    if (printControl > 100)
                    {
                        i = data.Length + 1;
                    }
                }

                Console.WriteLine("===================");
            } // k
        }
        private static double[][] Allocate(int numClusters, int numColumns)
        {
            double[][] result = new double[numClusters][];
            for (int k = 0; k < numClusters; ++k)
                result[k] = new double[numColumns];
            return result;
        }
        private static double[][] getDataFromImage()
        {
            double[][] result = new double[img.Height * img.Width][];
            Bitmap bmp = new Bitmap(img);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            // Declare an array to hold the bytes of the bitmap.
            int bytes = bmpData.Stride * bmp.Height;
            byte[] rgbValues = new byte[bytes];
            byte[] r = new byte[bytes / 3];
            byte[] g = new byte[bytes / 3];
            byte[] b = new byte[bytes / 3];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, bytes);

            int count = 0;
            int stride = bmpData.Stride;

            for (int column = 0; column < bmpData.Height; column++)
            {
                for (int row = 0; row < bmpData.Width; row++)
                {
                    r[count] = (byte)(rgbValues[(column * stride) + (row * 3)]);
                    b[count] = (byte)(rgbValues[(column * stride) + (row * 3) + 1]);
                    g[count++] = (byte)(rgbValues[(column * stride) + (row * 3) + 2]);

                }
            }
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new double[] { r[i], b[i], g[i] };
            }
            return result;
        }
        private static void calculateFinalMean(double[][] data, int[] clustering, int numOfCluster)
        {
            for (int i = 0; i < numOfCluster; i++)
            {
                double r = 0, g = 0, b = 0;
                int count = 0;
                for (int j = 0; j < clustering.Length; j++)
                {

                    if (clustering[j] == i)
                    {
                        r += data[j][0];
                        g += data[j][1];
                        b += data[j][2];
                        count++;
                    }
                }
                finalMeans[i] = new int[] { (int)r / count, (int)g / count, (int)b / count };
            }
        }
        private static void generateSegmImg(int[] clustering)
        {
            int[][] segmImgArray = new int[clustering.Length][]; // this array contains rgb data for the output image
            byte[] imgData; // this is the byte array that's converted from the segmImgArray
            for (int i = 0; i < clustering.Length; i++)
            {
                segmImgArray[i] = finalMeans[clustering[i]];
            }
            int width, height;
            width = img.Width;
            height = img.Height;
            imgData = new byte[width * height * 3];
            for (int i = 0; i < clustering.Length; i++)// fill byte array with RGB values;
            {
                imgData[i * 3] = (byte)segmImgArray[i][0];
                imgData[i * 3 + 1] = (byte)segmImgArray[i][1];
                imgData[i * 3 + 2] = (byte)segmImgArray[i][2];
            }
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bmData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr pNative = bmData.Scan0;
            Marshal.Copy(imgData, 0, pNative, width * height * 3);
            bitmap.UnlockBits(bmData);
            Image segmImg = bitmap;
            segmImg.Save("C:\\Users\\kkdhuiyu\\Desktop\\segmIMG.jpg"); // the output  image path.
        }
        private static List<double[]> mostSignificantColors(double[][] data)
        {
            List<double[]> colorList = new List<double[]>();
            
            for (int i = 0; i < data.Length; i++)
            {
                if (colorList.Count == 0)
                {
                    colorList.Add(data[i]);
                }
                else
                {
                    foreach (double[] x in colorList)
                    {
                        double colorDiff=Distance(x, data[i]);
                        if (colorDiff > 1500)
                        {
                            colorList.Add(data[i]);
                            break;
                        }
                    }
                }
            }
            return colorList;
        }
      
    }
}
