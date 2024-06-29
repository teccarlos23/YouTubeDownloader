using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using MediaToolkit;
using MediaToolkit.Model;

namespace YouTubeDownloader
{
    public partial class Form1 : Form
    {
        private string _selectedFolderPath = string.Empty;

        public Form1()
        {
            InitializeComponent();
            PopulateComboBoxes();
            cbFormato.SelectedIndexChanged += CbFormato_SelectedIndexChanged;
        }

        private void PopulateComboBoxes()
        {
            cbQualidade.Items.AddRange(new string[] { "144p", "360p", "480p", "720p", "720p60", "1080p", "1080p60", "128kbps" });
            cbFormato.Items.AddRange(new string[] { "mp3", "mp4" });

            cbQualidade.SelectedIndex = 5; // 1080p
            cbFormato.SelectedIndex = 1; // mp4

            if (cbFormato.SelectedItem.ToString() == "mp3")
            {
                cbQualidade.SelectedIndex = 7; // 128kbps
            }
        }

        private void CbFormato_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbFormato.SelectedItem.ToString() == "mp3")
            {
                cbQualidade.SelectedIndex = 7; // 128kbps
            } else
            {
                cbQualidade.SelectedIndex = 5; // 1080p
            }
        }

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolderPath))
            {
                MessageBox.Show("Por favor, selecione uma pasta de destino.");
                return;
            }

            if (!Directory.Exists(_selectedFolderPath))
            {
                MessageBox.Show("O caminho especificado não existe. Por favor, selecione um caminho válido.");
                return;
            }

            string videoUrl = txtUrlYouTube.Text;

            if (string.IsNullOrEmpty(videoUrl))
            {
                MessageBox.Show("Por favor, insira a URL do vídeo.");
                return;
            }

            try
            {
                var youtube = new YoutubeClient();
                var video = await youtube.Videos.GetAsync(videoUrl);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

                var qualidade = cbQualidade.SelectedItem.ToString();
                var formato = cbFormato.SelectedItem.ToString();

                IStreamInfo streamInfo = null;

                if (formato == "mp4")
                {
                    // Primeira tentativa: encontrar qualidade exata
                    streamInfo = streamManifest.GetMuxedStreams().FirstOrDefault(s => s.VideoQuality.Label == qualidade);

                    // Segunda tentativa: encontrar a maior qualidade disponível
                    if (streamInfo == null)
                    {

                        streamInfo = streamManifest.GetMuxedStreams().OrderByDescending(s => s.VideoQuality.IsHighDefinition).FirstOrDefault();
                    }
                } else if (formato == "mp3")
                {
                    streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                }

                if (streamInfo == null)
                {
                    MessageBox.Show($"Qualidade ou formato não disponível para {video.Title}");
                    lblStatusVideo.Text = $"Qualidade ou formato não disponível para {video.Title}";
                    return;
                }

                var safeFileName = GetSafeFileName(video.Title);
                var tempFilePath = Path.Combine(_selectedFolderPath, $"{safeFileName}.tmp");
                var finalFilePath = Path.Combine(_selectedFolderPath, $"{safeFileName}.{formato}");

                using (var stream = await youtube.Videos.Streams.GetAsync(streamInfo))
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    var totalBytes = streamInfo.Size.MegaBytes;
                    var buffer = new byte[81920];
                    int bytesRead;
                    double totalRead = 0;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        var progress = totalRead / totalBytes * 100;
                        BarraDeProgresso.Value = Math.Min((int)progress, 100); // Atualizar em termos percentuais
                        lblStatusVideo.Text = $"Baixando... {video.Title} ({progress:F2}%)";
                        BarraDeProgresso.Update();
                    }
                }

                if (formato == "mp3")
                {
                    lblStatusVideo.Text = $"Convertendo para MP3... {video.Title}";
                    ConvertToMp3(tempFilePath, finalFilePath);
                    File.Delete(tempFilePath);
                } else
                {
                    File.Move(tempFilePath, finalFilePath);
                }

                lblStatusVideo.Text = $"Concluído: {video.Title}";
                MessageBox.Show($"Download concluído: {video.Title}");

                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{finalFilePath}\"");


                Clear();
            } catch (Exception ex)
            {
                MessageBox.Show($"Ocorreu um erro: {ex.Message}");
                lblStatusVideo.Text = $"Erro: {ex.Message}";
            }
        }

        private void btnSalvar_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedFolderPath = dialog.SelectedPath;
                    lblSalvar.Text = _selectedFolderPath;
                }
            }
        }

        private void btnLimpar_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void Clear()
        {
            txtUrlYouTube.Text = string.Empty;
            BarraDeProgresso.Value = 0;
            lblSalvar.Text = string.Empty;
            lblStatusVideo.Text = "YOUTUBE";
        }

        private string GetSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            name = name.Length > 150 ? name.Substring(0, 150) : name;

            return name;
        }

        private void ConvertToMp3(string inputPath, string outputPath)
        {
            var inputFile = new MediaFile { Filename = inputPath };
            var outputFile = new MediaFile { Filename = outputPath };

            using (var engine = new Engine())
            {
                engine.Convert(inputFile, outputFile);
            }
        }

        private void voltarAoInícioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrimeiroForm primeiroForm = new PrimeiroForm();
            primeiroForm.Show();
            this.Close();
            
        }

        private void downloadDePlaylistsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            downloadDeVideosToolStripMenuItem.Visible = false;
            PlaylistForms playlistForms = new PlaylistForms();
            playlistForms.ShowDialog();
            this.Hide();
        }
    }
}



