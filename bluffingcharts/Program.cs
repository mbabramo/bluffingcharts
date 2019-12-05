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
                        {
                            if (csv.GetField<string>(column) == "")
                            {
                                proportions[row - firstRow] = null;
                                break;
                            }
                            proportions[row - firstRow][column - firstColumn] = csv.GetField<double>(column);
                        }
                }
            }
            return proportions;
        }

        static void BarFill(Graphics g, Rectangle r, bool individualSetHorizontally, double[][] proportions, Color color, bool includeTextWherePossible)
        {
            Rectangle[] rectangles = DivideRectangle(r, !individualSetHorizontally, proportions.Length, 2 * bitmapMultiplier);
            for (int i = 0; i < proportions.Length; i++)
                BarFill(g, rectangles[i], individualSetHorizontally, proportions[i], color, includeTextWherePossible);
        }

        static void BarFill(Graphics g, Rectangle r, bool horizontally, double[] proportions, Color color, bool includeTextWherePossible)
        {
            if (proportions == null)
                return;
            GetMainFont(out int fontSize, out Font f);
            Rectangle[] rects = DivideRectangle(r, horizontally, proportions.Length, 0, proportions);
            for (int i = 0; i < proportions.Length; i++)
            {
                if (proportions[i] > 0)
                {
                    DrawAndFillRectangle(g, rects[i], i, proportions.Length, color);
                    if (rects[i].Width > fontSize * 2 && includeTextWherePossible)
                        DrawTextCenteredHorizontally_Shadowed(g, rects[i], (i + 1).ToString(), f);
                }
            }
        }

        static void DrawAndFillRectangle(Graphics g, Rectangle r, int intensity, int maxIntensity, Color color)
        {
            if (r.Width == 0)
                return;
            Rectangle rect = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);
            g.DrawRectangle(new Pen(Brushes.Black), rect);
            Rectangle innerRect = new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
            int lowestAlpha = 100;
            int highestAlpha = 200;
            int stepSize = (int) (((float)(highestAlpha - lowestAlpha))/((float)(maxIntensity - 1)));
            byte alpha = (byte)(lowestAlpha + (intensity - 1) * stepSize);
            System.Drawing.Color adjustedColor = Color.FromArgb(alpha, color);
            Brush brush = new SolidBrush(adjustedColor);
            g.FillRectangle(brush, innerRect);
        }

        static Rectangle[] DivideRectangle_WithSpaceForHeader(Rectangle r, bool horizontally, int pixelsForHeader)
        {
            double proportionForHeader = (double)pixelsForHeader / (double) (horizontally ? r.Width : r.Height);
            double[] proportions = new double[] { proportionForHeader, 1.0 - proportionForHeader };
            return DivideRectangle(r, horizontally, 2, 0, proportions);
        }

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

        static void GetDataFromCSVAndPlotAcross(string path, Graphics g, Rectangle r, string filename, (int firstRow, int firstColumn)[] setLocations, int numRowsPerSet, int numColumnsPerSet, int marginBetweenSets, Color color, bool includeTextWherePossible)
        {
            int numSets = setLocations.Length;
            Rectangle[] rects = DivideRectangle(r, true, numSets, marginBetweenSets);
            for (int set = 0; set < numSets; set++)
            {
                double[][] data = GetValuesFromCSV(path + @"\" + filename + ".csv", setLocations[set].firstRow, setLocations[set].firstColumn, numRowsPerSet, numColumnsPerSet);
                BarFill(g, rects[set], true, data, color, includeTextWherePossible);
            }
        }

        static void GetDataFromCSVAndPlot(string path, Graphics g, Rectangle r, string filename, int firstRow, int firstColumn, int numRows, int numColumns, Color color, bool includeTextWherePossible)
        {
            double[][] data = GetValuesFromCSV(path + @"\" + filename + ".csv", firstRow, firstColumn, numRows, numColumns);
            BarFill(g, r, true, data, color, includeTextWherePossible);
        }

        public static void DrawText270(Graphics g, Rectangle r, string text, Font font, Brush brush)
        {
            DrawRotatedText(g, r.X + r.Width / 2, r.Y + r.Height / 2, 270, text, font, brush);
        }

        public static void DrawRotatedText(Graphics g, int x, int y, float angle, string text, Font font, Brush brush)
        {
            g.TranslateTransform(x, y); // Set rotation point
            g.RotateTransform(angle); // Rotate text
            g.TranslateTransform(-x, -y); // Reset translate transform
            SizeF size = g.MeasureString(text, font); // Get size of rotated text (bounding box)
            g.DrawString(text, font, brush, new PointF(x - size.Width / 2.0f, y - size.Height / 2.0f)); // Draw string centered in x, y
            g.ResetTransform(); // Only needed if you reuse the Graphics object for multiple calls to DrawString
        }

        public static void DrawTextCenteredHorizontally_Shadowed(Graphics g, Rectangle r, string text, Font font)
        {
            r.Offset(2, 2);
            DrawTextCenteredHorizontally(g, r, text, font, Brushes.Black);
            r.Offset(-2, -2);
            DrawTextCenteredHorizontally(g, r, text, font, Brushes.DarkGray);
        }

        public static void DrawTextCenteredHorizontally(Graphics g, Rectangle r, string text, Font font, Brush brush)
        {
            StringFormat stringFormatHorizontally = new StringFormat();
            stringFormatHorizontally.Alignment = StringAlignment.Center;
            stringFormatHorizontally.LineAlignment = StringAlignment.Center;
            g.DrawString(text, font, brush, r, stringFormatHorizontally);
        }

        private static void PlotPAndDThreeRounds(string path, string filename, Graphics g, Rectangle r, int numSignals, int numOffers, bool addRoundHeaders = true)
        {
            int numRounds = 3;

            int marginBetweenSetsAcross = 10 * bitmapMultiplier;
            int marginBetweenSetsVertically = 10 * bitmapMultiplier;
            GetMainFont(out int fontSize, out Font f);
            r = AddLeftHeaders_WithFurtherSubdivision(g, r, marginBetweenSetsVertically, 2 * bitmapMultiplier, new string[] { "P Signal", "D Signal" }, Enumerable.Range(1, numSignals).Select(x => x.ToString()).ToArray());
            if (addRoundHeaders)
                r = AddRoundHeaders(g, r, numRounds, fontSize);

            Rectangle[] verticallyDivided = DivideRectangle(r, false, 2, marginBetweenSetsVertically);
            var plaintiffRoundLocations = new (int firstRow, int firstColumn)[]
            {
                (140, 85),
                (152, 97),
                (164, 109)
            };
            bool includeTextWherePossible = true;
            GetDataFromCSVAndPlotAcross(path, g, verticallyDivided[0], filename, plaintiffRoundLocations, numSignals, numOffers, marginBetweenSetsAcross, Color.Blue, includeTextWherePossible);
            var defendantRoundLocations = new (int firstRow, int firstColumn)[]
            {
                (146, 91),
                (158, 103),
                (170, 115)
            };
            GetDataFromCSVAndPlotAcross(path, g, verticallyDivided[1], filename, defendantRoundLocations, numSignals, numOffers, marginBetweenSetsAcross, Color.Orange, includeTextWherePossible);
        }

        private static Rectangle AddRoundHeaders(Graphics g, Rectangle r, int numRounds, int fontSize)
        {
            r = AddTopHeaders(g, r, fontSize + 3 * bitmapMultiplier, 3 * (fontSize + 3 * bitmapMultiplier), Enumerable.Range(1, numRounds).Select(x => $"Round {x}").ToArray());
            return r;
        }

        private static void GetMainFont(out int fontSize, out Font f)
        {
            fontSize = 12 * bitmapMultiplier;
            f = new Font(FontFamily.Families.First(x => x.Name == "Calibri Light"), fontSize, GraphicsUnit.World);
        }

        private static Rectangle AddLeftHeaders_WithFurtherSubdivision(Graphics g, Rectangle overallRectangle, int outerMarginBetweenSetsVertically, int innerMarginBetweenSetsVertically, string[] textOuter, string[] textInner)
        {
            overallRectangle = AddLeftHeaders(g, overallRectangle, outerMarginBetweenSetsVertically, textOuter);
            var divided = DivideRectangle(overallRectangle, false, textOuter.Length, outerMarginBetweenSetsVertically);
            Rectangle remaining = default;
            foreach (var division in divided)
            {
                Rectangle inside = AddLeftHeaders(g, division, innerMarginBetweenSetsVertically, textInner);
                if (remaining == default)
                    remaining = inside;
                else
                    remaining = Rectangle.Union(remaining, inside);
            }
            return remaining;
        }

        private static Rectangle AddLeftHeaders(Graphics g, Rectangle overallRectangle, int marginBetweenSetsVertically, params string[] text)
        {
            GetMainFont(out int fontSize, out Font f);
            Rectangle[] leftHeaderAndRest = DivideRectangle_WithSpaceForHeader(overallRectangle, true, fontSize + 3 * bitmapMultiplier);
            Rectangle leftHeaderSet = leftHeaderAndRest[0];
            Rectangle[] leftHeaders = DivideRectangle(leftHeaderSet, false, text.Length, marginBetweenSetsVertically);
            for (int i = 0; i < text.Length; i++)
                DrawText270(g, leftHeaders[i], text[i], f, Brushes.Black);
            Rectangle everythingRemaining = leftHeaderAndRest[1];
            return everythingRemaining;
        }

        private static Rectangle AddTopHeaders(Graphics g, Rectangle overallRectangle, int marginBetweenSetsHorizontally, int spaceLeftForLeftHeaders, params string[] text)
        {
            GetMainFont(out int fontSize, out Font f);
            Rectangle[] topHeaderAndRest = DivideRectangle_WithSpaceForHeader(overallRectangle, false, fontSize + 3 * bitmapMultiplier);
            Rectangle topHeaderSet = topHeaderAndRest[0];
            topHeaderSet.Size = new Size(topHeaderSet.Width - spaceLeftForLeftHeaders, topHeaderSet.Height);
            topHeaderSet.Offset(spaceLeftForLeftHeaders, 0);
            Rectangle[] topHeaders = DivideRectangle(topHeaderSet, true, text.Length, marginBetweenSetsHorizontally);
            for (int i = 0; i < text.Length; i++)
                DrawTextCenteredHorizontally(g, topHeaders[i], text[i], f, Brushes.Black);
            Rectangle everythingRemaining = topHeaderAndRest[1];
            return everythingRemaining;
        }

        const int bitmapMultiplier = 10;

        static void Main(string[] args)
        {
            string path = @"H:\My Drive\Articles, books in progress\Machine learning model of litigation\bluffing results";
            string filename = "R070 baseline";
            PlotPAndDThreeRounds_WithHidden(path, filename, numSignals: 5, numOffers: 5);
        }

        private static void PlotPAndDThreeRounds_WithHidden(string path, string filename, int numSignals, int numOffers)
        {
            GetMainFont(out int fontSize, out Font f);
            CreateBlankDrawing(out Bitmap bmpOut, out Graphics g, out Rectangle r);
            int margin = 10 * bitmapMultiplier;
            r = AddRoundHeaders(g, r, 3, fontSize);
            r = AddLeftHeaders(g, r, margin, "Revealed Offers", "Hidden Offers");
            Rectangle[] regularAndHidden = DivideRectangle(r, false, 2, margin);
            PlotPAndDThreeRounds(path, filename, g, regularAndHidden[0], numSignals, numOffers, false);
            PlotPAndDThreeRounds(path, filename + "-hidden", g, regularAndHidden[1], numSignals, numOffers, false);
            bmpOut.Save(path + @"\plot-" + filename + ".png");
        }

        private static void PlotPAndDThreeRounds(string path, string filename, int numSignals, int numOffers)
        {
            Bitmap bmpOut;
            Graphics g;
            Rectangle overall;
            CreateBlankDrawing(out bmpOut, out g, out overall);
            PlotPAndDThreeRounds(path, filename, g, overall, numSignals, numOffers);
            bmpOut.Save(path + @"\plot-" + filename + ".png");
        }

        private static void CreateBlankDrawing(out Bitmap bmpOut, out Graphics g, out Rectangle overall)
        {
            int NewWidth = 500 * bitmapMultiplier;
            int NewHeight = 300 * bitmapMultiplier;
            bmpOut = new System.Drawing.Bitmap(NewWidth, NewHeight);
            g = System.Drawing.Graphics.FromImage(bmpOut);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            overall = new Rectangle(5 * bitmapMultiplier, 5 * bitmapMultiplier, NewWidth - 10 * bitmapMultiplier, NewHeight - 10 * bitmapMultiplier);
        }
    }
}
