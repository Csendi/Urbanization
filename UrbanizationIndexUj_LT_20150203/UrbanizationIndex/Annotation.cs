using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
//using UrbanizationIndex.SVM;

namespace UrbanizationIndex
{
    public partial class Annotation : Form
    {
        Project actproject;
        int length = 10; // Row count, Column count
        Panel[] panels; // Panel for building, vegetation and road panels
        Button[,] pix; // Little colored buttons on the building, vegetation and road panels
        List<int> PrevSelectedCellIDs = new List<int>();
        int AnnotationValue = 0;
        int GridWidth, GridHeight;
        Graphics SatGraphics, MapGraphics;
        Pen SelectedPen, DefaultPen, UsedPen;
        BackgroundWorker LoadingWorker = new BackgroundWorker();
        Territory actTerritory = null;

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);

        public Annotation(Project actproject)
        {
            InitializeComponent();
            this.actproject = actproject;

            panels = new Panel[3];
            pix = new Button[length * length, panels.Length];

            SelectedPen = new Pen(Color.Red, 1);
            DefaultPen = new Pen(Color.White, 1);
            UsedPen = new Pen(Color.SpringGreen, 1);

            // Fill combobox with images
            List<TerritoryForPCAResult> tempList = new List<TerritoryForPCAResult>();
            foreach (var item in actproject.Territories)
                tempList.Add(new TerritoryForPCAResult(item.Value));

            tempList.Sort();
            foreach (TerritoryForPCAResult item in tempList)
                comboBox1.Items.Add(item);

            LoadingWorker.DoWork += Loading;
            LoadingWorker.RunWorkerCompleted += LoadingCompleted;
            LoadingWorker.RunWorkerAsync();

        }

        private void LoadingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            for (int i = 0; i < panels.Length; i++)
                this.Controls.Add(panels[i]);

            hScrollBar1.Maximum = actproject.Territories.Count;
            hScrollBar1.Value = 1;
            comboBox1.SelectedIndex = 0;

