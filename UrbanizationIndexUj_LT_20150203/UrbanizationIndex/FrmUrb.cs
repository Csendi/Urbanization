using Google.Api.Maps.Service.StaticMaps;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Ionic.Zip;

namespace UrbanizationIndex
{
    public partial class FrmUrb : Form
    {
        Project actproject;
        Bitmap image, image_2, image_3, image_4; // These images will be filled with google maps pictures
        string enlargement = "16";
        string mapsize = "640x640";
        Calc formcalc;
        BackgroundWorker Worker;
        BackgroundWorker PCAWorker;
        BackgroundWorker LoadingWorker;
        BackgroundWorker DownloadImages;
        BackgroundWorker ExportWorker;
        BackgroundWorker ZipExtractWorker;
        BackgroundWorker ImportWorker;
        CultureInfo ci;
        string ImportFilePath;
        int PicturesLeft = 0;

        [DllImport("urbIndex1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ImageProcModule(byte[] path);

        bool NewSVM = true; // Az új vagy a régi SVM-et használjuk?
        [DllImport("SVMPCA.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int svm(byte[] path);

        [DllImport("SVMPCA.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pca(byte[] path);

        public FrmUrb()
        {
            InitializeComponent();

            // Initialize backgroundworkers
            Worker = new BackgroundWorker();
            Worker.DoWork += calculate_DoWork;
            Worker.RunWorkerCompleted += calculate_RunWorkerCompleted;

            PCAWorker = new BackgroundWorker();
            PCAWorker.DoWork += calculatePCA_DoWork;
            PCAWorker.RunWorkerCompleted += calculatePCA_RunWorkerCompleted;

            LoadingWorker = new BackgroundWorker();
            LoadingWorker.DoWork += Loading;
            LoadingWorker.RunWorkerCompleted += LoadingCompleted;

            DownloadImages = new BackgroundWorker();
            DownloadImages.DoWork += DownloadingImages;
            DownloadImages.ProgressChanged += DownloadingCompleted;
            DownloadImages.WorkerReportsProgress = true;

            ExportWorker = new BackgroundWorker();
            ExportWorker.DoWork += Exporting;
            ExportWorker.RunWorkerCompleted += ExportCompleted;

            ZipExtractWorker = new BackgroundWorker();
            ZipExtractWorker.DoWork += ZipExtracting;
            ZipExtractWorker.RunWorkerCompleted += ZipExtractCompleted;

            ImportWorker = new BackgroundWorker();
            ImportWorker.DoWork += Importing;
            ImportWorker.RunWorkerCompleted += ImportCompleted;

            // Initialize controls
            tabControlUrb.SelectedIndex = 0;
            comboBoxPT.SelectedIndex = 0;
            comboBoxType.SelectedIndex = 0;
            ribbonPanel8.Visible = false;

            // Initialize listBoxType's contextmenu
            listBoxType.ContextMenu = new ContextMenu();
            var item = new MenuItem("Delete Selected Points");
            item.Click += RemoveTrainingPoints;
            listBoxType.ContextMenu.MenuItems.Add(item);
            listBoxType.MouseDown += SelectWithRightClick;

            // Set cultureinfo
            ci = CultureInfo.InvariantCulture.Clone() as CultureInfo;
            ci.NumberFormat.NumberDecimalSeparator = ".";
        }

        private void calculatePCA_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            formcalc.Close();

            listBoxResult.Items.Clear();
            if (actproject.PCAState == Project.PCAStates.OK)
            {
                List<TerritoryForPCAResult> tempList = new List<TerritoryForPCAResult>();
                foreach (var item in actproject.Territories)
                    tempList.Add(new TerritoryForPCAResult(item.Value));

                tempList.Sort();

                foreach (TerritoryForPCAResult item in tempList)
                    listBoxResult.Items.Add(item);

                if (listBoxResult.Items.Count != 0)
                    listBoxResult.SelectedIndex = 0;
            }

            ribbonBtnResult_Click(null, null);
        }

        private void calculatePCA_DoWork(object sender, DoWorkEventArgs e)
        {
            byte[] array = Encoding.ASCII.GetBytes(actproject.DataPath + '\\');

            if (pca(array) == 0)
                actproject.LoadPCAResults();
        }

        private void SelectWithRightClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                int index = listBoxType.IndexFromPoint(e.X, e.Y);
                if (index >= 0)
                    listBoxType.SelectedIndices.Add(index);
            }
        }

        // New project
        private void btnNew_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Filter = "UrbIndex Project File (*.csv)|*.csv";
            dlg.RestoreDirectory = true;

            Stream myStream;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = dlg.OpenFile()) != null)
                {
                    myStream.Close();

                    try
                    {
                        loadProjectData(dlg.FileName);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Project file error! Please check the correct format!");
                    }
                }
            }
        }

