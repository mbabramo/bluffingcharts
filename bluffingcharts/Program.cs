using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace bluffingcharts
{
    class Program
    {
        static void BarFill(Graphics g, Rectangle r, bool individualSetHorizontally, double[][] proportions)
        {
            Rectangle[] rectangles = DivideRectangle(r, !individualSetHorizontally, proportions.Length, 2);
            for (int i = 0; i < proportions.Length; i++)
                BarFill(g, rectangles[i], individualSetHorizontally, proportions[i]);
        }

        static void BarFill(Graphics g, Rectangle r, bool horizontally, double[] proportions)
        {
            Rectangle[] rects = DivideRectangle(r, horizontally, proportions.Length, -1, proportions);
            for (int i = 0; i < proportions.Length; i++)
            {
                if (proportions[i] > 0)
                {
                    DrawAndFillRectangle(g, rects[i], i, proportions.Length);
                }
            }
        }

        static void DrawAndFillRectangle(Graphics g, Rectangle r, int intensity, int maxIntensity)
        {
            Rectangle rect = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);
            g.DrawRectangle(new Pen(Brushes.Black), rect);
            Rectangle innerRect = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);
            int lowestAlpha = 100;
            int highestAlpha = 200;
            int stepSize = (int) (((float)(highestAlpha - lowestAlpha))/((float)(maxIntensity - 1)));
            byte alpha = (byte)(lowestAlpha + (intensity - 1) * stepSize);
            System.Drawing.Color color = Color.FromArgb(alpha, 0, 0, 0);
            Brush brush = new SolidBrush(color);
            g.FillRectangle(brush, innerRect);
        }

        // Note that a margin of -1 will allow the inner rectangles to share borders
        static Rectangle[] DivideRectangle(Rectangle r, bool horizontally, int numRectangles, int margin, double[] proportions = null)
        {
            Rectangle[] result = new Rectangle[numRectangles];
            int[] pixels = new int[numRectangles];
            int existingPixels = horizontally ? r.Width : r.Height;
            int availableSpace = (existingPixels - (numRectangles - 1) * margin);
            int pixelsEach = availableSpace / numRectangles;
            if (proportions == null)
            {
                proportions = Enumerable.Range(1, numRectangles).Select(x => 1.0 / (double) numRectangles).ToArray();
            }
            pixels = proportions.Select(x => (int)(x * availableSpace)).ToArray();
            pixels[numRectangles - 1] = availableSpace + pixels[numRectangles - 1] - pixels.Sum();
            int pixelsSoFar = 0;
            for (int i = 0; i < numRectangles; i++)
            {
                if (horizontally)
                    result[i] = new Rectangle(r.X + pixelsSoFar, r.Y, pixels[i], r.Height);
                else
                    result[i] = new Rectangle(r.X, r.Y + pixelsSoFar, r.Width, pixels[i]);
                pixelsSoFar += pixels[i] + margin;
            }
            return result;
        }

        static void Main(string[] args)
        {
            string path = @"H:\My Drive\Articles, books in progress\Machine learning model of litigation\bluffing results";
            int NewWidth = 1000; int NewHeight = 1000;
            System.Drawing.Bitmap bmpOut = new System.Drawing.Bitmap(NewWidth, NewHeight);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmpOut);
            Rectangle overall = new Rectangle(0, 0, NewWidth, NewHeight);
            BarFill(g, overall, true, new double[][] 
            { 
                new double[] { 0.1, 0.1, 0.3, 0.35, 0.15 },
                new double[] { 0.2, 0.3, 0.1, 0.1, 0.1 },
                new double[] { 0.1, 0.1, 0.65, 0, 0.15 },
            });
            bmpOut.Save(path + @"\image.png");
        }
    }
}
