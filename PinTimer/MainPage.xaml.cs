﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PinTimer.Resources;
using Microsoft.Phone.Scheduler;
using Coding4Fun.Toolkit.Controls;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PinTimer
{
	public partial class MainPage : PhoneApplicationPage
	{
		PinTimer p;
		TimeSpanPicker _durationPicker;
		bool _shouldPlayAlarmSound = true;
		// Constructor
		public MainPage()
		{
			InitializeComponent();
			
			_durationPicker = new TimeSpanPicker();
			_durationPicker.Visibility = Visibility.Collapsed;
			_durationPicker.DialogTitle = "Select countdown time";
			_durationPicker.ValueChanged += tp_ValueChanged;
			LayoutRoot.Children.Add(_durationPicker);
			if (IsLightTheme())
				LayoutRoot.Background = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120));

			LoadTimers();

			PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
			PhoneApplicationService.Current.Deactivated += Current_Deactivated;
			PhoneApplicationService.Current.Closing += Current_Closing;
			PhoneApplicationService.Current.Activated += Current_Activated;
			
			//txtInput is a TextBox defined in XAML.
			/*var n = ScheduledActionService.GetActions<ScheduledNotification>();
			if (n.Count<ScheduledNotification>() > 0)
				EmptyTextBlock.Visibility = Visibility.Collapsed;
			else
				EmptyTextBlock.Visibility = Visibility.Visible;*/
			//NotificationListBox.ItemsSource = n;
			// Sample code to localize the ApplicationBar
			//BuildLocalizedApplicationBar();
		}

		private void TimersListBox_Loaded(object sender, RoutedEventArgs e)
		{
			TimersListBox.Loaded -= TimersListBox_Loaded;
			ShiftTimeForActiveTimers();
		}

		void Current_Activated(object sender, ActivatedEventArgs e)
		{
			ShiftTimeForActiveTimers();
		}

		void Current_Closing(object sender, ClosingEventArgs e)
		{
			SaveLastActiveTime();
			SaveTimers();
		}

		void Current_Deactivated(object sender, DeactivatedEventArgs e)
		{
			SaveLastActiveTime();
			SaveTimers();
		}

		private void ShiftTimeForActiveTimers()
		{
			if (TimersListBox.Items.Count == 0) return;
			_shouldPlayAlarmSound = false;
			TimeSpan lastTime = GetLastActiveTime();			
			TimeSpan shiftTime = TimeSpan.FromSeconds(Math.Floor(DateTime.Now.TimeOfDay.TotalSeconds - lastTime.TotalSeconds));			
			TimersListBox.Items.Cast<PinTimer>().Where(w => w.IsActive && !w.IsPaused).ToList().ForEach(fe => fe.ElapsedTime -= shiftTime);
			_shouldPlayAlarmSound = true;
		}

		private TimeSpan GetLastActiveTime()
		{
			IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
			TimeSpan result = new TimeSpan();
			if (settings.Contains("lastActiveTime"))
				result = (TimeSpan)settings["lastActiveTime"];
			
			return result;
		}

		private void SaveLastActiveTime()
		{
			IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
			if (!settings.Contains("lastActiveTime"))
				settings.Add("lastActiveTime", DateTime.Now.TimeOfDay);
			else
				settings["lastActiveTime"] = DateTime.Now.TimeOfDay;
			settings.Save();
		}

		private bool IsLightTheme()
		{
			Visibility isVisible = (Visibility)Application.Current.Resources["PhoneLightThemeVisibility"];
			return isVisible == Visibility.Visible;			
		}

		private void LoadTimers()
		{
			string timersString = "";
			IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
			if (settings.Contains("timers"))
				timersString = settings["timers"] as string;

			foreach (string timer in timersString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				PinTimer p = PinTimer.CreateFromString(timer);
				p.TimerCompleted += OnTimerCompleted;
				TimersListBox.Items.Add(p);
			}
		}

		private void SaveTimers()
		{
			if (TimersListBox.Items.Count == 0) return;
			string timersString = String.Join(";", TimersListBox.Items.Select(s => s.ToString()));
			IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

			if (!settings.Contains("timers"))
				settings.Add("timers", timersString);
			else
				settings["timers"] = timersString;

			settings.Save();
		}

		void OnTimerCompleted(PinTimer obj)
		{
			if (Dispatcher.CheckAccess())
				HandleSoundAndAnimation(obj);
			else
				Dispatcher.BeginInvoke(() =>
				{
					HandleSoundAndAnimation(obj);
				});
		}	

		private void HandleSoundAndAnimation(PinTimer obj)
		{
			if (_shouldPlayAlarmSound)
			{
				media.Source = obj.AudioSource;
				media.Play();
			}

			ListBoxItem item = TimersListBox.ItemContainerGenerator.ContainerFromItem(obj) as ListBoxItem;

			if (item.Tag == null)
			{
				ColorAnimation ca = new ColorAnimation();
				ca.Duration = TimeSpan.FromSeconds(0.75);
				ca.From = Colors.Red;
				ca.To = Colors.Blue;

				Storyboard.SetTarget(ca, item);
				Storyboard.SetTargetProperty(ca, new PropertyPath("(Panel.Background).(SolidColorBrush.Color)"));

				Storyboard sb = new Storyboard();
				sb.AutoReverse = true;
				sb.Children.Add(ca);
				sb.RepeatBehavior = RepeatBehavior.Forever;
				sb.Begin();
				item.Tag = sb;
			}
			else
				(item.Tag as Storyboard).Begin();
		}

		private void HandleTimerTap(PinTimer timer)
		{
			if (!timer.IsActive)
				timer.StartTimer();
			else
			{
				timer.StopTimer();
				timer.ResetTime();
			}
		}

		private void OnSrartTimerClick(object sender, RoutedEventArgs e)
		{
			PinTimer innerTimer = (sender as Button).Tag as PinTimer;
			HandleTimerTap(innerTimer);
		}

		void tp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<TimeSpan> e)
		{
			if (_durationPicker.Tag == null) // if we dont have timer for edit
			{
				p = new PinTimer(e.NewValue);
				TimersListBox.Items.Add(p);
				p.TimerCompleted += OnTimerCompleted;
			}else // edit timer time
			{
				PinTimer timer = _durationPicker.Tag as PinTimer;
				timer.CountDownTime = e.NewValue;
			}
			SaveTimers();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			string timerId;
			this.NavigationContext.QueryString.TryGetValue("id", out timerId);
			
			if (timerId != null)
			{
				PinTimer timer = TimersListBox.Items.First(s => (s as PinTimer).Id == timerId) as PinTimer;
				timer.StartTimer();
			}			
		}
		private void OnResetTimerClick(object sender, RoutedEventArgs e)
		{
			PinTimer timer = (sender as Button).Tag as PinTimer;
			timer.ResetTime();
		}

		private void OnPinTimerClick(object sender, RoutedEventArgs e)
		{
			PinTimer timer = (sender as Button).Tag as PinTimer;
			if (timer.HasTile)
				timer.DeleteTile();
			else
				timer.PinTile("/MainPage.xaml");
		}

		private void OnEditTimerClick(object sender, RoutedEventArgs e)
		{
			PinTimer timer = (sender as Button).Tag as PinTimer;
			_durationPicker.Tag = timer;
			DisplayTimePicker(timer.CountDownTime);
		}

		private void OnPauseTimerClick(object sender, RoutedEventArgs e)
		{
			PinTimer timer = (sender as Button).Tag as PinTimer;
			if (!timer.IsPaused)
				timer.PauseTimer();
			else
				timer.ResumeTimer();
		}

		private void OnAddNewTimerClick(object sender, EventArgs e)
		{
			DisplayTimePicker(new TimeSpan());
		}

		void DisplayTimePicker(TimeSpan currentTime)
		{
			_durationPicker.ValueChanged -= tp_ValueChanged;
			_durationPicker.Value = currentTime;
			_durationPicker.ValueChanged += tp_ValueChanged;

			_durationPicker.OpenPicker();
		}

		private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
		{
			TimersListBox.Items.Remove((sender as MenuItem).DataContext);
			SaveTimers();
		}

		private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var item = TimersListBox.ItemContainerGenerator.ContainerFromItem((sender as Grid).Tag) as ListBoxItem;
			PinTimer innerTimer = (sender as Grid).Tag as PinTimer;
			Storyboard board = item.Tag != null ? item.Tag as Storyboard : null;
			if (board != null && board.GetCurrentState() == ClockState.Active)
			{				
				board.Stop();
				media.Stop();
				innerTimer.ResetTime();
			}			
			else
				HandleTimerTap(innerTimer);
		}

		private void RoundButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			e.Handled = true;
		}


		
		// Sample code for building a localized ApplicationBar
		//private void BuildLocalizedApplicationBar()
		//{
		//    // Set the page's ApplicationBar to a new instance of ApplicationBar.
		//    ApplicationBar = new ApplicationBar();

		//    // Create a new button and set the text value to the localized string from AppResources.
		//    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
		//    appBarButton.Text = AppResources.AppBarButtonText;
		//    ApplicationBar.Buttons.Add(appBarButton);

		//    // Create a new menu item with the localized string from AppResources.
		//    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
		//    ApplicationBar.MenuItems.Add(appBarMenuItem);
		//}
	}
}