        // Open project
        private void btnOpen_Click(object sender, EventArgs e)
        {
            // Create OpenFileDialog 
            OpenFileDialog dlg = new OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.Filter = "UrbIndex Project File (*.csv)|*.csv";

            // Display OpenFileDialog by calling ShowDialog method 
            DialogResult result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == DialogResult.OK)
            {
                try
                {
                    loadProjectData(dlg.FileName);
                }
                catch (Exception)
                {
                    MessageBox.Show("Project file error! Please check the correct format!");
                }
            }
        }


        // Loads actproject and ui
        private void loadProjectData(string path)
        {
            LoadingButton1.Visible = true;
            LoadingWorker.RunWorkerAsync(path);
        }

        private void LoadingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LoadingButton1.Visible = false;

            labelProjPath.Text = actproject.Path;
            labelProjName.Text = actproject.Name;
            this.Text = "Project's title: " + actproject.Name;
            textBoxProjName.Text = actproject.Name;
            textBoxPath.Text = actproject.Path;
            textBoxLng.Text = "";
            textBoxLat.Text = "";
            listBoxcsv.Items.Clear();
            listBoxTerritories.Items.Clear();
            listBoxResult.Items.Clear();


            List<TerritoryForPCAResult> tempList = new List<TerritoryForPCAResult>();
            foreach (KeyValuePair<int, Territory> item in actproject.Territories)
            {
                listBoxTerritories.Items.Add(item.Value);
                listBoxcsv.Items.Add(item.Value);
                if (actproject.PCAState == Project.PCAStates.OK || actproject.PCAState == Project.PCAStates.NeedsRecalc)
                    tempList.Add(new TerritoryForPCAResult(item.Value));
            }

            tempList.Sort();
            foreach (TerritoryForPCAResult item in tempList)
            {
                listBoxResult.Items.Add(item);
            }

            if (listBoxTerritories.Items.Count != 0)
                listBoxTerritories.SelectedIndex = 0;

            if (listBoxResult.Items.Count != 0)
                listBoxResult.SelectedIndex = 0;

            tabControlUrb.SelectedIndex = 1;
        }

        private void Loading(object sender, DoWorkEventArgs e)
        {
            actproject = Project.Load((string)e.Argument);
        }


        private void changeImage(object sender, EventArgs e)
        {
            if (listBoxTerritories.SelectedItem != null && comboBoxPT.SelectedIndex != -1)
            {
                Territory territory = (Territory)listBoxTerritories.SelectedItem;
                switch (comboBoxPT.SelectedItem.ToString())
                {
                    case "Satellite":
                        pictureBoxMap.Image = territory.SatellitePic;
                        listBoxType.Visible = false;
                        comboBoxType.Visible = false;
                        checkBoxDraw.Visible = false;
                        break;

                    case "Map":
                        pictureBoxMap.Image = territory.MapPic;
                        listBoxType.Visible = false;
                        comboBoxType.Visible = false;
                        checkBoxDraw.Visible = false;
                        break;

                    default:
                        pictureBoxMap.Image = territory.TrainingPic;
                        if (territory.TrainingPic != null)
                        {
                            listBoxType.Visible = true;
                            comboBoxType.Visible = true;
                            checkBoxDraw.Visible = true;
                            listBoxType.Items.Clear();

                            foreach (TrainingPoint tp in territory.TrainingPoints)
                            {
                                listBoxType.Items.Add(tp);
                            }

                            drawPoints(territory.TrainingPoints);
                        }
                        break;
                }
            }
        }

        private void showHelp(object sender, EventArgs e)
        {
            tabControlUrb.SelectedIndex = 0;
        }

        private void btnRibDownload_Click(object sender, EventArgs e)
        {
            if (actproject == null)
            {
                MessageBox.Show(this, "Create or load project!", "No project", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            tabControlUrb.SelectedIndex = 3;
            listBox2.Items.Clear();

            foreach (KeyValuePair<int, Territory> item in actproject.Territories)
            {
                if (item.Value.SatellitePic == null)
                    listBox2.Items.Add(item.Value);
            }

            labNofDP.Text = "Number of pictures to be downloaded: " + listBox2.Items.Count.ToString();
        }

        private void btnDownl_Click(object sender, EventArgs e)
        {
            if (listBox2.Items.Count == 0)
            {
                MessageBox.Show(this, "All pictures have been downloaded!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PicturesLeft = listBox2.Items.Count;
            LoadingButton2.Visible = true;
            ButtonDownloadStart.Enabled = false;
            DownloadImages.RunWorkerAsync();
        }

        private void DownloadingImages(object sender, DoWorkEventArgs e)
        {
            try
            {
                foreach (var o in listBox2.Items)
                {
                    Territory territory = (Territory)o;
                    if (territory.SatellitePic == null)
                    {
                        refreshMap(territory.Latitude, territory.Longitude, "satellite");
                        Bitmap combinedImageSat = ImageProcessing.combinePictures(image, image_2, image_3, image_4, -1);

                        refreshMap(territory.Latitude, territory.Longitude, "roadmap");
                        int shift = 1176 - combinedImageSat.Height; //eltolás kiszámolva a Satellite kép alapján, Roadmapnál már nem kell eltolást számolni
                        Bitmap combinedImageRoad = ImageProcessing.combinePictures(image, image_2, image_3, image_4, shift);

                        refreshMap(territory.Latitude, territory.Longitude, "roadmap_st");
                        Bitmap combinedImageRoadStyle = ImageProcessing.combinePictures(image, image_2, image_3, image_4, shift);
                        combinedImageRoadStyle = ImageProcessing.resizePicture(combinedImageRoadStyle, 1000, 1000, enlargement);
                        combinedImageRoadStyle.Save(territory.DirPath + "\\04.bmp");

                        territory.SatellitePic = ImageProcessing.resizePicture(combinedImageSat, 1000, 1000, enlargement);
                        territory.SatelliteGridPic = ImageProcessing.partitionPicture(territory.SatellitePic);
                        territory.MapPic = ImageProcessing.resizePicture(combinedImageRoad, 1000, 1000, enlargement);
                        territory.MapGridPic = ImageProcessing.partitionPicture(territory.MapPic);
                        territory.TrainingPic = new Bitmap(territory.SatellitePic);

                        territory.SatellitePic.Save(territory.DirPath + "\\01.bmp");
                        territory.SatelliteGridPic.Save(territory.DirPath + "\\01_grid.bmp");
                        territory.MapPic.Save(territory.DirPath + "\\02.bmp");
                        territory.MapGridPic.Save(territory.DirPath + "\\02_grid.bmp");

                        DownloadImages.ReportProgress(0);
                    }
                }
            }
            catch (Exception)
            {
                LoadingButton2.Visible = false;
                ButtonDownloadStart.Enabled = false;
                MessageBox.Show(this, "GoogleMaps Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
            }
        }

        private void DownloadingCompleted(object sender, ProgressChangedEventArgs e)
        {
            PicturesLeft--;

            labNofDP.Text = "Number of pictures to be downloaded: " + PicturesLeft;

            if (PicturesLeft == 0)
            {
                LoadingButton2.Visible = false;
                ButtonDownloadStart.Enabled = false;
                listBox2.Items.Clear();
                MessageBox.Show(this, "All pictures have been downloaded!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                listBoxcsv.Items.Clear();
                listBoxTerritories.Items.Clear();
                foreach (var item in actproject.Territories)
                {
                    listBoxcsv.Items.Add(item.Value);
                    listBoxTerritories.Items.Add(item.Value);
                }

                if (listBoxTerritories.Items.Count != 0)
                    listBoxTerritories.SelectedIndex = 0;
            }
        }


        private void btnRibBrowse_Click(object sender, EventArgs e)
        {
            if (actproject == null)
            {
                MessageBox.Show(this, "Create or load project!", "No project", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            tabControlUrb.SelectedIndex = 2;
        }

        private void refreshMap(double latdoub, double longdoub, string maptype)
        {
            var map = new StaticMap();
            var map2 = new StaticMap();
            var map3 = new StaticMap();
            var map4 = new StaticMap();

            try
            {
                double latbf = 0, latjf = 0, latba = 0, latja = 0, lonbf = 0, lonjf = 0, lonba = 0, lonja = 0;
                decimal lat = Convert.ToDecimal(latdoub);
                decimal lng = Convert.ToDecimal(longdoub);
                if (enlargement == "16")
                {
                    latjf = Convert.ToDouble(lat + Convert.ToDecimal(0.004155396));
                    lonjf = Convert.ToDouble(lng + Convert.ToDecimal(0.0063105175292152327211808));

                    latbf = Convert.ToDouble(lat + Convert.ToDecimal(0.004155396));
                    lonbf = Convert.ToDouble(lng - Convert.ToDecimal(0.0063105175292152327211808));

                    latja = Convert.ToDouble(lat - Convert.ToDecimal(0.004155396));
                    lonja = Convert.ToDouble(lng + Convert.ToDecimal(0.0063105175292152327211808));

                    latba = Convert.ToDouble(lat - Convert.ToDecimal(0.004155396));
                    lonba = Convert.ToDouble(lng - Convert.ToDecimal(0.0063105175292152327211808));
                }
                else if (enlargement == "17")
                {
                    latjf = Convert.ToDouble(lat + Convert.ToDecimal(0.002077698));
                    lonjf = Convert.ToDouble(lng + Convert.ToDecimal(0.0031552587646076163605904));

                    latbf = Convert.ToDouble(lat + Convert.ToDecimal(0.002077698));
                    lonbf = Convert.ToDouble(lng - Convert.ToDecimal(0.0031552587646076163605904));


                    latja = Convert.ToDouble(lat - Convert.ToDecimal(0.002077698));
                    lonja = Convert.ToDouble(lng + Convert.ToDecimal(0.0031552587646076163605904));

                    latba = Convert.ToDouble(lat - Convert.ToDecimal(0.002077698));
                    lonba = Convert.ToDouble(lng - Convert.ToDecimal(0.0031552587646076163605904));
                }

                while (Math.Abs(latLongToPixels(latbf, lonbf) - latLongToPixels(latba, lonba)) > 588)
                { //decrease the distance between upper and lower pictures
                    latbf -= 0.000001;
                    latjf -= 0.000001;
                    latja += 0.000001;
                    latba += 0.000001;
                }

                while (Math.Abs(latLongToPixels(latbf, lonbf) - latLongToPixels(latba, lonba)) < 568)
                { //increase the distance between upper and lower pictures
                    latbf += 0.000001;
                    latjf += 0.000001;
                    latja -= 0.000001;
                    latba -= 0.000001;
                }

                //centers are counted, giving to the map
                map.Center = latjf.ToString().Replace(',', '.') + "," + lonjf.ToString().Replace(',', '.');
                map2.Center = latbf.ToString().Replace(',', '.') + "," + lonbf.ToString().Replace(',', '.');
                map3.Center = latja.ToString().Replace(',', '.') + "," + lonja.ToString().Replace(',', '.');
                map4.Center = latba.ToString().Replace(',', '.') + "," + lonba.ToString().Replace(',', '.');
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Refresh Error" + e.Message);
                return;
            }


            map.Zoom = enlargement;
            map.Size = mapsize;
            map2.Zoom = enlargement;
            map2.Size = mapsize;
            map3.Zoom = enlargement;
            map3.Size = mapsize;
            map4.Zoom = enlargement;
            map4.Size = mapsize;

            map.MapType = maptype;
            map2.MapType = maptype;
            map3.MapType = maptype;
            map4.MapType = maptype;

            map.Sensor = "false";
            map2.Sensor = "false";
            map3.Sensor = "false";
            map4.Sensor = "false";

            if (maptype == "roadmap_st")
            {
                String src = "http://maps.googleapis.com/maps/api/staticmap?center=" +
            map.Center + "&zoom=" + map.Zoom + "&size=" + map.Size + "&format=png32&maptype=" + map.MapType +
            "&style=feature:all%7Celement:all%7Cvisibility:off" +
            "&style=feature:water%7Celement:geometry%7Ccolor:0x000000%7Cvisibility:on" +
            "&style=feature:landscape%7Celement:geometry%7Ccolor:0x000000%7Cvisibility:on" +
            "&style=feature:road%7Celement:geometry%7Ccolor:0xffffff%7Cvisibility:on" +
            "&style=feature:road.highway%7Celement:geometry%7Ccolor:0xffff00%7Cvisibility:on";
                String src2 = "http://maps.googleapis.com/maps/api/staticmap?center=" +
           map2.Center + "&zoom=" + map2.Zoom + "&size=" + map2.Size + "&format=png32&maptype=" + map2.MapType +
           "&style=feature:all%7Celement:all%7Cvisibility:off" +
           "&style=feature:water%7Celement:geometry%7Ccolor:0x000000%7Cvisibility:on" +
           "&style=feature:landscape%7Celement:geometry%7Ccolor:0x000000%7Cvisibility:on" +
           "&style=feature:road%7Celement:geometry%7Ccolor:0xffffff%7Cvisibility:on" +
           "&style=feature:road.highway%7Celement:geometry%7Ccolor:0xffff00%7Cvisibility:on";
                String src3 = "http://maps.googleapis.com/maps/api/staticmap?center=" +
           map3.Center + "&zoom=" + map.Zoom + "&size=" + map.Size + "&format=png32&maptype=" + map.MapType +
           "&style=feature:all%7Celement:all%7Cvisibility:off" +
           "&style=feature:water%7Celement:geometry%7Ccolor:0x000000%7Cvisibility:on" +
           "&style=feature:landscape%7Celement:geometry%7Ccolor:0x000000%7Cvisibility:on" +
           "&style=feature:road%7Celement:geometry%7Ccolor:0xffffff%7Cvisibility:on" +
           "&style=feature:road.highway%7Celement:geometry%7Ccolor:0xffff00%7Cvisibility:on";
                String src4 = "http://maps.googleapis.com/maps/api/staticmap?center=" +
           map4.Center + "&zoom=" + map.Zoom + "&size=" + map.Size + "&format=png32&maptype=" + map.MapType +
           "&style=feature:all%7Celement:all%7Cvisibility:off" +
           "&style=feature:water%7Celement:geometry%7Ccolor:0x000000%7Cvisibility:on" +
           "&style=feature:landscape%7Celement:geometry%7Ccolor:0x000000%7Cvisibility:on" +
           "&style=feature:road%7Celement:geometry%7Ccolor:0xffffff%7Cvisibility:on" +
           "&style=feature:road.highway%7Celement:geometry%7Ccolor:0xffff00%7Cvisibility:on";
                Uri uriSrc = new Uri(src);
                Uri uriSrc2 = new Uri(src2);
                Uri uriSrc3 = new Uri(src3);
                Uri uriSrc4 = new Uri(src4);
                image = LoadPicture(uriSrc.AbsoluteUri);
                image_2 = LoadPicture(uriSrc2.AbsoluteUri);
                image_3 = LoadPicture(uriSrc3.AbsoluteUri);
                image_4 = LoadPicture(uriSrc4.AbsoluteUri);
            }
            else
            {
                image = LoadPicture(map.ToUri().AbsoluteUri);
                image_2 = LoadPicture(map2.ToUri().AbsoluteUri);
                image_3 = LoadPicture(map3.ToUri().AbsoluteUri);
                image_4 = LoadPicture(map4.ToUri().AbsoluteUri);
            }
        }

        private double latLongToPixels(double Latitude, double Longitude)
        {
            double sinLatitudeCenter = Math.Sin(Latitude * Math.PI / 180);
            double pixelYCenter = (0.5 - Math.Log((1 + sinLatitudeCenter) / (1 - sinLatitudeCenter)) / (4 * Math.PI)) * 256 * Math.Pow(2, Convert.ToInt32(enlargement));//the number of pixel vertically
            return pixelYCenter;
        }

        private double latLongToPixelsX(double Latitude, double Longitude)
        {
            double sinLatitudeCenter = Math.Sin(Latitude * Math.PI / 180);
            double pixelXCenter = ((Longitude + 180) / 360) * 256 * Math.Pow(2, Convert.ToInt32(enlargement)); // the number of pixel horizontally
            return pixelXCenter;
        }

        private Bitmap LoadPicture(string url)
        {
            Bitmap pic = null;
            try
            {
                var wreq = (HttpWebRequest)WebRequest.Create(url);
                using (var wresp = (HttpWebResponse)wreq.GetResponse())
                using (var mystream = wresp.GetResponseStream())
                {
                    pic = new Bitmap(mystream);
                }
            }
            catch (Exception e)
            {
                LoadingButton2.Visible = false;
                ButtonDownloadStart.Enabled = true;
                System.Windows.Forms.MessageBox.Show(e.Message, "GoogleMaps could not be accessed!");
            }
            return pic;
        }


        private void pictureBoxMap_MouseDown(object sender, MouseEventArgs e)
        {
            if (checkBoxDraw.Checked && comboBoxPT.Text == "Training" && comboBoxType.Text != "Choose type" && listBoxTerritories.SelectedItem != null)
            {
                Territory territory = (Territory)listBoxTerritories.SelectedItem;
                TrainingPoint tp = new TrainingPoint(e.X, e.Y, comboBoxType.Text);
                territory.TrainingPoints.Add(tp);
                listBoxType.Items.Add(tp);
                DrawPoint(tp);

                actproject.TerritoryState = Project.TerritoryStates.Modified;
            }
        }

        private void DrawPoint(TrainingPoint tp, bool refresh = true)
        {
            Graphics g = Graphics.FromImage(pictureBoxMap.Image);
            try
            {
                g.DrawEllipse(new Pen(TrainingPoint.GetColorForTP(tp), 2), tp.XPos - 10, tp.YPos - 10, 20, 20);
                
                if (refresh)
                    pictureBoxMap.Refresh();
            }
            catch (Exception) { }
        }

        public void drawPoints(List<TrainingPoint> trainingPoints)
        {
            foreach (TrainingPoint point in trainingPoints)
            {
                DrawPoint(point, false);
            }
            pictureBoxMap.Refresh();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (actproject == null)
            {
                MessageBox.Show(this, "Create or load project!", "No project", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            try
            {
                actproject.Save();
                MessageBox.Show(this, "Saved successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception)
            {
                MessageBox.Show(this, "Project could not be saved", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveTrainingPoints(object sender, EventArgs e)
        {
            if (listBoxTerritories.SelectedItem == null)
                return;

            Territory territory = (Territory)listBoxTerritories.SelectedItem;
            List<TrainingPoint> ToDelete = new List<TrainingPoint>();
            foreach (var item in listBoxType.SelectedItems)
            {
                TrainingPoint tp = (TrainingPoint)item;
                territory.TrainingPoints.Remove(tp);
                ToDelete.Add(tp);
            }

            foreach (TrainingPoint tp in ToDelete)
                listBoxType.Items.Remove(tp);

            territory.TrainingPic = new Bitmap(territory.SatellitePic);
            pictureBoxMap.Image = territory.TrainingPic;
            drawPoints(territory.TrainingPoints);

            actproject.TerritoryState = Project.TerritoryStates.Modified;
        }

        private void ribbonBtnCalculate_Click(object sender, EventArgs e)
        {
            if (actproject == null)
            {
                MessageBox.Show(this, "Create or load project!", "No project", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            bool calc = true;
            if (actproject.Territories.Count < 2)
            {
                DialogResult res = MessageBox.Show(this, "Urbanization Index will not be calculated, because it needs at least two pictures. Would you like to continue?", "PCA needs at least two pictures", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                calc = (res == DialogResult.Yes ? true : false);
            }
            if (!calc)
                return;

            actproject.Save(); // Saves new/deleted training points if there are any
            calc = true;
            if (actproject.PCAState == Project.PCAStates.OK)
            {
                DialogResult res = MessageBox.Show("The result has already been calculated. Do you want to calculate anyway?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                calc = (res == DialogResult.Yes ? true : false);
            }

            if (calc)
            {
                foreach (KeyValuePair<int, Territory> item in actproject.Territories)
                {
                    if (item.Value.SatellitePic == null)
                    {
                        MessageBox.Show("All images should be downloaded!");
                        return;
                    }

                    if (item.Value.TrainingPoints.Count == 0)
                    {
                        MessageBox.Show("All images need training points!");
                        return;
                    }
                }

                formcalc = new Calc();
                formcalc.Show();
                ribbonPanel8.Visible = true;
                Worker.RunWorkerAsync();
            }
            else
            {
                ribbonBtnResult_Click(null, null);
            }
        }

        private void ribbonBtnAnnotation_Click(object sender, EventArgs e)
        {
            if (actproject == null)
            {
                MessageBox.Show(this, "Create or load project!", "No project", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            if (actproject.Territories.Count == 0)
            {
                MessageBox.Show(this, "Please add at least one location first", "No location", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            if (!File.Exists(actproject.DataPath + "\\out.csv"))
            {
                MessageBox.Show(this, "Please calculate results first!", "Results needed", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            
            new Annotation(actproject).Show();
        }

        private void ribbonBtnResult_Click(object sender, EventArgs e)
        {
            if (actproject == null)
            {
                MessageBox.Show(this, "Create or load project!", "No project", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            tabControlUrb.SelectedIndex = 4;

            if (actproject.PCAState == Project.PCAStates.NeedsRecalc)
            {
                MessageBox.Show(this, "PCA needs to be recalculated!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (actproject.PCAState == Project.PCAStates.NoPCA)
            {
                MessageBox.Show(this, "Calculate result first!", "No results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (actproject.PCAState == Project.PCAStates.BadPCA)
            {
                MessageBox.Show(this, "PCA could not be computed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ribbonBtnPCA_Click(object sender, EventArgs e)
        {
            if (actproject == null)
            {
                MessageBox.Show(this, "Create or load project!", "No project", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            if (!File.Exists(actproject.DataPath + "\\out.csv") || !File.Exists(actproject.DataPath + "\\svmResult.csv"))
            {
                MessageBox.Show(this, "Please calculate results first!", "No result", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            if (actproject.Territories.Count < 2)
            {
                MessageBox.Show(this, "Urbanization Index can not be calculated because it needs at least two pictures!", "PCA needs at least two pictures", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }


            bool calc = true;
            if (actproject.PCAState == Project.PCAStates.OK)
            {
                DialogResult ans = MessageBox.Show("The result has already been calculated. Do you want to calculate it anyway?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                calc = (ans == DialogResult.Yes ? true : false);
            }

            if (calc)
            {
                formcalc = new Calc();
                formcalc.Show();
                PCAWorker.RunWorkerAsync();
            }
        }

        private void listBoxResult_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxResult.SelectedItem != null)
            {
                TerritoryForPCAResult tfpr = (TerritoryForPCAResult)listBoxResult.SelectedItem;
                pictureBoxResult.Image = tfpr.territory.SatellitePic;
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
                registry.SetValue("ProxyEnable", checkBoxProxy.Checked ? 1 : 0);
                registry.SetValue("ProxyServer", textBoxAddress.Text + ":" + textBoxPort.Text);
            }
            catch (Exception)
            {
                MessageBox.Show(this, "Error while trying to set the proxy. Please consider using system proxy instead of this!", "Proxy error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        
        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (textBoxName.Text != "" && textBoxLat.Text != "" && textBoxLng.Text != "")
            {
                try
                {
                    int id = actproject.NextTerritoryID;
                    double lat = double.Parse(textBoxLat.Text.Replace(',', '.'), ci);
                    double lng = double.Parse(textBoxLng.Text.Replace(',', '.'), ci);
                    Territory territory = new Territory(id, lat, lng, textBoxName.Text, actproject.DataPath);

                    actproject.Territories.Add(id, territory);
                    listBoxTerritories.Items.Add(territory);
                    listBoxcsv.Items.Add(territory);

                    actproject.NextTerritoryID++;
                    actproject.TerritoryState = Project.TerritoryStates.Modified;
                    actproject.PCAState = Project.PCAStates.NeedsRecalc;

                    actproject.Save();
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "One of the given values are incorrect!", "Incorrect value(s)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show(this, "Give all data for territory!", "Missing data(s)!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes != MessageBox.Show(this, "Are you sure?", "Confirmation needed", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                return;

            if (listBoxcsv.SelectedItem != null)
            {
                Territory territory = (Territory)listBoxcsv.SelectedItem;

                if (Directory.Exists(territory.DirPath))
                    Directory.Delete(territory.DirPath, true);

                actproject.Territories.Remove(territory.ID);
                listBoxcsv.Items.Remove(territory);
                listBoxTerritories.Items.Remove(territory);

                actproject.PCAState = Project.PCAStates.NeedsRecalc;
                actproject.TerritoryState = Project.TerritoryStates.Modified;
                actproject.ReCalculateNextTerrID();

                actproject.Save();
            }
        }

        private void buttonClearAll_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes != MessageBox.Show(this, "Are you sure?", "Confirmation needed", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                return;

            foreach (var item in actproject.Territories)
            {
                if (Directory.Exists(item.Value.DirPath))
                    Directory.Delete(item.Value.DirPath, true);
            }

            actproject.Territories.Clear();
            actproject.NextTerritoryID = 1;
            actproject.PCAState = Project.PCAStates.OK;
            actproject.TerritoryState = Project.TerritoryStates.Modified;

            listBoxcsv.Items.Clear();
            listBoxTerritories.Items.Clear();
            listBoxResult.Items.Clear();

            actproject.Save();
        }

        private void buttonConv_Click(object sender, EventArgs e)
        {
            var new_form = new angle_converter.angle_converter();
            DialogResult rs = new_form.ShowDialog();
            if (rs == DialogResult.OK)
            {
                textBoxLat.Text = new_form.getLatResult().ToString();
                textBoxLng.Text = new_form.getLongResult().ToString();
            }
        }

        private void calculate_DoWork(object sender, DoWorkEventArgs e)
        {
            byte[] array = Encoding.ASCII.GetBytes(actproject.DataPath + '\\');
            ImageProcModule(array);

            if (NewSVM)
            {
                if (svm(array) == 0)
                    actproject.LoadSVMResults();

                if (pca(array) == 0)
                    actproject.LoadPCAResults(); // This will modify pcaState.csv automatically
            }
            else
            {
                if (svm(array) == 0)
                {
                    actproject.LoadSVMResults();
                    actproject.LoadPCAResults(); // This will modify pcaState.csv automatically

                    // Convert first line to english
                    actproject.SVMState = Project.SVMStates.Modified;
                    actproject.Save();
                }
            }
        }

        private void calculate_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            formcalc.Close();
            ribbonPanel8.Visible = false;

            listBoxResult.Items.Clear();
            if (actproject.PCAState == Project.PCAStates.OK)
            {
                List<TerritoryForPCAResult> tempList = new List<TerritoryForPCAResult>();
                foreach (var item in actproject.Territories)
                    tempList.Add(new TerritoryForPCAResult(item.Value));

                tempList.Sort();

                foreach (TerritoryForPCAResult item in tempList)
                    listBoxResult.Items.Add(item);
            }

            if (listBoxResult.Items.Count != 0)
                listBoxResult.SelectedIndex = 0;

            ribbonBtnResult_Click(null, null);
        }

        private void ColoredRows(object sender, DrawItemEventArgs e)
        {
            TrainingPoint tp = (TrainingPoint)listBoxType.Items[e.Index];
            e.DrawBackground();
            Graphics g = e.Graphics;

            // draw the text of the list item, not doing this will only show
            // the background color
            // you will need to get the text of item to display
            g.DrawString(tp.ToString(), e.Font, new SolidBrush(TrainingPoint.GetColorForTP(tp)), new PointF(e.Bounds.X, e.Bounds.Y));

            e.DrawFocusRectangle();
        }

        private void ShowProjectData(object sender, EventArgs e)
        {
            if (actproject == null)
            {
                MessageBox.Show(this, "Create or load project!", "No project", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            tabControlUrb.SelectedIndex = 1;
        }

        private void ExportButtonClick(object sender, EventArgs e)
        {
            if (actproject == null)
            {
                MessageBox.Show(this, "Create or load project!", "No project", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            if (!File.Exists(actproject.DataPath + "\\out.csv"))
            {
                MessageBox.Show(this, "Please calculate results first!", "Results needed", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            exportCheckBoxList.Items.Clear();
            List<TerritoryForPCAResult> tempList = new List<TerritoryForPCAResult>();

            foreach (var item in actproject.Territories)
            {
                if (item.Value.SatellitePic != null && item.Value.SatelliteGridPic != null && item.Value.MapPic != null && item.Value.MapGridPic != null && item.Value.TrainingPoints.Count != 0 && File.Exists(item.Value.DirPath + "\\04.bmp"))
                {
                    tempList.Add(new TerritoryForPCAResult(item.Value));
                }
            }

            tempList.Sort();
            foreach (var item in tempList)
                exportCheckBoxList.Items.Add(item);

            tabControlUrb.SelectedIndex = 5;
        }

        private void selectAllButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < exportCheckBoxList.Items.Count; i++)
            {
                exportCheckBoxList.SetItemChecked(i, true);
            }
        }

        private void selectNoneButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < exportCheckBoxList.Items.Count; i++)
            {
                exportCheckBoxList.SetItemChecked(i, false);
            }
        }

        private void ButtonStartExport_Click(object sender, EventArgs e)
        {
            bool ok = false;
            for (int i = 0; i < exportCheckBoxList.Items.Count; i++)
            {
                if (exportCheckBoxList.GetItemChecked(i))
                {
                    ok = true;
                    break;
                }
            }

            if (!ok)
            {
                MessageBox.Show(this, "Please select the location(s) you would like to export!", "No selected locations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }


            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Filter = "UrbIndex Export File (*.zip)|*.zip";
            dlg.RestoreDirectory = true;

            Stream myStream;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = dlg.OpenFile()) != null)
                {
                    myStream.Close();

                    ExportWorker.RunWorkerAsync(dlg.FileName);
                    LoadingButton3.Visible = true;
                    foreach (Control ctl in exportTab.Controls) ctl.Enabled = false;
                }
            }
        }

        private void ExportCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (Control ctl in exportTab.Controls) ctl.Enabled = true;
            LoadingButton3.Visible = false;
            if (e.Error != null)
            {
                if (e.Error.Message == "1")
                    MessageBox.Show(this, "Please calculate results first!", "Results needed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show(this, "The export has been failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(this, "Export succeed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Exporting(object sender, DoWorkEventArgs e)
        {
            ClearDirectory("temp");

            Dictionary<string, List<string>> TerritoriesOutData = new Dictionary<string, List<string>>();
            for (int i = 0; i < exportCheckBoxList.Items.Count; i++)
            {
                if (exportCheckBoxList.GetItemChecked(i))
                {
                    Territory territory = (Territory)((TerritoryForPCAResult)exportCheckBoxList.Items[i]).territory;

                    TerritoriesOutData.Add(territory.ID.ToString(), new List<string>());

                    Directory.CreateDirectory("temp\\" + territory.ID.ToString());
                }
            }

            if (TerritoriesOutData.Count == 0)
            {
                throw new Exception("1");
            }

            using (StreamReader sr = new StreamReader(actproject.DataPath + "\\out.csv"))
            {
                sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    try
                    {
                        string line = sr.ReadLine();
                        int last = line.LastIndexOf('\\');
                        int from = line.LastIndexOf('\\', last - 1) + 1;
                        string id = line.Substring(from, last - from);

                        if (TerritoriesOutData.ContainsKey(id))
                            TerritoriesOutData[id].Add(line);
                    }
                    catch (Exception) { }
                }
            }


            bool missingData = false;
            foreach (var item in TerritoriesOutData)
            {
                if (item.Value.Count != 100)
                {
                    missingData = true;
                    break;
                }
            }

            if (missingData)
            {
                throw new Exception("1");
            }

            using (ZipFile zip = new ZipFile())
            {
                using (StreamWriter dataFile = new StreamWriter("temp\\data.csv"))
                {
                    dataFile.WriteLine(actproject.Name);

                    for (int i = 0; i < exportCheckBoxList.Items.Count; i++)
                    {
                        if (exportCheckBoxList.GetItemChecked(i))
                        {
                            Territory territory = (Territory)((TerritoryForPCAResult)exportCheckBoxList.Items[i]).territory;

                            zip.AddFile(territory.DirPath + "\\01.bmp", territory.ID.ToString());
                            zip.AddFile(territory.DirPath + "\\01_grid.bmp", territory.ID.ToString());
                            zip.AddFile(territory.DirPath + "\\02.bmp", territory.ID.ToString());
                            zip.AddFile(territory.DirPath + "\\02_grid.bmp", territory.ID.ToString());
                            zip.AddFile(territory.DirPath + "\\03.txt", territory.ID.ToString());
                            zip.AddFile(territory.DirPath + "\\04.bmp", territory.ID.ToString());

                            // Generate teaching.csv
                            string id = territory.Latitude.ToString() + "|" + territory.Longitude.ToString();
                            List<string> outData = TerritoriesOutData[territory.ID.ToString()];

                            using (StreamWriter wr = new StreamWriter("temp\\" + territory.ID.ToString() + " \\teaching.csv"))
                            {
                                wr.WriteLine("Image_name;Cell_id;RgbMean_r;RgbMean_g;RgbMean_b;Canny_thresh_10;Canny_thresh_15;Canny_thresh_20;Canny_thresh_25;Canny_thresh_30;Canny_thresh_35;Canny_thresh_40;Canny_thresh_45;Canny_thresh_50;Canny_thresh_55;Canny_thresh_60;Canny_thresh_65;Canny_thresh_70;Canny_thresh_75;Canny_thresh_80;Canny_thresh_85;Canny_thresh_90;Canny_thresh_95;Canny_thresh_100;Mean;Modus;Mean_dev;Modus_dev;Harris_Q0.02_D5;Harris_Q0.02_D10;Harris_Q0.02_D20;Harris_Q0.16_D5;Harris_Q0.16_D10;Harris_Q0.16_D20;Road;Main_road;Laws_W7_b;Laws_W7_o;Laws_W7_r;Laws_W7_v_r;Laws_W7_v_u;Laws_W11_b;Laws_W11_o;Laws_W11_r;Laws_W11_v_r;Laws_W11_v_u;Laws_W15_b;Laws_W15_o;Laws_W15_r;Laws_W15_v_r;Laws_W15_v_u;Laws_W31_b;Laws_W31_o;Laws_W31_r;Laws_W31_v_r;Laws_W31_v_u;b;v;r");

                                for (int j = 0; j < outData.Count; j++)
                                {
                                    int semicol = outData[j].IndexOf(';');
                                    wr.Write(id);
                                    wr.Write(outData[j].Substring(semicol));
                                    wr.Write(';');
                                    wr.Write(territory.SVMResults[j].Building);
                                    wr.Write(';');
                                    wr.Write(territory.SVMResults[j].Vegetation);
                                    wr.Write(';');
                                    wr.Write(territory.SVMResults[j].Road);
                                    wr.Write(Environment.NewLine);
                                }
                            }

                            zip.AddFile("temp\\" + territory.ID.ToString() + " \\teaching.csv", territory.ID.ToString());

                            dataFile.WriteLine(territory.ID.ToString() + ';' + territory.Latitude + ';' + territory.Longitude + ';' + CsvHelper.ToCsvData(territory.Name));
                        }
                    }
                }
                zip.AddFile("temp\\data.csv", "/");
                zip.Save((string)e.Argument);

                // Clear temp directory..
                ClearDirectory("temp");
            }
        }

        private void ImportButtonClick(object sender, EventArgs e)
        {
            /*
            if (actproject == null)
            {
                MessageBox.Show(this, "Create or load project!", "No project", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            */

            // Create OpenFileDialog 
            OpenFileDialog dlg = new OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.Filter = "UrbIndex Import File (*.zip)|*.zip";

            // Display OpenFileDialog by calling ShowDialog method 
            DialogResult result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == DialogResult.OK)
            {
                if (actproject == null)
                {
                    ClearDirectory("tempproject");
                    if (File.Exists("tempproject.csv"))
                        File.Delete("tempproject.csv");
                    File.Create("tempproject.csv").Close();
                    actproject = Project.Load("tempproject.csv");
                }

                ImportFilePath = dlg.FileName;
                ZipExtractWorker.RunWorkerAsync(dlg.FileName);
                LoadingButton3.Visible = true;
            }
        }

        private void ImportCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LoadingButton3.Visible = false;
            foreach (Control ctl in importTab.Controls) ctl.Enabled = true;

            if (e.Error != null)
            {
                MessageBox.Show(this, "The import has been failed!", "Import fail!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            List<Territory> importedTerritories = (List<Territory>)e.Result;
            foreach (Territory territory in importedTerritories)
            {
                listBoxTerritories.Items.Add(territory);
                listBoxcsv.Items.Add(territory);
            }

            MessageBox.Show(this, "Import succeed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Importing(object sender, DoWorkEventArgs e)
        {
            ClearDirectory("temp");

            List<ImportData> importData = (List<ImportData>)dataGridView1.DataSource;
            SortedDictionary<int, ImportData> importIDAndData = new SortedDictionary<int, ImportData>();
            foreach (ImportData item in importData)
            {
                if (item.Import)
                    importIDAndData.Add(item.getID(), item);
            }

            string rootDir = string.Empty;
            bool rootDirSet = false;
            using (ZipFile zip = ZipFile.Read(ImportFilePath))
            {
                foreach (ZipEntry item in zip.Entries)
                {
                    string[] data = item.FileName.Split(new char[] { '/' });
                    if (data.Length == 3) // webFile
                    {
                        if (!rootDirSet)
                        {
                            rootDir = data[0];
                            rootDirSet = true;
                        }

                        try
                        {
                            int id = int.Parse(data[1]);
                            if (importIDAndData.ContainsKey(id))
                            {
                                item.Extract("temp");
                            }
                        }
                        catch (Exception) { }
                    }
                    else if (data.Length == 2)
                    {
                        try
                        {
                            int id = int.Parse(data[0]);
                            if (importIDAndData.ContainsKey(id))
                            {
                                item.Extract("temp");
                            }
                        }
                        catch (Exception) { }
                    }
                }
            }

            if (rootDir != string.Empty)
                rootDir += '\\';

            List<Territory> importedTerritories = new List<Territory>();
            foreach (var item in importIDAndData)
            {
                // Move files
                string fromDir = "temp\\" + rootDir + item.Value.getID().ToString();
                string toDir = actproject.DataPath + "\\" + actproject.NextTerritoryID.ToString();

                if (!Directory.Exists(toDir))
                    Directory.CreateDirectory(toDir);
                else
                    ClearDirectory(toDir);

                File.Move(fromDir + "\\01.bmp", toDir + "\\01.bmp");
                File.Move(fromDir + "\\01_grid.bmp", toDir + "\\01_grid.bmp");
                File.Move(fromDir + "\\02.bmp", toDir + "\\02.bmp");
                File.Move(fromDir + "\\02_grid.bmp", toDir + "\\02_grid.bmp");
                File.Move(fromDir + "\\03.txt", toDir + "\\03.txt");
                File.Move(fromDir + "\\04.bmp", toDir + "\\04.bmp");

                // Create territory
                int id = actproject.NextTerritoryID;
                double lat = item.Value.Latitude;
                double lng = item.Value.Longitude;
                Territory territory = new Territory(id, lat, lng, item.Value.Name, actproject.DataPath);

                actproject.Territories.Add(id, territory);
                importedTerritories.Add(territory);

                actproject.NextTerritoryID++;

                // Load teaching.csv
                string[] teachingLines = File.ReadAllLines(fromDir + "\\teaching.csv");

                // Save SVM values
                List<string[]> teachingLineData = new List<string[]>();
                for (int i = 1; i < teachingLines.Length; i++)
                {
                    teachingLineData.Add(teachingLines[i].Split(new char[] { ';' }));
                    string[] data = teachingLineData[i - 1];

                    territory.SVMResults[i - 1].Building = int.Parse(data[data.Length - 3]);
                    territory.SVMResults[i - 1].Vegetation = int.Parse(data[data.Length - 2]);
                    territory.SVMResults[i - 1].Road = int.Parse(data[data.Length - 1]);
                }

                // Append data to out.csv
                bool outCsvCreated = false;
                if (!File.Exists(actproject.DataPath + "\\out.csv"))
                {
                    File.Create(actproject.DataPath + "\\out.csv").Close();
                    outCsvCreated = true;
                }

                using (StreamWriter sw = new StreamWriter(actproject.DataPath + "\\out.csv", true))
                {
                    if (outCsvCreated)
                        sw.WriteLine("Image_name;Cell_id;RgbMean_r;RgbMean_g;RgbMean_b;Canny_thresh_10;Canny_thresh_15;Canny_thresh_20;Canny_thresh_25;Canny_thresh_30;Canny_thresh_35;Canny_thresh_40;Canny_thresh_45;Canny_thresh_50;Canny_thresh_55;Canny_thresh_60;Canny_thresh_65;Canny_thresh_70;Canny_thresh_75;Canny_thresh_80;Canny_thresh_85;Canny_thresh_90;Canny_thresh_95;Canny_thresh_100;Mean;Modus;Mean_dev;Modus_dev;Harris_Q0.02_D5;Harris_Q0.02_D10;Harris_Q0.02_D20;Harris_Q0.16_D5;Harris_Q0.16_D10;Harris_Q0.16_D20;Road;Main_road;Laws_W7_b;Laws_W7_o;Laws_W7_r;Laws_W7_v_r;Laws_W7_v_u;Laws_W11_b;Laws_W11_o;Laws_W11_r;Laws_W11_v_r;Laws_W11_v_u;Laws_W15_b;Laws_W15_o;Laws_W15_r;Laws_W15_v_r;Laws_W15_v_u;Laws_W31_b;Laws_W31_o;Laws_W31_r;Laws_W31_v_r;Laws_W31_v_u");


                    foreach (string[] data in teachingLineData)
                    {
                        sw.Write(new FileInfo(toDir + "\\01.bmp").FullName);
                        sw.Write(';');
                        for (int i = 1; i < data.Length - 3; i++)
                        {
                            sw.Write(data[i]);
                            if (i + 1 < data.Length - 3)
                                sw.Write(';');
                        }
                        sw.Write(Environment.NewLine);
                    }
                }

                // Append to teaching.csv
                if (item.Value.Teaching)
                {
                    string[] outData = new string[100];
                    for (int i = 1; i < teachingLines.Length; i++)
                        outData[i - 1] = teachingLines[i];

                    GlobalManager.SaveIntoTeachingSet(actproject, territory, outData);
                }
            }

            actproject.TerritoryState = Project.TerritoryStates.Modified;
            actproject.SVMState = Project.SVMStates.Modified;
            actproject.PCAState = Project.PCAStates.NeedsRecalc;
            actproject.Save();

            ClearDirectory("temp");

            e.Result = importedTerritories;
        }

        private void ZipExtractCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LoadingButton3.Visible = false;

            if (e.Error != null)
            {
                if (e.Error.Message == "1")
                    MessageBox.Show(this, "The selected zip file was wrong! Please check if you selected the correct zip file to import!", "Wrong zip file!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show(this, "The import has been failed!", "Import fail!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            dataGridView1.DataSource = e.Result;
            dataGridView1.Columns[0].ReadOnly = true;
            dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns[1].ReadOnly = true;
            dataGridView1.Columns[2].ReadOnly = true;

            tabControlUrb.SelectedIndex = 6;
        }

        private void ZipExtracting(object sender, DoWorkEventArgs e)
        {
            // Clear temp directory..
            ClearDirectory("temp");

            string dataFile = string.Empty;
            try
            {
                using (ZipFile zip = ZipFile.Read((string)e.Argument))
                {
                    foreach (ZipEntry item in zip.Entries)
                    {
                        if (item.FileName.Contains("data.csv"))
                        {
                            item.Extract("temp");
                            dataFile = item.FileName;
                            break;
                        }
                    }
                }
            }
            catch (Exception) { }

            if (dataFile == string.Empty)
            {
                throw new Exception("1");
            }

            List<ImportData> importData = new List<ImportData>();

            string[] dataLines = File.ReadAllLines("temp\\" + dataFile);
            bool webFile = CsvHelper.Separate(dataLines[0], ';').Count == 4;
            int i = 0;
            foreach (string line in dataLines)
            {
                if (webFile && i % 2 == 1)
                {
                    i++;
                    continue;
                }

                try
                {
                    List<string> data = CsvHelper.Separate(line, ';');
                    data[1] = data[1].Replace(',', '.');
                    data[2] = data[2].Replace(',', '.');
                    importData.Add(new ImportData(int.Parse(data[0]), data[3], double.Parse(data[1], ci), double.Parse(data[2], ci)));
                }
                catch (Exception) { }

                i++;
            }

            ClearDirectory("temp");

            e.Result = importData;
        }

        private void ClearDirectory(string dirname)
        {
            System.IO.DirectoryInfo tempinfo = new DirectoryInfo(dirname);

            foreach (FileInfo file in tempinfo.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in tempinfo.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        private void ButtonStartImport_Click(object sender, EventArgs e)
        {
            bool ok = false;
            List<ImportData> importData = (List<ImportData>)dataGridView1.DataSource;
            foreach (ImportData item in importData)
            {
                if (item.Import)
                {
                    ok = true;
                    break;
                }
            }

            if (!ok)
            {
                MessageBox.Show(this, "Please select the location(s) you would like to import!", "No selected locations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            LoadingButton3.Visible = true;
            foreach (Control ctl in importTab.Controls) ctl.Enabled = false;
            ImportWorker.RunWorkerAsync();
        }

        private void SelectAllToImportButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
                row.Cells[3].Value = true;
        }

        private void SelectNoneToImportButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
                row.Cells[3].Value = false;
        }

        private void SelectAllToTeachingButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
                row.Cells[4].Value = true;
        }

        private void SelectNoneToTeachingButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
                row.Cells[4].Value = false;
        }
    }
}