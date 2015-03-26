using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
//using Emgu.CV;
//using Emgu.CV.Structure;
//using WindowsFormsApplication2.ImageProcess.Algoritmhs;
//using WindowsFormsApplication2ImageProcessAlgoritmhs;

namespace Kepfeldolgozas.ImageProcess
{
    class ImageProcModule
    {
        //az outfile
        private StringBuilder csv;

        //fajlkezeleshez kell
        private readonly string _rootPath;


        private List<string> _imageFolders;
        private List<string> _satImages;
        private List<string> _roadImages;
        private List<string> _strTextures;

        //csv fejlecei, sorrendben: RGB, Harris
        private List<string> RgbMeanHeader;
        private List<string> CannyHeader;
        private List<string> HarrisHeader;
        private List<string> HueHeader;
        private List<string> RoadHeader;
        private List<string> LawsHeader; 


        //private int _headerCounter;


        /// <summary>
        /// Initializes a new instance of the <see cref="ImageProcModule"/> class.
        /// </summary>
        /// <param name="rootPath">The root path.</param>
        public ImageProcModule(string rootPath)
        {
            csv = new StringBuilder();
            //root folder, és a Debug megadása
            _rootPath = rootPath;
            _imageFolders = new List<string>();
            _satImages = new List<string>();
            _roadImages = new List<string>();
            _strTextures = new List<string>();
  
            //_headerCounter = 0;
            //headerek , vagyis az output csv fejlece              
            HarrisHeader = new List<string>();
            RgbMeanHeader = new List<string>();
            CannyHeader = new List<string>();
            HueHeader=new List<string>();
            RoadHeader=new List<string>();
            LawsHeader=new List<string>();
            

        }

        /// <summary>
        /// Calculates this instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        /// Nem talalhato mappa
        /// or
        /// Error in the directory! Some files are missing!
        /// </exception>
        public int Calculate()
        {
            return 0;
            /*
            //check filPath and open fileReader
            if (!Directory.Exists(_rootPath))
            {
                Console.WriteLine("No directory exist!", _rootPath);
                throw new Exception("Nem talalhato mappa");
            }


            FillWorkingSets(_imageFolders, 0);
            FillWorkingSets(_satImages, 1);
            FillWorkingSets(_roadImages, 2);
            FillWorkingSets(_strTextures, 3);
            _satImages.Sort();
            _roadImages.Sort();

            //Szamolgatni az ertekeket

            //mind ugyan annyi-e ?
            int amount = _imageFolders.Count;

            if (_satImages.Count != amount ||
                _roadImages.Count != amount || _strTextures.Count != amount)
            {
                //tehat nem ugyanannyi a darabszamuk
                throw new Exception("Error in the directory! Some files are missing!");
            }

            for (int i = 0; i < _imageFolders.Count; i++)
            {
                Attributes imgAttributes = new Attributes();
                //kinyerem a 2 kepet
                Image<Bgr, Byte> imgSat = new Image<Bgr, Byte>(_satImages[i]);
                Image<Bgr, Byte> imgRoad = new Image<Bgr, Byte>(_roadImages[i]);


                //RGB Begin, buffer nem kotele
               

                RGBMean rgb = new RGBMean();
                rgb.CalcRgbMean(imgSat, RgbMeanHeader, imgAttributes._arrRgbMean);
                //RGB End


                //Harris begin
          

                Harris h = new Harris();

                h.CalcHarris(imgSat, HarrisHeader, imgAttributes._arrHarris_Q002_D5, 0.02, 5.0);
                h.CalcHarris(imgSat, HarrisHeader, imgAttributes._arrHarris_Q002_D10, 0.02, 10.0);
                h.CalcHarris(imgSat, HarrisHeader, imgAttributes._arrHarris_Q002_D20, 0.02, 20.0);
                h.CalcHarris(imgSat, HarrisHeader, imgAttributes._arrHarris_Q016_D5, 0.16, 5.0);
                h.CalcHarris(imgSat, HarrisHeader, imgAttributes._arrHarris_Q016_D10, 0.16, 10.0);
                h.CalcHarris(imgSat, HarrisHeader, imgAttributes._arrHarris_Q016_D20, 0.16, 20.0);
                //Harris end

                //Canny begin
               
                Canny c = new Canny();
                CannyHeader = c.generateHeader();

                c.CalcCanny(imgSat, imgAttributes._cannyEdges);
                //Canny end

                //Hue begin
                HueHeader.Add("Mean");
                HueHeader.Add("Modus");
                HueHeader.Add("Mean_dev");
                HueHeader.Add("Modus_dev");

                Hue h2=new Hue();
                h2.CalcHueMeanAndModus(imgSat, imgAttributes._hue);

                //Hue end

                //Road begin
                RoadHeader.Add("Road");
                RoadHeader.Add("Main_road");
                Road r=new Road();
                r.CalcRoadPixels(imgRoad, _rootPath+"\\"+_imageFolders[i],imgAttributes._road);
                //Road end

                //laws begin
                Laws l=new Laws();
                //100as tarolo, minden kepkockara
                //List<List<int>> taroloLaws = new List<List<int>>(100);

                l.CalcLaws(imgSat, LawsHeader, _rootPath + "\\" + _imageFolders[i],_rootPath + "\\"+ _imageFolders[i]+"\\03.txt",imgAttributes._laws);



                //laws end

                //Grid begin
                Grid g = new Grid();
                g.DrawGrid(imgSat, imgRoad, _rootPath + "\\" + _imageFolders[i]);

                //Grid end

                if (_headerCounter < 1)
                {
                    SaveHeaders();
                    _headerCounter++;
                }

                SaveActualDatas(_satImages[i], imgAttributes);

            }


            //Debugba kiszorni a tartalmat, header-t is kiírni, de csak egyszer
           

            //kesz a process

            

            return 0;
            */
        }

