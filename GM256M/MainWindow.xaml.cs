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
        long polyphony = 0;
        long tempPoly = 0;

        public MainWindow()
        {
            InitializeComponent();
        }
        void resetState()
        {
            //Reset everything to their default values
            in_path = "";
            out_path = "";
            progressBar.Maximum = 1;
            progressBar.Value = 0;
            Status.Content = "Inactive. Select a MIDI and a save location to merge at.";
            isNull = true;
            isNull2 = true;
            checkPoly.IsEnabled = false;

        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = "Load MIDI file";
            theDialog.Filter = "MIDI files|*.mid";
            theDialog.RestoreDirectory = true;
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
                checkPoly.IsEnabled = true;
                midiName1.Content = in_path;
            }
            else
            {
                midiName1.Content = "Select MIDI";
                checkPoly.IsEnabled = false;
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
            theDialog.Title = "Save file location and name";
            theDialog.Filter = "MIDI files|*.mid";
            theDialog.RestoreDirectory = true;
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
                saveName.Content = "Select save location";

        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (isNull2 == true)
            {
                MessageBox.Show("Please select a save location and filename first!", "Error");
            }
            else
            {
                Status.Content = "Initializing";
                long totalNotes = 0;
                MidiFile file = new MidiFile(in_path);
                BufferedStream outmidi = new BufferedStream(new StreamWriter(out_path).BaseStream);
                MidiWriter writer = new MidiWriter(outmidi);

                writer.Init(file.PPQ);
                progressBar.Maximum = file.TrackCount;
                Status.Content = "Merging";
                new Thread(() =>
                {
                    Stopwatch s = new Stopwatch();
                    s.Start();
                    for (int i = 0; i < file.TrackCount; i++)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (s.ElapsedMilliseconds > 35)
                            {
                                s.Reset();
                                s.Start();
                                progressBar.Value = i;
                                currentTrack.Content = "Processing track " + i + "/" + file.TrackCount;
                                totalNotesL.Content = "Total notes: " + totalNotes;
                            }
                        });
                        Dispatcher.Invoke(() =>
                        {
                            Status.Content = "Starting track " + i;
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
                            if (stop == true)
                            {
                                return;
                            }

                        }
                        if (stop == true)
                            return;
                        Dispatcher.Invoke(() =>
                        {
                            Status.Content = "Ending track " + i;
                        });
                        writer.EndTrack();
                    }
                    writer.Close();
                    Thread.Sleep(100);
                    Dispatcher.Invoke(() =>
                    {
                        resetState();
                        MessageBox.Show("Complete!", "Merge complete.");
                    });
                }).Start();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            stop = true;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            long totalNotes = 0;
            MidiFile file = new MidiFile(in_path);
            Random rnd = new Random();
            checkPoly.IsEnabled = false;

            new Thread(() =>
            {
                Stopwatch s = new Stopwatch();
                s.Start();
                Dispatcher.Invoke(() =>
                {
                    //progressBar.Value = i; //TODO
                    Status.Content = "Merging tracks for checking";
                    currentTrack.Content = "Checking Track: N/A";
                    totalNotesL.Content = "Checked Notes: N/A";
                });
                var merge = Mergers.MergeSequences(file.IterateTracks()).ChangePPQ(file.PPQ, 1).CancelTempoEvents(250000);
                Dispatcher.Invoke(() =>
                {
                    Status.Content = "Checking MIDI";
                });
                foreach (MIDIEvent a in merge)
                {
                    if (s.ElapsedMilliseconds > 200)
                    {
                        s.Reset();
                        s.Start();
                        Dispatcher.Invoke(() =>
                        {
                            totalNotesL.Content = "Checked notes: " + totalNotes;
                        });
                    }
                    if (a is NoteOnEvent)
                    {
                        var ev = (NoteOnEvent)a;
                        tempPoly++;
                        if (tempPoly > polyphony) polyphony = tempPoly;
                        totalNotes += 1;

                    }
                    else if (a is NoteOffEvent)
                    {
                        var ev = (NoteOffEvent)a;
                        tempPoly--;
                    }
                }
                Thread.Sleep(100);
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"-------{System.IO.Path.GetFileName(in_path)} results-------\nMax polyphony: {polyphony}\nNote count: {totalNotes}\nPPQ: {file.PPQ}\nTracks: {file.TrackCount}\nFormat: {file.Format}", "Check complete!");
                    resetState();
                });
            }).Start();
        }
    }
}
