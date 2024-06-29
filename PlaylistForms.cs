using System;
using System.Diagnostics;
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
    public partial class PlaylistForms : Form
    {
        public PlaylistForms()
        {
            InitializeComponent();
            PopulateComboBoxes();
        }

        private void PopulateComboBoxes()
        {
            cbQualidadePlaylist.Items.AddRange(new string[] { "144p", "360p", "480p", "720p", "1080p" });
            cbFormatoPlaylist.Items.AddRange(new string[] { "mp3", "mp4" });

            cbQualidadePlaylist.SelectedIndex = 3; // 720p
            cbFormatoPlaylist.SelectedIndex = 1; // mp4
        }

        private string _selectedFolderPath;

        private void downloadDeVideosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            downloadDePlaylistsToolStripMenuItem.Visible = false;
            Form1 form1 = new Form1();
            form1.ShowDialog();
            this.SuspendLayout();
        }

        void Clear()
        {
            txtUrlPlaylist.Text = string.Empty;
            lblSalvarPlaylist.Text = string.Empty;
            BarraDeProgressoPlaylist.Value = 0;
            lblStatusPlaylist.Items.Clear();
        }

        private void voltarAoInícioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            downloadDePlaylistsToolStripMenuItem.Visible = false;
            Form1 form1 = new Form1();
            form1.ShowDialog();
            this.SuspendLayout();
        }

        private async void btnDownloadPlaylist_Click(object sender, EventArgs e)
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

            string playlistUrl = txtUrlPlaylist.Text;

            if (string.IsNullOrEmpty(playlistUrl))
            {
                MessageBox.Show("Por favor, insira a URL da playlist.");
                return;
            }

            try
            {
                var youtube = new YoutubeClient();
                var playlist = await youtube.Playlists.GetAsync(playlistUrl);
                var videos = await youtube.Playlists.GetVideosAsync(playlist.Id);

                BarraDeProgressoPlaylist.Maximum = 100;
                BarraDeProgressoPlaylist.Value = 0;

                foreach (var video in videos)
                {
                    lblStatusPlaylist.Items.Add($"Baixando... {video.Title}");
                    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

                    var qualidade = cbQualidadePlaylist.SelectedItem.ToString();
                    var formato = cbFormatoPlaylist.SelectedItem.ToString();

                    IStreamInfo streamInfo = null;

                    if (formato == "mp4")
                    {
                        streamInfo = streamManifest.GetMuxedStreams().FirstOrDefault(s => s.VideoQuality.Label == qualidade);
                    } else if (formato == "mp3")
                    {
                        streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                    }

                    if (streamInfo == null)
                    {
                        lblStatusPlaylist.Items.Add($"Qualidade ou formato não disponível para {video.Title}, pulando...");
                        continue;
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
                            BarraDeProgressoPlaylist.Value = Math.Min((int)progress, 100); // Atualizar em termos percentuais

                        }
                    }

                    if (formato == "mp3")
                    {
                        ConvertToMp3(tempFilePath, finalFilePath);
                        File.Delete(tempFilePath);
                    } else
                    {
                        File.Move(tempFilePath, finalFilePath);
                    }

                    lblStatusPlaylist.Items.Add($"Concluído: {video.Title}");
                }

                MessageBox.Show("Download da playlist concluído!");
                Clear();
            } catch (Exception ex)
            {
                MessageBox.Show($"Ocorreu um erro: {ex.Message}");
            }
        }

        private void btnSalvarPlaylist_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedFolderPath = dialog.SelectedPath;
                    lblSalvarPlaylist.Text = _selectedFolderPath;
                }
            }
        }

        private void btnLimparPlaylist_Click(object sender, EventArgs e)
        {
            Clear();
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
    }
}
