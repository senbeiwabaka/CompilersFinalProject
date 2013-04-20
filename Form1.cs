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

namespace CompilersFinalProject
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var open = new OpenFileDialog();
            open.InitialDirectory = Environment.CurrentDirectory + "\\Cases\\";
            open.Multiselect = false;
            open.Filter = "Text Files (.txt)|*.txt|All Files (*.*)|*.*";
            open.FilterIndex = 1;

            var result = open.ShowDialog();

            if (result == DialogResult.OK)
            {
                var resultsReader = new StreamReader(open.FileName);

                txtCode.Clear();
                txtNormalization.Clear();

                txtCode.Text = resultsReader.ReadToEnd();

                Text = "Output File : " + open.SafeFileName;
            }
        }

        private void loopNormalizationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtNormalization.Lines = LoopNormalization.Normalize(txtCode.Lines);
        }

        private void outputGenerationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtOutput.Lines = OutputGenerator.Generate(txtNormalization.Lines.ToList());
        }

        private void dependencyToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
