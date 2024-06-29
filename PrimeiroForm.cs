using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YouTubeDownloader
{
    public partial class PrimeiroForm : Form
    {
        public PrimeiroForm()
        {
            InitializeComponent();
        }


        
        private void btnbaixarVideos_Click(object sender, EventArgs e)
        {
            Form1 form1 = new Form1();
            form1.ShowDialog();
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PlaylistForms playlistForms = new PlaylistForms();
            playlistForms.ShowDialog();
          
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
