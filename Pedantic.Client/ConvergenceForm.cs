using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScottPlot;

namespace Pedantic.Client
{
    public partial class ConvergenceForm : Form
    {
        public ConvergenceForm()
        {
            InitializeComponent();
        }

        private void ConvergenceForm_Load(object sender, EventArgs e)
        {
            plotConverge.Plot.Legend(true, Alignment.LowerCenter);
            plotConverge.Plot.XAxis.Ticks(true, false, true);
        }
    }
}
