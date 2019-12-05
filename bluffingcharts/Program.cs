using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using CsvHelper;
using System.Linq;

namespace bluffingcharts
{
    class Program
    {
        static double[][] GetValuesFromCSV(string fileName, int firstRow, int firstColumn, int numRows, int numColumns)
        {
            // Change from 1 numbering to 0 numbering
            firstRow -= 2; // 1 to adjust for 1 numbering, and 1 to adjust for header row
            firstColumn--;
            double[][] proportions = new double[numRows][];
            for (int i = 0; i < numColumns; i++)
                proportions[i] = new double[numColumns];
            using (var reader = new StreamReader(fileName))
            using (var csv = new CsvReader(reader))
            {
                csv.Read();
                csv.ReadHeader();
                int row = -1;
                while (csv.Read())
                {
                    row++;
                    var rowHeader = csv.GetField(0);
                    if (row == firstRow + numRows)
                        return proportions;
                    if (row >= firstRow)
                        for (int column = firstColumn; column < firstColumn + numColumns; column++)
                            proportions[row - firstRow][column - firstColumn] = csv.GetField<double>(column);
                }
            }
            return proportions;
        }

        static void BarFill(Graphics g, Rectangle r, bool individualSetHorizontally, double[][] proportions)
        {
            Rectangle[] rectangles = DivideRectangle(r, !individualSetHorizontally, proportions.Length, 2);
            for (int i = 0; i < proportions.Length; i++)
                BarFill(g, rectangles[i], individualSetHorizontally, proportions[i]);
        }

        static void BarFill(Graphics g, Rectangle r, bool horizontally, double[] proportions)
        {
            Rectangle[] rects = DivideRectangle(r, horizontally, proportions.Length, 0, proportions);
            for (int i = 0; i < proportions.Length; i++)
            {
                if (proportions[i] > 0)
                {
                    DrawAndFillRectangle(g, rects[i], i, proportions.Length);
                }
            }
        }

        static void DrawAndFillRectangle(Graphics g, Rectangle r, int intensity, int maxIntensity, Color color = default)
        {
            if (r.Width == 0)
                return;
            Rectangle rect = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);
            g.DrawRectangle(new Pen(Brushes.Black), rect);
            Rectangle innerRect = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);
            int lowestAlpha = 100;
            int highestAlpha = 200;
            int stepSize = (int) (((float)(highestAlpha - lowestAlpha))/((float)(maxIntensity - 1)));
            byte alpha = (byte)(lowestAlpha + (intensity - 1) * stepSize);
            System.Drawing.Color adjustedColor = Color.FromArgb(alpha, color);
            Brush brush = new SolidBrush(adjustedColor);
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
            int spaceAdjustment = pixels.Sum() - availableSpace;
            int indexToChange = -1;
            while (spaceAdjustment != 0)
            {
                indexToChange++;
                if (indexToChange == numRectangles)
                    indexToChange = 0;
                if (pixels[indexToChange] > 0)
                {
                    if (spaceAdjustment > 0)
                        pixels[indexToChange]--;
                    else
                        pixels[indexToChange]++;
                }
                spaceAdjustment = pixels.Sum() - availableSpace;
            }
            int pixelsSoFar = 0;
            for (int i = 0; i < numRectangles; i++)
            {
                bool avoidDoubleMargin = true;
                int marginAdjustment = 0;
                if (avoidDoubleMargin && i > 0)
                    marginAdjustment = 1;
                if (horizontally)
                    result[i] = new Rectangle(r.X + pixelsSoFar - marginAdjustment, r.Y, pixels[i] + marginAdjustment, r.Height);
                else
                    result[i] = new Rectangle(r.X, r.Y + pixelsSoFar - marginAdjustment, r.Width, pixels[i] + marginAdjustment);
                pixelsSoFar += pixels[i] + margin;
            }
            return result;
        }

        static void Main(string[] args)
        {
            string path = @"H:\My Drive\Articles, books in progress\Machine learning model of litigation\bluffing results";
            int NewWidth = 300; int NewHeight = 300;
            System.Drawing.Bitmap bmpOut = new System.Drawing.Bitmap(NewWidth, NewHeight);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmpOut);
            Rectangle overall = new Rectangle(0, 0, NewWidth, NewHeight);
            double[][] data = GetValuesFromCSV(path + @"\" + "R070 baseline" + ".csv", 140, 85, 5, 5) ;
            BarFill(g, overall, true, data);
            //new double[][] 
            //{ 
            //    new double[] { 0.1, 0.1, 0.3, 0.35, 0.15 },
            //    new double[] { 0.2, 0.3, 0.1, 0.1, 0.1 },
            //    new double[] { 0.1, 0.1, 0.65, 0, 0.15 },
            //});
            bmpOut.Save(path + @"\image.png");
        }
    }
}
