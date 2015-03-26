using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace UrbanizationIndex
{
    public class Territory
    {
        public int ID { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Name { get; set; }
        public Bitmap SatellitePic { get; set; }
        public Bitmap SatelliteGridPic { get; set; }
        public Bitmap MapPic { get; set; }
        public Bitmap MapGridPic { get; set; }
        public Bitmap TrainingPic { get; set; }
        public List<TrainingPoint> TrainingPoints { get; set; }
        public SVMResult[] SVMResults { get; set; }
        public double PCAResult { get; set; }
        public string DirPath { get; set; }

        public Territory(int id, double latitude, double longitude, string name, string projectDataPath)
        {
            ID = id;
            Latitude = latitude;
            Longitude = longitude;
            Name = name;
            TrainingPoints = new List<TrainingPoint>();
            SVMResults = new SVMResult[100];
            DirPath = projectDataPath + '\\' + ID.ToString();

            if (!Directory.Exists(DirPath))
                Directory.CreateDirectory(DirPath);

            if (File.Exists(DirPath + "\\01.bmp"))
            {
                using (FileStream stream = new FileStream(DirPath + "\\01.bmp", FileMode.Open, FileAccess.Read))
                {
                    SatellitePic = new Bitmap(stream);
                }
            }

            if (File.Exists(DirPath + "\\01_grid.bmp"))
            {
                using (FileStream stream = new FileStream(DirPath + "\\01_grid.bmp", FileMode.Open, FileAccess.Read))
                {
                    SatelliteGridPic = new Bitmap(stream);
                }
            }

            if (File.Exists(DirPath + "\\02.bmp"))
            {
                using (FileStream stream = new FileStream(DirPath + "\\02.bmp", FileMode.Open, FileAccess.Read))
                {
                    MapPic = new Bitmap(stream);
                }
            }

            if (File.Exists(DirPath + "\\02_grid.bmp"))
            {
                using (FileStream stream = new FileStream(DirPath + "\\02_grid.bmp", FileMode.Open, FileAccess.Read))
                {
                    MapGridPic = new Bitmap(stream);
                }
            }

            if (SatellitePic != null)
                TrainingPic = new Bitmap(SatellitePic);

            if (File.Exists(DirPath + "\\03.txt"))
            {
                string[] lines = File.ReadAllLines(DirPath + "\\03.txt");

                foreach (string line in lines)
                {
                    try
                    {
                        string[] data = line.Split(new char[] { ' ' });
                        TrainingPoints.Add(new TrainingPoint(int.Parse(data[0]), int.Parse(data[1]), data[2], true));
                    }
                    catch (Exception) { }
                }
            }

            for (int i = 0; i < SVMResults.Length; i++)
                SVMResults[i] = new SVMResult();
        }

        public override string ToString()
        {
            if (SatellitePic == null)
                return Name + " (Lat: " + Latitude.ToString() + ", Lng: " + Longitude.ToString() + ") - No image!";
            else
                return Name + " (Lat: " + Latitude.ToString() + ", Lng: " + Longitude.ToString() + ")";
        }
    }
}
