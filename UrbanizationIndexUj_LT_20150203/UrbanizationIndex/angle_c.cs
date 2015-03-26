using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace angle_converter
{
    public partial class angle_converter : Form
    {
        private double lat_result, long_result;
        public double getLatResult() { return lat_result; }
        public double getLongResult() { return long_result; }
        

        public angle_converter ( )
        {
            InitializeComponent();
            lat_result = long_result = 0;
            
        }

        public string ConvertDecimalPoint(string strnum)
        {
            int res = strnum.IndexOf('.');
            if (res == -1)
            {
                return strnum;
            }
            else
            {   
                return strnum.Replace('.', ',');
                
            }

        }

        private void conv_btn_Click(object sender, EventArgs e)
        {
            

            double lat_ang, lat_min, lat_sec, long_ang, long_min, long_sec;

            bool lat_a = Double.TryParse(lat_angle_text.Text, out lat_ang);
            bool lat_m = Double.TryParse(lat_min_text.Text, out lat_min);
            bool lat_s = Double.TryParse(lat_sec_text.Text, out lat_sec);

            bool long_a = Double.TryParse(long_angle_text.Text, out long_ang);
            bool long_m = Double.TryParse(long_min_text.Text, out long_min);
            bool long_s = Double.TryParse(long_sec_text.Text, out long_sec);
            

            if (lat_a && lat_m && lat_s && long_a && long_m && long_s)
            {
                lat_result = Math.Round( lat_ang + (lat_min / 60) + (lat_sec / 3600), 5);
                long_result = Math.Round( long_ang + (long_min / 60) + (long_sec / 3600), 5);
                //MessageBox.Show(lat_result.ToString()+long_result.ToString());
                this.Close();

            }
            else {
                lat_a = Double.TryParse(ConvertDecimalPoint(lat_angle_text.Text), out lat_ang);
                lat_m = Double.TryParse(ConvertDecimalPoint(lat_min_text.Text), out lat_min);
                lat_s = Double.TryParse(ConvertDecimalPoint(lat_sec_text.Text), out lat_sec);
                long_a = Double.TryParse(ConvertDecimalPoint(long_angle_text.Text), out long_ang);
                long_m = Double.TryParse(ConvertDecimalPoint(long_min_text.Text), out long_min);
                long_s = Double.TryParse(ConvertDecimalPoint(long_sec_text.Text), out long_sec);
                if (lat_a && lat_m && lat_s && long_a && long_m && long_s) {
                    lat_result = Math.Round(lat_ang + (lat_min / 60) + (lat_sec / 3600), 5);
                    long_result = Math.Round(long_ang + (long_min / 60) + (long_sec / 3600), 5);
                    //MessageBox.Show(lat_result.ToString()+long_result.ToString());
                    this.Close();
                }
                else MessageBox.Show("Incorrect data!");
            }



        }
    }
}
