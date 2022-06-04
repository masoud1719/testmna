using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;

namespace testmna
{
    public partial class Form1 : Form
    {
        private double  u0 = 4e-7;
        private double gravityForce = 9.81;
        private bool isNeuton;
        private double force;
        private double stroke;
        private double voltage;
        private double temperature;
        private double ambientTemperature;
        private double ssf;
        private double heightToDepthRatio;
        private double dutyCycle;
        public Form1()
        {
            InitializeComponent();
            isNeuton = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            chkDutyCycle.SelectedIndex = 0;
            wireGauge.SelectedIndex = 0;
        }

        private IInterpolation getModel (String filePath) {
            var x = new List<double>();
            var y = new List<double>();
            using (var streamReader = new StreamReader(filePath))
            {
                String line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    string[] arr = line.Split(',');
                    x.Add(Double.Parse(arr[0]));
                    y.Add(Double.Parse(arr[1]));
                }
            }
            return Interpolate.CubicSpline(x.AsEnumerable(), y.AsEnumerable());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            getValues();
            // addad shakhes ra hesab mikonim
            double vahidNumber = Math.Sqrt(force) / stroke;
            // Bg az chegali favaran vs sqrt force over tool fasele havayi
            double Bg = getModel(@"Resources\\Bg.txt").Interpolate(vahidNumber);
            // calc r1
            double r1;
            if (isNeuton)
            {
                 r1 = Math.Sqrt((force * u0 ) / (Math.Pow(Bg, 2) * Math.PI)) * 100;
            }
            else
            {
                 r1 = Math.Sqrt((force * u0 * gravityForce) / Math.Pow(Bg, 2)) * 100;
            }
            
            double mmf ;
            if (method1.Checked)
            {
                // 1.2 -> 1.35
                mmf = 16e5 * stroke/100 * Bg * 1.2;
            }
            else if (method2.Checked)
            {
                mmf = (16e5 * stroke/100 * Bg + 8000 * r1/100) ;
            }
            else
            {
                mmf = getMMFFromBHCurve();
            }

            double pho = 2.1e-6;
            double RT1 = 234.5 + temperature;
            double RT2 = 234.5 + (temperature + ambientTemperature);
            double pho2 = pho / (RT1 / RT2);

            // TODO landa bayad az nemoydar khiz va dama 
            // double landa = 0.00121;
            double lambda = getModel(@"Resources\\lambda.txt").Interpolate(temperature);
            // h^2 ( r2 - r1 )
            // validatio ssf 0.5 - 0.7  0.5 -> >100   0.7 -> <100
            double vahidValue = (dutyCycle * pho2 * Math.Pow(mmf, 2)) / (2 * lambda * ssf * temperature);

            // r2 -r1 =rDiff
            double rDiff = Math.Pow(vahidValue / (heightToDepthRatio * heightToDepthRatio), (double)1 / 3);
            double r2 = rDiff + r1;
            // h/ (r2-r1 ) = heightToDepthRatio
            double h = heightToDepthRatio * rDiff;

            // r3 = sqrt (r1^2 +r2 ^2 ) 
            double r3 = Math.Sqrt(Math.Pow(r1, 2) + Math.Pow(r2, 2));

            double t1 = Math.Pow(r1, 2) / (2 * r1);

            double t2 = Math.Pow(r1,2) / (2 * r2);

            double d = Math.Sqrt((4 * pho2 * (r1 + r2) * mmf) / voltage);

            double di = d + 0.02;
            double netHeightOfCoil ;
            if (rdbNetHeightMethod1.Checked)
            {
                netHeightOfCoil = h - 0.28;
            }
            else
            {
                netHeightOfCoil = h - 0.3;
            }

            int doorDarHarLaye = (int) Math.Ceiling(netHeightOfCoil / di);
            // depth of callaf
            double dc = r2 - r1;

            // TODO rename callaf
            double netDepthOfCallaf = dc - 0.2;

            int NumberOfLayer = (int) (netDepthOfCallaf / di);

            double N = doorDarHarLaye * NumberOfLayer;

            double az = (Math.PI / 4) * Math.Pow(d, 2);

            double lmt = Math.PI * (r1 + r2);
            // Resistance
            double R = (pho2 * lmt * N) / az;
            // current
            double I = voltage / R;
            //  talafat
            double P = R * Math.Pow(I, 2);

            double checkedMMF = N * I;

            Console.WriteLine( ((checkedMMF - mmf)/mmf ) *100);

        }

        private double getMMFFromBHCurve()
        {
            return 0;
        }

        private void getValues()
        {
            try
            {
                if (comboBox1.SelectedIndex == 1)
                {
                    isNeuton = false;
                }
                force = Double.Parse(txtForce.Text);
                stroke = Double.Parse(txtStroke.Text);
                voltage = Double.Parse(txtVolyage.Text);
                temperature = Double.Parse(txtTemperature.Text);
                ambientTemperature = Double.Parse(txtAmbientTemp.Text);
                ssf = Double.Parse(txtslotSpaceFactor.Text);
                heightToDepthRatio = Double.Parse(txtHeightToDepthRatio.Text);
                dutyCycle = Double.Parse(chkDutyCycle.Text);
            }
            catch (Exception exception)
            {
                MessageBox.Show("Please Enter a valid data");
            }
        }
    }
}