        /// <summary>
        /// Saves the actual datas.
        /// </summary>
        /// <param name="satImage">The sat image.</param>
        /// <param name="imgAttributes">The img attributes.</param>
        /*
        private void SaveActualDatas(string satImage, Attributes imgAttributes)
        {
            string newLine = null;

            
           // var valami=imgAttributes._cannyEdges;
            //var valami2 = imgAttributes._cannyEdges[0];
           // var valami3 = imgAttributes._road;
            for (int i = 0; i < 100; i++)
            {

                newLine = satImage + ";" + i + ";" +
                          imgAttributes._arrRgbMean[i][0].ToString() + ";" +
                          imgAttributes._arrRgbMean[i][1].ToString() + ";" +
                          imgAttributes._arrRgbMean[i][2].ToString() + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[10], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[15], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[20], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[25], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[30], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[35], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[40], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[45], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[50], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[55], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[60], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[65], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[70], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[75], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[80], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[85], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[90], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[95], i) + ";" +
                          GetCannyValueByTresholdAndIndex(imgAttributes._cannyEdges[100], i) + ";" +
                          imgAttributes._hue[i][0] + ";" +
                          imgAttributes._hue[i][1] + ";" +
                          imgAttributes._hue[i][2] + ";" +
                          imgAttributes._hue[i][3] + ";" +
                          SearchHarrisValue(i, imgAttributes._arrHarris_Q002_D5) + ";" +
                          SearchHarrisValue(i, imgAttributes._arrHarris_Q002_D10) + ";" +
                          SearchHarrisValue(i, imgAttributes._arrHarris_Q002_D20) + ";" +
                          SearchHarrisValue(i, imgAttributes._arrHarris_Q016_D5) + ";" +
                          SearchHarrisValue(i, imgAttributes._arrHarris_Q016_D10) + ";" +
                          SearchHarrisValue(i, imgAttributes._arrHarris_Q016_D20) + ";" +
                          imgAttributes._road[i][0] + ";" +
                          imgAttributes._road[i][1]+GetLawValues(imgAttributes._laws[i]);
csv.AppendLine(newLine);
            }
        }
        */

