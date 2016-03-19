using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Diagnostics;
using Windows.ApplicationModel.Core;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.Activation;
using Microsoft.Phone.Scheduler;
using System.Windows.Media;
using Microsoft.Phone.Reactive;

namespace PinTimer
{
	partial class TimerSettingsSetup : UserControl
	{
		CoreApplicationView view = CoreApplication.GetCurrentView();
		public PinTimer Timer { get; set; }
		public TimerSettingsSetup()
		{
			InitializeComponent();
			soundPicker.ItemsSource = AudioItemData.ListOfPossible;
			this.Loaded += TimerSettingsSetup_Loaded;
		}

		void TimerSettingsSetup_Loaded(object sender, RoutedEventArgs e)
		{
			if (beginDatePicker.Value == TimeSpan.Zero)
				//Scheduler.Dispatcher.Schedule(() => { beginDatePicker.OpenPicker(); }, TimeSpan.FromMilliseconds(200));
				beginDatePicker.OpenPicker();
		}

		public TimerSettingsSetup(PinTimer timer) : this()
		{
			Timer = timer ?? new PinTimer(TimeSpan.Zero) { AudioSource = AudioItemData.ListOfPossible[0]};
			DataContext = Timer;
		}

		private void OnAddSoundTap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			e.Handled = true;		

			var ImagePath = string.Empty;
			FileOpenPicker filePicker = new FileOpenPicker();
			filePicker.ViewMode = PickerViewMode.List;

			// Filter to include a sample subset of file types
			filePicker.FileTypeFilter.Clear();
			filePicker.FileTypeFilter.Add(".wav");
			filePicker.FileTypeFilter.Add(".mp3");
			filePicker.FileTypeFilter.Add(".wma");

			filePicker.PickSingleFileAndContinue();
			view.Activated += viewActivated;
		}

		private void viewActivated(CoreApplicationView sender, IActivatedEventArgs args1)
		{
			FileOpenPickerContinuationEventArgs args = args1 as FileOpenPickerContinuationEventArgs;

			if (args != null)
			{
				if (args.Files.Count == 0) return;

				view.Activated -= viewActivated;
				var file = args.Files[0];
				var audioUri = new Uri(file.Path, UriKind.Relative);
				try
				{
					Alarm al = new Alarm(new Guid().ToString());
					al.Sound = audioUri;

					AudioItemData audioData = new AudioItemData();
					audioData.Path = audioUri;
					audioData.Title = file.DisplayName;
					AudioItemData.AddNewAudio(audioData);
					soundPicker.SelectedIndex = soundPicker.Items.Count - 1;
				}
				catch (System.Exception ex)
				{
					MessageBox.Show("Files name contain unsupported characters. Please select another file.");
				}				
			}
		}

		private MediaElement media;
		private void RoundButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			e.Handled = true;
			var data = (sender as Button).DataContext as AudioItemData;
			if (media == null)
			{
				//media = (App.RootFrame.Content as ListPickerPage).GetChildOfType<MediaElement>();				
				media = new MediaElement();
				media.Unloaded += (s, p) => { media = null; };
				media.Volume = 1.0;
				((App.RootFrame.Content as ListPickerPage).Content as Panel).Children.Add(media);				
			}

			if (media.Source != data.Path)
			{
				media.Source = data.Path;
				return;
			}

			if (media.CurrentState != MediaElementState.Playing)
				media.Play();
			else
				media.Stop();
		}		
	}
}
