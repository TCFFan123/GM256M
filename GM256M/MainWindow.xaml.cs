using Microsoft.Win32;
using MIDIModificationFramework;
using MIDIModificationFramework.MIDIEvents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GM256M
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool isNull = true;
        bool isNull2 = true;
        bool mirror = false;
        string in_path = "nothing";
        string out_path = "nothing";
        bool stop = false;

        public MainWindow()
        {
            InitializeComponent();
        }
        void resetState()
        {
            in_path = "";
            out_path = "";
            progressBar.Maximum = 0;
            progressBar.Value = 0;
            isNull = true;
            isNull2 = true;

        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = "Load MIDI File";
            theDialog.Filter = "MIDI Files|*.mid";
            theDialog.InitialDirectory = @"C:\";
            theDialog.ShowDialog();
            in_path = theDialog.FileName;
            if (in_path == "")
            {
                isNull = true;
                return;
            }
            else
                isNull = false;
            if (isNull == false)
            {
                start.IsEnabled = true;
                midiName1.Content = in_path;
            }
            else
            {
                midiName1.Content = "{Select MIDI}";
                start.IsEnabled = false;
            }



        }

        private void MirrorModeCB_Checked(object sender, RoutedEventArgs e)
        {
            if (mirrorModeCB.IsChecked == true)
                mirror = true;
            else
                mirror = false;

        }

        private void SaveLoc_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog theDialog = new SaveFileDialog();
            theDialog.Title = "Save File Location And Name";
            theDialog.Filter = "MIDI Files|*.mid";
            theDialog.InitialDirectory = @"C:\";
            theDialog.ShowDialog();
            out_path = theDialog.FileName;
            if (out_path == "")
            {
                isNull2 = true;
                return;
            }
            else
                isNull2 = false;
            if (isNull2 == false)
                saveName.Content = out_path;
            else
                saveName.Content = "{Select Save Location}";

        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (isNull2 == true)
            {
                MessageBox.Show("Please select a save location first!", "Hey!");
            }
            else
            {
                long totalNotes = 0;
                MidiFile file = new MidiFile(in_path);
                BufferedStream outmidi = new BufferedStream(new StreamWriter(out_path).BaseStream);
                MidiWriter writer = new MidiWriter(outmidi);

                writer.Init();

                writer.WriteFormat(file.Format);
                writer.WritePPQ(file.PPQ);
                writer.WriteNtrks((ushort)file.TrackCount);
                Random rnd = new Random();
                progressBar.Maximum = file.TrackCount;

                new Thread(() =>
                {
                    Stopwatch s = new Stopwatch();
                    s.Start();
                    for (int i = 0; i < file.TrackCount; i++)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (s.ElapsedMilliseconds > 500)
                            {
                                s.Reset();
                                s.Start();
                                progressBar.Value = i;
                                currentTrack.Content = "Working On Track" + i + "/" + file.TrackCount;
                                totalNotesL.Content = "Total Notes: " + totalNotes;
                            }
                        });

                        writer.InitTrack();
                        var reader = file.GetTrack(i);
                        foreach (MIDIEvent a in reader)
                        {
                            if (a is NoteOnEvent)
                            {
                                var ev = (NoteOnEvent)a;
                                if (mirror == true)
                                    writer.Write(new NoteOnEvent(0, ev.Channel, (byte)(127 - ev.Key + 128), ev.Velocity));
                                else
                                    writer.Write(new NoteOnEvent(0, ev.Channel, (byte)(ev.Key + 128), ev.Velocity));
                                totalNotes += 2;
                                
                            }
                            else if (a is NoteOffEvent)
                            {
                                var ev = (NoteOffEvent)a;
                                if (mirror == true)
                                    writer.Write(new NoteOffEvent(0, ev.Channel, (byte)(127 - ev.Key + 128)));
                                else
                                    writer.Write(new NoteOffEvent(0, ev.Channel, (byte)(ev.Key + 128)));
                            }
                            
                            writer.Write(a);
                            if(stop == true)
                            {
                                return;
                            }

                        }
                        if (stop == true)
                            return;
                        writer.EndTrack();
                    }
                    writer.Close();
                    MessageBox.Show("Complete!", "Merge Complete.");
                    resetState();
                }).Start();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            stop = true;
        }
    }
}
