using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UrbanizationIndex
{
    /**
     * Int this class there are those things which may be used by other classes / windows
     */
    class GlobalManager
    {
        public static void SaveIntoTeachingSet(Project actproject, Territory actTerritory, string[] outData)
        {
            string[] teachingLines = File.ReadAllLines("teaching.csv");

            int counter = 0;
            bool start = false;
            bool done = false;
            string idstr = actTerritory.Latitude.ToString() + "|" + actTerritory.Longitude.ToString();

            using (StreamWriter wr = new StreamWriter("teaching.csv"))
            {
                wr.WriteLine("Image_name;Cell_id;RgbMean_r;RgbMean_g;RgbMean_b;Canny_thresh_10;Canny_thresh_15;Canny_thresh_20;Canny_thresh_25;Canny_thresh_30;Canny_thresh_35;Canny_thresh_40;Canny_thresh_45;Canny_thresh_50;Canny_thresh_55;Canny_thresh_60;Canny_thresh_65;Canny_thresh_70;Canny_thresh_75;Canny_thresh_80;Canny_thresh_85;Canny_thresh_90;Canny_thresh_95;Canny_thresh_100;Mean;Modus;Mean_dev;Modus_dev;Harris_Q0.02_D5;Harris_Q0.02_D10;Harris_Q0.02_D20;Harris_Q0.16_D5;Harris_Q0.16_D10;Harris_Q0.16_D20;Road;Main_road;Laws_W7_b;Laws_W7_o;Laws_W7_r;Laws_W7_v_r;Laws_W7_v_u;Laws_W11_b;Laws_W11_o;Laws_W11_r;Laws_W11_v_r;Laws_W11_v_u;Laws_W15_b;Laws_W15_o;Laws_W15_r;Laws_W15_v_r;Laws_W15_v_u;Laws_W31_b;Laws_W31_o;Laws_W31_r;Laws_W31_v_r;Laws_W31_v_u;b;v;r");
                for (int i = 1; i < teachingLines.Length; i++)
                {
                    if (!start)
                    {
                        if (!done && teachingLines[i].Length >= idstr.Length && teachingLines[i].Substring(0, idstr.Length) == idstr)
                            start = true;
                        else
                        {
                            wr.WriteLine(teachingLines[i]);
                            continue;
                        }
                    }

                    if (!done && start)
                    {
                        int semicol = outData[counter].IndexOf(';');
                        wr.Write(idstr);
                        wr.Write(outData[counter].Substring(semicol));
                        wr.Write(';');
                        wr.Write(actTerritory.SVMResults[counter].Building);
                        wr.Write(';');
                        wr.Write(actTerritory.SVMResults[counter].Vegetation);
                        wr.Write(';');
                        wr.Write(actTerritory.SVMResults[counter].Road);
                        wr.Write(Environment.NewLine);

                        counter++;
                        if (counter == 100)
                        {
                            done = true;
                            start = false;
                        }
                    }
                }

                if (!done)
                {
                    for (int i = 0; i < outData.Length; i++)
                    {
                        int semicol = outData[i].IndexOf(';');
                        wr.Write(idstr);
                        wr.Write(outData[i].Substring(semicol));
                        wr.Write(';');
                        wr.Write(actTerritory.SVMResults[i].Building);
                        wr.Write(';');
                        wr.Write(actTerritory.SVMResults[i].Vegetation);
                        wr.Write(';');
                        wr.Write(actTerritory.SVMResults[i].Road);
                        wr.Write(Environment.NewLine);
                    }
                }
            }
        }
    }
}
