using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Windows.Media.Imaging;
using System.Windows;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.ComponentModel;
//using System.Windows.Media;

namespace UrbanizationIndex
{
    public partial class ImageProcessing
    {
        public static Bitmap combinePictures(Bitmap image, Bitmap image_2, Bitmap image_3, Bitmap image_4, int shift)
        {
            Rectangle rect = new Rectangle(26, 26, 588, 588);

            Bitmap cb = new Bitmap(image.Clone(rect, PixelFormat.Format32bppArgb));
            Bitmap cb2 = new Bitmap(image_2.Clone(rect, PixelFormat.Format32bppArgb));
            Bitmap cb3 = new Bitmap(image_3.Clone(rect, PixelFormat.Format32bppArgb));
            Bitmap cb4 = new Bitmap(image_4.Clone(rect, PixelFormat.Format32bppArgb));

            Bitmap wb = new Bitmap(cb);
            Bitmap wb2 = new Bitmap(cb2);
            Bitmap wb3 = new Bitmap(cb3);
            Bitmap wb4 = new Bitmap(cb4);

            byte[] pixels = new byte[(int)(wb.Width * wb.Height * 4)];
            byte[] pixels2 = new byte[(int)(wb.Width * wb.Height * 4)];
            byte[] pixels3 = new byte[(int)(wb.Width * wb.Height * 4)];
            byte[] pixels4 = new byte[(int)(wb.Width * wb.Height * 4)];

            //pixels arrays from images 

            // Lock the bitmap's bits.  
            Rectangle rect2 = new Rectangle(0, 0, wb.Width, wb.Height);
            System.Drawing.Imaging.BitmapData bmpData = new BitmapData();
            bmpData = wb.LockBits(rect2, System.Drawing.Imaging.ImageLockMode.ReadWrite, wb.PixelFormat);
            System.Drawing.Imaging.BitmapData bmpData2 = new BitmapData();
            bmpData2 = wb2.LockBits(rect2, System.Drawing.Imaging.ImageLockMode.ReadWrite, wb.PixelFormat);
            System.Drawing.Imaging.BitmapData bmpData3 = new BitmapData();
            bmpData3 = wb3.LockBits(rect2, System.Drawing.Imaging.ImageLockMode.ReadWrite, wb.PixelFormat);
            System.Drawing.Imaging.BitmapData bmpData4 = new BitmapData();
            bmpData4 = wb4.LockBits(rect2, System.Drawing.Imaging.ImageLockMode.ReadWrite, wb.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            IntPtr ptr2 = bmpData2.Scan0;
            IntPtr ptr3 = bmpData3.Scan0;
            IntPtr ptr4 = bmpData4.Scan0;


            // Declare an array to hold the bytes of the bitmap. 
            int bytes = Math.Abs(bmpData.Stride) * wb.Height;
            //pixels = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, bytes);
            System.Runtime.InteropServices.Marshal.Copy(ptr2, pixels2, 0, bytes);
            System.Runtime.InteropServices.Marshal.Copy(ptr3, pixels3, 0, bytes);
            System.Runtime.InteropServices.Marshal.Copy(ptr4, pixels4, 0, bytes);


            int[] results = new int[35];
            int min, minplace, diff = 0;

            for (int i = 0; i < 35; i++)
            {
                for (int j = 0; j < wb.Width * 4; j++)
                {
                    diff += Math.Abs(pixels2[1380624 + j] - pixels4[(i * 2352) + j]);
                }
                results[i] = diff;
                diff = 0;
            }

            min = results[0];
            minplace = 0;
            for (int i = 0; i < 35; i++)
            {
                if (min > results[i]) { min = results[i]; minplace = i; }
            }
            minplace++; //number of pixels-difference between under and upper pictures

            if (shift != -1) minplace = shift;//shift - we use the satellite shift at the roadmap

            string kulok = "";
            for (int i = 0; i < 35; i++)
            {
                kulok += " " + results[i].ToString();
            }

            //bottom picture's array without temp
            byte[] pixels3uj = new byte[(int)(wb.Width * wb.Height * 4 - (minplace * 2352))];
            byte[] pixels4uj = new byte[(int)(wb.Width * wb.Height * 4 - (minplace * 2352))];


            for (int i = minplace * 2352; i < pixels3uj.Length; i++)
            {
                pixels3uj[i - (minplace * 2352)] = pixels3[i];
                pixels4uj[i - (minplace * 2352)] = pixels4[i];
            }

            byte[] pixelsSum = new byte[(int)(pixels.Length * 2 + pixels3uj.Length * 2)];

            for (int i = 0; i < pixelsSum.Length; i++) { pixelsSum[i] = 255; }

            int count = 0;
            for (int i = 0; i < pixels2.Length * 2; i++) //upper left
            {
                if (i % (2352) == 0 && i != 0)
                {
                    i += 2352;

                }
                if (count < pixels2.Length)
                {
                    pixelsSum[i] = pixels2[count];
                }
                count++;
            }

            count = 0;
            for (int i = 2352; i < pixels.Length * 2; i++) //upper right
            {
                if (i % (4704) == 0 && i != 0)
                {

                    i += 2352;

                }
                if (count < pixels.Length)
                {
                    pixelsSum[i] = pixels[count];
                }
                count++;
            }

            count = 0;
            for (int i = pixels.Length * 2; i < pixelsSum.Length; i++) //bottom left
            {
                if (i % (2352) == 0 && i != pixels.Length * 2)
                {

                    i += 2352;

                }
                if (count < pixels4uj.Length)
                {
                    pixelsSum[i] = pixels4uj[count];
                }
                count++;
            }

            count = 0;
            for (int i = pixels.Length * 2; i < pixelsSum.Length; i++) //bottom right
            {
                if (i % (4704) == 0 && i != 0)
                {

                    i += 2352;

                }
                if (count < pixels3uj.Length)
                {
                    pixelsSum[i] = pixels3uj[count];
                }
                count++;
            }


            byte[] pixelsSumUj = new byte[pixelsSum.Length];
            for (int i = 0; i < (minplace / 2) * 1176 * 4; i++) //align centre and distributing empty places
            {
                pixelsSumUj[i] = 255;
            }

            for (int i = (minplace / 2) * 1176 * 4; i < pixelsSum.Length; i++)
            {
                if (i < pixelsSumUj.Length)
                {
                    pixelsSumUj[i] = pixelsSum[i - (minplace / 2) * 1176 * 4];
                }
            }
            Bitmap wbSum = new Bitmap(1176, 588 + pixels3uj.Length / 2352, wb.PixelFormat); //the high of picture is not constant
            Rectangle rect3 = new Rectangle(0, 0, wbSum.Width, wbSum.Height);
            System.Drawing.Imaging.BitmapData bmpDataSum =
                wbSum.LockBits(rect3, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                wbSum.PixelFormat);

            // Get the address of the first line.
            IntPtr ptrSum = bmpDataSum.Scan0;

            // Declare an array to hold the bytes of the bitmap. 
            int bytesSum = Math.Abs(bmpDataSum.Stride) * bmpDataSum.Height;

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(pixelsSumUj, 0, ptrSum, bytesSum);

            // Unlock the bits.
            wbSum.UnlockBits(bmpDataSum);
            wb.UnlockBits(bmpData);
            wb2.UnlockBits(bmpData2);
            wb3.UnlockBits(bmpData3);
            wb4.UnlockBits(bmpData4);
            //wbSum.Save(System.IO.Path.GetTempPath() + "\\combined.bmp");
            return wbSum;
        }

        //megadott szélesség és magasság beállítása
        public static Bitmap resizePicture(Bitmap wb, int w, int h, string nagyitas)
        {
            double oszto = 0;
            if (nagyitas == "16") { oszto = 1.612; } //1 pixel = 1,612m
            if (nagyitas == "17") { oszto = 0.806; } //1 pixel = 0,806m
            int szelesseg = Convert.ToInt32(w / oszto), magassag = Convert.ToInt32(h / oszto);
            while (szelesseg % 10 != 0) { szelesseg++; }
            while (magassag % 10 != 0) { magassag++; }

            Rectangle rect = new Rectangle(Convert.ToInt32((wb.Width - szelesseg) / 2), Convert.ToInt32((wb.Height - magassag) / 2), szelesseg, magassag);
            return wb.Clone(rect, PixelFormat.Format32bppArgb);
        }

        //red grid
        public static Bitmap partitionPicture(Bitmap wbold)
        {
            Bitmap wb = new Bitmap(wbold);
            //byte[] pixels2 = new byte[wb.Width * wb.Height * 4];
            Rectangle rect = new Rectangle(0, 0, wb.Width, wb.Height);
            Graphics g = Graphics.FromImage(wb);
            for (int i = 0; i < wb.Height - 1; i += wb.Height / 10)
            {
                g.DrawLine(new Pen(Color.White), 0, i, wb.Height - 1, i);
                g.DrawLine(new Pen(Color.White), i, 0, i, wb.Height - 1);
            }
            g.Flush();
            return wb;//picture with red grid
        }
    }
}
