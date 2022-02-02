using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Speech.Synthesis;
using System.IO;
using GemBox.Pdf;
using Microsoft.Office.Interop.Word;
using System.Runtime.InteropServices;

namespace SyntezaMowy
{
    public partial class Form1 : Form
    {
        SpeechSynthesizer voice;
        bool ssml = false;
        private readonly List<System.Drawing.Point> theWordsLocations = new List<System.Drawing.Point>();
        private int loc = 0;
        private List<string> theWords = new List<string>();

        private readonly Color HighLightBackColor = Color.Yellow;
        private Color previousSelectionBackColor;

        private System.Drawing.Point previousSelection;

        private int currentWord;
        private int nWords;

        private readonly char[] splitChar = { ' ', '\n' };


        public Form1()
        {
            InitializeComponent();
            
        }

        
       public void speakCompleted(object sender, SpeakCompletedEventArgs e)
       {
           richTextBox1.SelectAll();
           richTextBox1.SelectionBackColor = previousSelectionBackColor;
       }

       private void voice_SpeakProgress(object sender, SpeakProgressEventArgs e)
       {
           if (currentWord == theWordsLocations.Count)
           {
               richTextBox1.SelectionBackColor = previousSelectionBackColor;
           }
           else
           {
               // get current selection locations
               previousSelection = new System.Drawing.Point(richTextBox1.SelectionStart, richTextBox1.SelectionLength);

               // reset the previous word's state Colors
               if (previousSelection != System.Drawing.Point.Empty)
               {
                   richTextBox1.SelectionBackColor = previousSelectionBackColor;
               }

               // save the current Selection state Colors
               previousSelectionBackColor = richTextBox1.SelectionBackColor;

               // get the location of the next word
               var location = theWordsLocations[currentWord];

               // select it
               richTextBox1.Select(location.X, location.Y);

               // highlight it
               richTextBox1.SelectionBackColor = HighLightBackColor;

               // finished ?
               if (currentWord == nWords)
               {
                   richTextBox1.SelectionBackColor = previousSelectionBackColor;
               }

               // move on
               currentWord++;
           }
       }

        private void btnStart_Click(object sender, EventArgs e)
        {
            /*  foreach (var v in voice.GetInstalledVoices().Select(v => v.VoiceInfo))
              {
                  Console.WriteLine("Name:{0}, Gender:{1}, Age:{2}",
                    v.Description, v.Gender, v.Age);
              }
            */
            if(comboBoxVoice.Text == "Wybierz glos")
            {
                MessageBox.Show("Wybierz glos");
                return;
            }
            nWords = 0;
            theWords.Clear();
            theWordsLocations.Clear();
            loc = 0;

            //Text Selector Starts Here
            theWords = richTextBox1.Text.Split(splitChar, StringSplitOptions.RemoveEmptyEntries).ToList();

            nWords = theWords.Count;

            foreach (var word in theWords)
            {
                theWordsLocations.Add(new System.Drawing.Point(loc, word.Length));
                loc += word.Length + 1;
            }
            currentWord = 0;
            voice.Dispose();
            voice = new SpeechSynthesizer();
            voice.SpeakProgress += new EventHandler<SpeakProgressEventArgs>(voice_SpeakProgress);
            voice.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(speakCompleted);
            voice.SetOutputToDefaultAudioDevice();
            voice.SelectVoice(comboBoxVoice.Text);
            voice.Rate = trackBar1.Value;
            voice.Volume = trackBar2.Value;
            // voice.SpeakAsyncCancelAll();

      //      if (ssml)
        //        voice.SpeakSsmlAsync(richTextBox1.Text);
          //  else
                voice.SpeakAsync(richTextBox1.Text);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            voice = new SpeechSynthesizer();
            foreach (InstalledVoice voiceInstall in voice.GetInstalledVoices())
            {
                VoiceInfo infoVoice = voiceInstall.VoiceInfo;
                comboBoxVoice.Items.Add(infoVoice.Name);
            }
             voice.Dispose();   
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            Stream stream;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.ValidateNames = true;
            dialog.Filter = "Txt Files|*.txt|Word Files|*.docx|PDF Files|*.pdf|SSML Files|*.ssml";
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string ext = Path.GetExtension(dialog.FileName);
                try
                {
                    richTextBox1.Text = "";
                    if (ext == ".txt")
                        richTextBox1.LoadFile(dialog.FileName, RichTextBoxStreamType.PlainText);
            /*        else if (ext == ".ssml")
                    {
                        richTextBox1.LoadFile(dialog.FileName, RichTextBoxStreamType.PlainText);
                        ssml = true;
                    }
            */
                    else if (ext == ".pdf")
                    {
                        loadPDF(dialog.FileName);
                    }
                    else if (ext == ".docx")
                    {
                        loadDOCX(dialog.FileName);
                    }
                }
                catch(Exception ex)
                {
                    
                }
            }
        }

        private void loadDOCX(string fileName)
        {
            Microsoft.Office.Interop.Word.Application word = new Microsoft.Office.Interop.Word.Application();
            Document doc = word.Documents.Open(fileName);
            
            for(int i = 1; i <= doc.Paragraphs.Count; i++)
            {
                richTextBox1.Text += doc.Paragraphs[i].Range.Text.Trim();
            }

            if(doc != null)
            {
                doc.Close();
                Marshal.ReleaseComObject(doc);
            }

            if(word != null)
            {
                word.Quit();
                Marshal.ReleaseComObject(word);
            }
        }

        private void loadPDF(string fileName)
        {
            richTextBox1.Text = "";
            ComponentInfo.SetLicense("FREE-LIMITED-KEY");
            PdfDocument doc = PdfDocument.Load(fileName);

            if(doc.Pages.Count <= 2)
            {
                foreach(var page in doc.Pages)
                {
                    richTextBox1.Text += page.Content.ToString();
                }
            }
            else
            {
                MessageBox.Show("Darmowa wersja do 2 stron");
            }
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            voice.Pause();
        }

        private void btnResume_Click(object sender, EventArgs e)
        {
            voice.Resume();
        }

        private void btnHigh_Click(object sender, EventArgs e)
        {
            if (comboBoxVoice.Text == "Wybierz glos")
            {
                MessageBox.Show("Wybierz glos");
                return;
            }

            SpeechSynthesizer spe = new SpeechSynthesizer();
            string sel = richTextBox1.SelectedText;
            spe.SetOutputToDefaultAudioDevice();
            spe.SelectVoice(comboBoxVoice.Text);
            spe.Speak(sel.Trim());
            spe.Dispose();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "Wav files|*.wav";

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        FileStream fs = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write);
                        voice.SetOutputToWaveStream(fs);
                        voice.Speak(richTextBox1.Text);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