        /// <summary>
        /// Gets the law values.
        /// </summary>
        /// <param name="laws">The laws list.</param>
        /// <returns>new line string</returns>
        private string GetLawValues(List<int> laws)
        {
            string newLine = null;
            foreach (var law in laws)
            {
                newLine += ";"+law;
            }
            return newLine;
        }

        /// <summary>
        /// Gets the index of the canny value by treshold and.
        /// </summary>
        /// <param name="cannyEdge">The canny edge.</param>
        /// <param name="cellaID">The cella ID.</param>
        /// <returns>The Harris attributes list</returns>
        private string GetCannyValueByTresholdAndIndex(Dictionary<int, int> cannyEdge, int cellaID)
        {
            return cannyEdge[cellaID].ToString();
        }

        private string SearchHarrisValue(int i, Dictionary<int, int> arrHarrisQ002D5)
        {
            if (arrHarrisQ002D5.ContainsKey(i))
            {
                return arrHarrisQ002D5[i].ToString();
            }
            else
            {
                return "0";
            }

        }

        /// <summary>
        /// Saves the CSV.
        /// </summary>
        /// <param name="folder">The folder.</param>
        public void SaveCSV(string folder)
        {
            File.WriteAllText(folder+"\\out.csv", csv.ToString());
        }




        /// <summary>
        /// Fills the working sets.
        /// </summary>
        /// <param name="toFill">To fill.</param>
        /// <param name="i">The i., what to fill</param>
        private void FillWorkingSets(List<string> toFill, int i)
        {
            switch (i)
            {
                case 0:
                    DirectoryInfo dInfo = new DirectoryInfo(_rootPath);
                    DirectoryInfo[] subdirs = dInfo.GetDirectories();

                    foreach (DirectoryInfo directoryInfo in subdirs)
                    {
                        _imageFolders.Add(directoryInfo.ToString());

                        //ezzel megvan
                        //MessageBox.Show(directoryInfo.ToString());
                    }

                    break;

                case 1:
                    String[] satfiles = System.IO.Directory.GetFiles(_rootPath, "01.bmp", System.IO.SearchOption.AllDirectories);
                    foreach (string allfile in satfiles)
                    {
                        _satImages.Add(allfile);
                    }

                    //megvan ezis
                    //MessageBox.Show(_satImages[1]);
                    break;

                case 2:
                    String[] roadfiles = System.IO.Directory.GetFiles(_rootPath, "02.bmp", System.IO.SearchOption.AllDirectories);
                    foreach (string allfile in roadfiles)
                    {
                        _roadImages.Add(allfile);
                    }

                    //megvan ezis
                    //MessageBox.Show(_satImages[1]);
                    break;

                case 3:
                    String[] lawFiles = System.IO.Directory.GetFiles(_rootPath, "03.txt", System.IO.SearchOption.AllDirectories);
                    foreach (string allfile in lawFiles)
                    {
                        _strTextures.Add(allfile);
                    }

                    //megvan ezis
                    //MessageBox.Show(_satImages[1]);
                    break;
            }
        }



        /// <summary>
        /// Saves the headers.
        /// </summary>
        private void SaveHeaders()
        {
            //var csv = new StringBuilder();
            string newLine = null;

            //összes attribútum
            newLine = "Image_name;Cell_id";

            foreach (var VARIABLE in RgbMeanHeader)
            {
                newLine += (";" + VARIABLE);
            }

            foreach (var VARIABLE in CannyHeader)
            {
                newLine += (";" + VARIABLE);
            }

            foreach (var VARIABLE in HueHeader)
            {
                newLine += (";" + VARIABLE);
            }

            foreach (var VARIABLE in HarrisHeader)
            {
                newLine += (";" + VARIABLE);
            }

            foreach (var VARIABLE in RoadHeader)
            {
                newLine += (";" + VARIABLE);
            }

            foreach (var VARIABLE in LawsHeader)
            {
                newLine += (";" + VARIABLE);
            }

            newLine += ";b;v;r";
            csv.AppendLine(newLine);

            // File.WriteAllText("out.csv", csv.ToString());
        }

    }
}
