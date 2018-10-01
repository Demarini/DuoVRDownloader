using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownloadDuoVRSongs
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public void DownloadSongs(List<DuoVRSong> downloadLinks)
        {
            int i2 = 1;
            for(int i = downloadLinks.Count() - 1;i >= 0; i--)
            {
                if(textBox3.Text != "")
                {
                    if (i2 > int.Parse(textBox3.Text))
                    {
                        break;
                    }
                }

                if (textBox4.Text != "")
                {
                    if (downloadLinks[i].Difficulty < double.Parse(textBox4.Text))
                    {
                        continue;
                    }
                }

                if (textBox5.Text != "")
                {
                    if (downloadLinks[i].PP < double.Parse(textBox5.Text))
                    {
                        continue;
                    }
                }
                  
                using (WebClient wc = new WebClient())
                {
                    label9.Text = "Downloading " + downloadLinks[i].Name;
                    wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                    wc.DownloadFile(
                        // Param1 = Link of file
                        new Uri(downloadLinks[i].URL),
                        // Param2 = Path to save
                        textBox1.Text + downloadLinks[i].ID + ".zip"
                    );
                    try
                    {
                        System.IO.Compression.ZipFile.ExtractToDirectory(textBox1.Text + downloadLinks[i].ID + ".zip", textBox2.Text);
                    }
                    catch
                    {
                        continue;
                    }
                    
                    DateTime lastHigh = new DateTime(1900, 1, 1);
                    string highDir = "";
                    foreach (string subdir in Directory.GetDirectories(textBox2.Text))
                    {
                        DirectoryInfo fi1 = new DirectoryInfo(subdir);
                        DateTime created = fi1.LastWriteTime;

                        if (created > lastHigh)
                        {
                            highDir = subdir;
                            lastHigh = created;
                        }
                    }
                    string[] files = Directory.GetFiles(highDir);
                    string infos = GetInfo(files);
                    InfoEntity info = ReturnInfos(infos);
                    info = ModifyInfoNum(i2, downloadLinks[i].PP, info);
                    WriteInfos(info, highDir);
                    i2++;
                }
            }
            MessageBox.Show("Download Complete!");
            
        }
        private void WriteInfos(InfoEntity info, string path)
        {
            var newJson = JsonConvert.SerializeObject(info,
                                Newtonsoft.Json.Formatting.None,
                                new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Ignore
                                });
            System.IO.File.WriteAllText(path + "\\info.json", newJson);
        }
        private string GetInfo(string[] files)
        {
            foreach (string s in files)
            {
                string s2 = System.IO.Path.GetExtension(s).ToUpper();
                if (System.IO.Path.GetFileName(s).ToUpper() == "INFO.JSON")
                {
                    return s;
                }
            }
            return null;
        }
        public InfoEntity ReturnInfos(string json)
        {
            var json2 = System.IO.File.ReadAllText(json);
            InfoEntity info = JsonConvert.DeserializeObject<InfoEntity>(json2);
            return info;
        }
        public InfoEntity ModifyInfoNum(int counter, double pp, InfoEntity info)
        {
            string counterString = "";
            if (counter < 10)
            {
                counterString = "0000" + counter.ToString();
            }
            else if (counter > 9 && counter < 100)
            {
                counterString = "000" + counter.ToString();
            }
            else if(counter > 99 && counter < 1000)
            {
                counterString = "00" + counter.ToString();
            }
            else if(counter > 999 && counter < 10000)
            {
                counterString = "0" + counter.ToString();
            }
            else
            {
                counterString = counter.ToString();
            }
            info.songName = counterString + ". " + " - " + pp.ToString() + " - " + info.songName;
            return info;
        }
        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }
        public List<DuoVRSong> GetSongDownloads()
        {
            try
            { 
            List<DuoVRSong> songs1 = new List<DuoVRSong>();
            string songs = ReturnHTML("https://raw.githubusercontent.com/DuoVR/PPFarming/master/js/songlist.tsv");
            List<string> lines = songs.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
                ).ToList();
            for (int i = lines.Count() - 1; i >= 0; i--)
            {
                DuoVRSong song = new DuoVRSong();
                if (lines[i].Contains("\t"))
                {
                    song.Name = lines[i].Split(new string[] { "\t" }, StringSplitOptions.None)[0];
                    song.PP = double.Parse(lines[i].Split(new string[] { "\t" }, StringSplitOptions.None)[1]);
                        string temp = lines[i].Split(new string[] { "\t" }, StringSplitOptions.None)[3].Split('★')[0];
                        if(temp == "")
                        {
                            song.Difficulty = 0;
                        }
                        else
                        {
                            song.Difficulty = double.Parse(temp);
                        }
                    
                    string s = lines[i];
                    lines[i] = lines[i].Split(new string[] { "\t" }, StringSplitOptions.None)[4];
                    s = lines[i];
                    if (lines[i] != "EMPTY" && s != "")
                    {
                        s = lines[i];
                        lines[i] = lines[i].Split(new string[] { "'" }, StringSplitOptions.None)[1];
                        s = lines[i];
                        if (lines[i].Contains('/'))
                        {
                            s = lines[i];
                            lines[i] = lines[i].Split('/')[6];
                            s = lines[i];
                            lines[i] = lines[i].Split('.')[0];
                            song.ID = lines[i];
                            lines[i] = "https://beatsaver.com/download/" + lines[i];
                            song.URL = lines[i];
                            songs1.Add(song);
                        }
                        else
                        {
                            lines.Remove(lines[i]);
                        }
                    }
                    else
                    {
                        lines.Remove(lines[i]);
                    }

                }
            }

            return songs1;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public string ReturnHTML(string urlAddress)
        {
            //string urlAddress = "https://duovr.github.io/PPFarming/";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }

                string data = readStream.ReadToEnd();

                response.Close();
                readStream.Close();
                return data;
            }
            else
            {
                return null;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DownloadSongs(GetSongDownloads());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.ShowDialog();
            textBox1.Text = dlg.SelectedPath;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.ShowDialog();
            textBox2.Text = dlg.SelectedPath;
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
