﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SoundBoard.Model;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;

namespace SoundBoard
{
    public sealed partial class MainPage : Page
    {
        private Guid _currentlyPlaying;

        public ObservableCollection<SampleGroup> Samples { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadData();
        }

        private async Task LoadData()
        {
            this.progressRing.Visibility = Visibility.Visible;
            this.progressRing.IsActive = true;
            this.itemGridView.Visibility = Visibility.Collapsed;

            this.Samples = await DataSource.GetSamplesGrouped();
            SamplesCVS.Source = this.Samples;
            
            this.progressRing.Visibility = Visibility.Collapsed;
            this.progressRing.IsActive = false;
            this.itemGridView.Visibility = Visibility.Visible;
        }


        /// <summary>
        /// Invoked when an item within a group is clicked.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is snapped)
        /// displaying the item clicked.</param>
        /// <param name="e">Event data that describes the item clicked.</param>
        private void itemGridView_ItemClick(object sender, ItemClickEventArgs e)
        {

            try
            {
                var sampleItem = ((Sample)e.ClickedItem);

                //Resume/pause existing sample
                if (_currentlyPlaying == sampleItem.UniqueID)
                {
                    if (this.AudioPlayer.MediaPlayer?.PlaybackSession?.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
                    {
                        this.AudioPlayer.MediaPlayer?.Pause();
                    }
                    else
                    {
                        this.AudioPlayer.MediaPlayer?.Play();
                    }
                    return;
                }

                this.AudioPlayer.Source = sampleItem.MediaSource;
                _currentlyPlaying = sampleItem.UniqueID;
                this.AudioPlayer.MediaPlayer?.Play();
                this.AudioPlayer.Visibility = Visibility.Visible;
                this.playBackErrorMessage.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                this.AudioPlayer.Visibility = Visibility.Collapsed;

                this.playBackErrorMessage.Text = ex.Message;
                this.playBackErrorMessage.Visibility = Visibility.Visible;
            }

        }

        private async void RemoveSampleButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedSample = (Sample)this.itemGridView.SelectedItem;

            // Create the message dialog and set its content
            var messageDialog = new MessageDialog($"Are you sure you want to delete {selectedSample.Title}?");

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            messageDialog.Commands.Add(new UICommand(
                "Yes",
                new UICommandInvokedHandler(this.CommandInvokeDelete)));
            messageDialog.Commands.Add(new UICommand("Cancel"));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 1;

            // Show the message dialog
            await messageDialog.ShowAsync();
        }

        private async void CommandInvokeDelete(IUICommand command)
        {
            this.progressRing.Visibility = Visibility.Visible;
            this.itemGridView.Visibility = Visibility.Collapsed;
            var selectedSample = (Sample)this.itemGridView.SelectedItem;

            StopPlaybackAndClear();

            await DataSource.RemoveSample(selectedSample.UniqueID);
            await this.LoadData();
        }

        private void AddSampleButton_Click(object sender, RoutedEventArgs e)
        {
            StopPlaybackAndClear();

            //Navigate to next screen
            ((Frame)Window.Current.Content).Navigate(typeof(AddSample));
        }

        private void itemGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.RemoveSampleButton.Visibility = e.AddedItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void StopPlaybackAndClear()
        {
            // Stop any play back
            this.AudioPlayer.MediaPlayer?.Pause();
            this.AudioPlayer.Source = null;
            _currentlyPlaying = Guid.Empty;
        }


    }
}