            GridWidth = pictureBox1.Width / length;
            GridHeight = pictureBox1.Height / length;
        }


        private void Loading(object sender, DoWorkEventArgs e)
        {
            // Load panels and their butttons
            for (int i = 0; i < panels.Length; i++)
            {
                this.panels[i] = new Panel();
                this.panels[i].Location = new Point(5 + i * 205, 60);
                this.panels[i].Size = new System.Drawing.Size(200, 200);

                // Load length x length buttons onto the panel
                for (int j = 0; j < length; j++)
                {
                    for (int k = 0; k < length; k++)
                    {
                        pix[j * 10 + k, i] = new Button();
                        pix[j * 10 + k, i].FlatStyle = FlatStyle.Flat;
                        pix[j * 10 + k, i].Location = new Point(k * 20, j * 20);
                        pix[j * 10 + k, i].Size = new Size(20, 20);
                        pix[j * 10 + k, i].FlatAppearance.BorderSize = 2;
                        pix[j * 10 + k, i].FlatAppearance.BorderColor = Color.Wheat;
                        pix[j * 10 + k, i].Tag = j * 10 + k;
                        pix[j * 10 + k, i].Click += PixClick;
                        pix[j * 10 + k, i].TabStop = false;

                        this.toolTip1.SetToolTip(this.pix[j * 10 + k, i], (j + 1).ToString() + "x" + (k + 1).ToString());
                        panels[i].Controls.Add(pix[j * 10 + k, i]);
                    }
                }
            }
        }

        private void PixClick(object sender, EventArgs e)
        {
            int cellID = (int)((Button)sender).Tag;

            SelectPartOfImage(cellID);
        }


        private void FreeSelection()
        {
            if (actTerritory == null)
                return;

            int picPartWidthSat = actTerritory.SatellitePic.Width / length;
            int picPartHeightSat = actTerritory.SatellitePic.Height / length;
            int picPartWidthMap = actTerritory.MapPic.Width / length;
            int picPartHeightMap = actTerritory.MapPic.Height / length;

            foreach (int CellID in PrevSelectedCellIDs)
            {
                int X = CellID % length;
                int Y = CellID / length;

                if (satelliteRadio.Checked)
                    SatGraphics.DrawRectangle(DefaultPen, new Rectangle(X * picPartWidthSat, Y * picPartHeightSat, picPartWidthSat, picPartHeightSat));
                else
                    MapGraphics.DrawRectangle(DefaultPen, new Rectangle(X * picPartWidthMap, Y * picPartHeightMap, picPartWidthMap, picPartHeightMap));
            }

            if (PrevSelectedCellIDs.Count > 0)
            {
                pix[PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1], 0].FlatAppearance.BorderColor = Color.Wheat;
                pix[PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1], 1].FlatAppearance.BorderColor = Color.Wheat;
                pix[PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1], 2].FlatAppearance.BorderColor = Color.Wheat;
            }

            PrevSelectedCellIDs.Clear();
        }


        private void CopySelection()
        {
            if (actTerritory == null)
                return;

            int picPartWidthSat = actTerritory.SatellitePic.Width / length;
            int picPartHeightSat = actTerritory.SatellitePic.Height / length;
            int picPartWidthMap = actTerritory.MapPic.Width / length;
            int picPartHeightMap = actTerritory.MapPic.Height / length;

            int i = 0;
            foreach (int CellID in PrevSelectedCellIDs)
            {
                i++;

                int X = CellID % length;
                int Y = CellID / length;

                if (satelliteRadio.Checked)
                {
                    if (i < PrevSelectedCellIDs.Count)
                        SatGraphics.DrawRectangle(UsedPen, new Rectangle(X * picPartWidthSat, Y * picPartHeightSat, picPartWidthSat, picPartHeightSat));
                    else
                        SatGraphics.DrawRectangle(SelectedPen, new Rectangle(X * picPartWidthSat, Y * picPartHeightSat, picPartWidthSat, picPartHeightSat));
                    MapGraphics.DrawRectangle(DefaultPen, new Rectangle(X * picPartWidthMap, Y * picPartHeightMap, picPartWidthMap, picPartHeightMap));
                }
                else
                {
                    SatGraphics.DrawRectangle(DefaultPen, new Rectangle(X * picPartWidthSat, Y * picPartHeightSat, picPartWidthSat, picPartHeightSat));
                    if (i < PrevSelectedCellIDs.Count)
                        MapGraphics.DrawRectangle(UsedPen, new Rectangle(X * picPartWidthMap, Y * picPartHeightMap, picPartWidthMap, picPartHeightMap));
                    else
                        MapGraphics.DrawRectangle(SelectedPen, new Rectangle(X * picPartWidthMap, Y * picPartHeightMap, picPartWidthMap, picPartHeightMap));
                }
            }
        }



        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null)
                return;

            FreeSelection();
            actTerritory = ((TerritoryForPCAResult)comboBox1.SelectedItem).territory;

            if (actTerritory.SatellitePic == null)
            {
                MessageBox.Show(this, "The selected location has no images! Please download image first!", "No images!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                SatGraphics = null;
                MapGraphics = null;
                actTerritory = null;
                return;
            }

            pictureBox1.Image = actTerritory.SatelliteGridPic;
            pictureBox2.Image = actTerritory.MapGridPic;

            SatGraphics = Graphics.FromImage(pictureBox1.Image);
            MapGraphics = Graphics.FromImage(pictureBox2.Image);

            int i = 0;
            foreach (SVMResult res in actTerritory.SVMResults)
            {
                if (res.Building == 0)
                    pix[i, 0].BackColor = Color.White;
                else if (res.Building == 1)
                    pix[i, 0].BackColor = Color.Gray;
                else if (res.Building == 2)
                    pix[i, 0].BackColor = Color.Black;

                if (res.Vegetation == 0)
                    pix[i, 1].BackColor = Color.White;
                else if (res.Vegetation == 1)
                    pix[i, 1].BackColor = Color.Gray;
                else if (res.Vegetation == 2)
                    pix[i, 1].BackColor = Color.Black;

                if (res.Road == 0)
                    pix[i, 2].BackColor = Color.White;
                else if (res.Road == 1)
                    pix[i, 2].BackColor = Color.Black;
                else
                    pix[i, 2].BackColor = Color.Yellow;

                i++;
            }

            labelTerritory.Text = "Name: " + actTerritory.Name;
            labelTerritoryLat.Text = "Latitude: " + actTerritory.Latitude;
            labelTerritoryLng.Text = "Longitude: " + actTerritory.Longitude;

            hScrollBar1.Value = comboBox1.SelectedIndex + 1;
        }


        private void SelectPartOfImage(int cellID, bool forceAnnotation = false)
        {
            if (actTerritory == null)
                return;

            Graphics g = (satelliteRadio.Checked) ? SatGraphics : MapGraphics;

            if (g == null)
                return;

            int X = cellID % length;
            int Y = cellID / length;

            if (PrevSelectedCellIDs.Count == 0 || PrevSelectedCellIDs.Count > 0 && cellID != PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1])
            {
                int picPartWidthSat = actTerritory.SatellitePic.Width / length;
                int picPartHeightSat = actTerritory.SatellitePic.Height / length;
                int picPartWidthMap = actTerritory.MapPic.Width / length;
                int picPartHeightMap = actTerritory.MapPic.Height / length;

                pictureBox4.Image = actTerritory.SatellitePic.Clone(new Rectangle(X * picPartWidthSat, Y * picPartHeightSat, picPartWidthSat, picPartHeightSat), actTerritory.SatellitePic.PixelFormat);
                pictureBox3.Image = actTerritory.MapPic.Clone(new Rectangle(X * picPartWidthMap, Y * picPartHeightMap, picPartWidthMap, picPartHeightMap), actTerritory.MapPic.PixelFormat);

                if (PrevSelectedCellIDs.Count > 0)
                {
                    int PrevX = PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1] % length;
                    int PrevY = PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1] / length;

                    if (satelliteRadio.Checked)
                        g.DrawRectangle(UsedPen, new Rectangle(PrevX * picPartWidthSat, PrevY * picPartHeightSat, picPartWidthSat, picPartHeightSat));
                    else
                        g.DrawRectangle(UsedPen, new Rectangle(PrevX * picPartWidthMap, PrevY * picPartHeightMap, picPartWidthMap, picPartHeightMap));

                    pix[PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1], 0].FlatAppearance.BorderColor = Color.Wheat;
                    pix[PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1], 1].FlatAppearance.BorderColor = Color.Wheat;
                    pix[PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1], 2].FlatAppearance.BorderColor = Color.Wheat;
                }

                PrevSelectedCellIDs.Remove(cellID);
                PrevSelectedCellIDs.Add(cellID);

                if (satelliteRadio.Checked)
                    g.DrawRectangle(SelectedPen, new Rectangle(X * picPartWidthSat, Y * picPartHeightSat, picPartWidthSat, picPartHeightSat));
                else
                    g.DrawRectangle(SelectedPen, new Rectangle(X * picPartWidthMap, Y * picPartHeightMap, picPartWidthMap, picPartHeightMap));

                pix[cellID, 0].FlatAppearance.BorderColor = SelectedPen.Color;
                pix[cellID, 1].FlatAppearance.BorderColor = SelectedPen.Color;
                pix[cellID, 2].FlatAppearance.BorderColor = SelectedPen.Color;

                if (satelliteRadio.Checked)
                    pictureBox1.Refresh();
                else
                    pictureBox2.Refresh();
            }
            
            if (annotationCheckBox.Checked || forceAnnotation)
            {
                if (buildingRadio.Checked)
                {
                    actTerritory.SVMResults[cellID].Building = AnnotationValue;
                    if (AnnotationValue == 0)
                        pix[cellID, 0].BackColor = Color.White;
                    else if (AnnotationValue == 1)
                        pix[cellID, 0].BackColor = Color.Gray;
                    else if (AnnotationValue == 2)
                        pix[cellID, 0].BackColor = Color.Black;
                }
                else if (vegetationRadio.Checked)
                {
                    actTerritory.SVMResults[cellID].Vegetation = AnnotationValue;
                    if (AnnotationValue == 0)
                        pix[cellID, 1].BackColor = Color.White;
                    else if (AnnotationValue == 1)
                        pix[cellID, 1].BackColor = Color.Gray;
                    else if (AnnotationValue == 2)
                        pix[cellID, 1].BackColor = Color.Black;

                }
                else if (roadRadio.Checked)
                {
                    if (AnnotationValue == 2)
                    {
                        actTerritory.SVMResults[cellID].Road = 1;
                        pix[cellID, 2].BackColor = Color.Black;
                    }
                    else
                    {
                        actTerritory.SVMResults[cellID].Road = 0;
                        pix[cellID, 2].BackColor = Color.White;
                    }
                }

                actproject.SVMState = Project.SVMStates.Modified;
                actproject.PCAState = Project.PCAStates.NeedsRecalc;
            }
        }

        
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            SelectPartOfImage(e.Y / GridHeight * length + e.X / GridWidth);
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            label4.Text = hScrollBar1.Value.ToString();
            comboBox1.SelectedIndex = hScrollBar1.Value - 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (actTerritory == null)
                return;

            //tanitoponthoz adjuk-e?
            DialogResult dialogResult = MessageBox.Show("Do you want to add the changes to the teaching set ?", "Teaching set", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                try
                {
                    int counter = 0;

                    string[] outData = new string[100];
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
                                int id = int.Parse(line.Substring(from, last - from));

                                if (id == actTerritory.ID)
                                {
                                    outData[counter++] = line;

                                    if (counter == outData.Length)
                                        break;
                                }
                            }
                            catch (Exception) { }
                        }
                    }

                    if (counter != outData.Length)
                        return;

                    GlobalManager.SaveIntoTeachingSet(actproject, actTerritory, outData);

                    MessageBox.Show(this, "Teaching set was updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "The operation has been failed!", "Fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool CapsLock = (((ushort)GetKeyState(0x14)) & 0xffff) != 0;

            if (keyData == Keys.B)
            {
                buildingRadio.Checked = true;
                return true;
            }
            else if (keyData == Keys.V)
            {
                vegetationRadio.Checked = true;
                return true;
            }
            else if (keyData == Keys.R)
            {
                roadRadio.Checked = true;
                return true;
            }
            else if (keyData == Keys.Enter || keyData == Keys.Space)
            {
                if (PrevSelectedCellIDs.Count > 0)
                    SelectPartOfImage(PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1], true);
                return true;
            }
            else if (keyData == Keys.A)
            {
                annotationCheckBox.Checked = !annotationCheckBox.Checked;
                return true;
            }
            else if (keyData == Keys.M)
            {
                mapRadio.Checked = true;
                mapRadio_Click(null, null);
                return true;
            }
            else if (keyData == Keys.S)
            {
                satelliteRadio.Checked = true;
                satelliteRadio_Click(null, null);
                return true;
            }
            else if (keyData == Keys.D0 || keyData == Keys.NumPad0)
            {
                btnAnnotationVal0_Click(null, null);
                return true;
            }
            else if (keyData == Keys.D1 || keyData == Keys.NumPad1)
            {
                btnAnnotationVal1_Click(null, null);
                return true;
            }
            else if (keyData == Keys.D2 || keyData == Keys.NumPad2)
            {
                btnAnnotationVal2_Click(null, null);
                return true;
            }
            else if (CapsLock && keyData == Keys.Right)
            {
                if (PrevSelectedCellIDs.Count == 0 || PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1] == 99)
                    SelectPartOfImage(0);
                else
                    SelectPartOfImage(PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1] + 1);
                return true;
            }
            else if (CapsLock && keyData == Keys.Left)
            {
                if (PrevSelectedCellIDs.Count == 0)
                    SelectPartOfImage(0);
                else if (PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1] == 0)
                    SelectPartOfImage(99);
                else
                    SelectPartOfImage(PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1] - 1);
                return true;
            }
            else if (CapsLock && keyData == Keys.Up)
            {
                if (PrevSelectedCellIDs.Count == 0)
                    SelectPartOfImage(0);
                else
                {
                    if (PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1] - 10 < 0)
                        SelectPartOfImage(100 + (PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1] - 10));
                    else
                        SelectPartOfImage(PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1] - 10);
                }
                return true;
            }
            else if (CapsLock && keyData == Keys.Down)
            {
                if (PrevSelectedCellIDs.Count == 0)
                    SelectPartOfImage(0);
                else
                {
                    if (PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1] + 10 > 99)
                        SelectPartOfImage(PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1] + 10 - 100);
                    else
                        SelectPartOfImage(PrevSelectedCellIDs[PrevSelectedCellIDs.Count - 1] + 10);
                }
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            FreeSelection();
            actproject.Save();
        }

        private void btnAnnotationVal0_Click(object sender, EventArgs e)
        {
            AnnotationValue = 0;

            btnAnnotationVal0.FlatAppearance.BorderColor = Color.Red;
            btnAnnotationVal1.FlatAppearance.BorderColor = Color.White;
            btnAnnotationVal2.FlatAppearance.BorderColor = Color.White;
        }

        private void btnAnnotationVal1_Click(object sender, EventArgs e)
        {
            if (roadRadio.Checked)
                return;

            AnnotationValue = 1;

            btnAnnotationVal0.FlatAppearance.BorderColor = Color.White;
            btnAnnotationVal1.FlatAppearance.BorderColor = Color.Red;
            btnAnnotationVal2.FlatAppearance.BorderColor = Color.White;
        }

        private void btnAnnotationVal2_Click(object sender, EventArgs e)
        {
            AnnotationValue = 2;

            btnAnnotationVal0.FlatAppearance.BorderColor = Color.White;
            btnAnnotationVal1.FlatAppearance.BorderColor = Color.White;
            btnAnnotationVal2.FlatAppearance.BorderColor = Color.Red;
        }

        private void mapRadio_Click(object sender, EventArgs e)
        {
            pictureBox1.Visible = false;
            pictureBox2.Visible = true;

            CopySelection();
        }

        private void satelliteRadio_Click(object sender, EventArgs e)
        {
            pictureBox1.Visible = true;
            pictureBox2.Visible = false;

            CopySelection();
        }

        private void roadRadio_Click(object sender, EventArgs e)
        {
            if (AnnotationValue == 1)
            {
                AnnotationValue = 0;

                btnAnnotationVal0.FlatAppearance.BorderColor = Color.Red;
                btnAnnotationVal1.FlatAppearance.BorderColor = Color.White;
            }
        }

        private void buttonClearAll_Click(object sender, EventArgs e)
        {
            FreeSelection();

            if (satelliteRadio.Checked)
                pictureBox1.Refresh();
            else
                pictureBox2.Refresh();
        }
    }
}