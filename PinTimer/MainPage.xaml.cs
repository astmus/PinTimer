using System;
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
		//PinTimer p;
		TimeSpanPicker _durationPicker;
		List<PinTimer> _listNeedAmimationFor = new List<PinTimer>();
		//PinTimer _tileTapTimer = null;
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

			ConfigIdleDetectionMode();
			PhoneApplicationService.Current.Deactivated += Current_Deactivated;
			PhoneApplicationService.Current.Closing += Current_Closing;
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

		void ConfigIdleDetectionMode()
		{
			PhoneApplicationService.Current.UserIdleDetectionMode = TimersListBox.Items.Cast<PinTimer>().Any(w => w.IsActive && !w.IsPaused) ? IdleDetectionMode.Disabled : IdleDetectionMode.Enabled;
		}

		private void TimersListBox_Loaded(object sender, RoutedEventArgs e)
		{
			foreach (PinTimer timer in _listNeedAmimationFor)
			{
				ListBoxItem item = TimersListBox.ItemContainerGenerator.ContainerFromItem(timer) as ListBoxItem;
				StartCompleteAnimation(item);
			}
			_listNeedAmimationFor.Clear();
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

		void ScheduleBackgroundTimers()
		{
			/*Alarm alarm = new Alarm(name);
			alarm.Content = contentTextBox.Text;
			//alarm.Sound = new Uri("/Ringtones/Ring01.wma", UriKind.Relative);
			alarm.BeginTime = beginTime;
			alarm.ExpirationTime = expirationTime;
			alarm.RecurrenceType = recurrence;

			ScheduledActionService.Add(alarm);*/
		}

		private void ShiftTimeForActiveTimers(PinTimer excepHandleTimer = null)
		{
			if (TimersListBox.Items.Count == 0) return;
			var activeTimers = TimersListBox.Items.Cast<PinTimer>().Where(w => w.IsActive && !w.IsPaused && w != excepHandleTimer).ToList();
			if (activeTimers.Count() == 0) return;			
			TimeSpan lastTime = GetLastActiveTime();			
			TimeSpan shiftTime = TimeSpan.FromSeconds(Math.Floor(DateTime.Now.TimeOfDay.TotalSeconds - lastTime.TotalSeconds));			
			activeTimers.ForEach(fe => fe.ElapsedTime -= shiftTime);						
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

		void OnTimerCompleted(PinTimer obj, bool inBackground)
		{
			if (Dispatcher.CheckAccess())
				HandleSoundAndAnimation(obj, inBackground);
			else
				Dispatcher.BeginInvoke(() =>
				{
					Debug.WriteLine("other thread " + media.CurrentState);
					HandleSoundAndAnimation(obj, inBackground);
				});
		}	

		private void HandleSoundAndAnimation(PinTimer obj, bool inBackground)
		{
			ListBoxItem item = TimersListBox.ItemContainerGenerator.ContainerFromItem(obj) as ListBoxItem;
			ConfigIdleDetectionMode();
			if (item == null)
				_listNeedAmimationFor.Add(obj);
			else
				StartCompleteAnimation(item);
			if (!inBackground)
			{
				media.Source = obj.AudioSource;
				//media.Play();
				Debug.WriteLine("play started " + media.CurrentState);
			}
		}

		private void StartCompleteAnimation(ListBoxItem item)
		{
#if RELEASE
			if (item == null) return;
#endif
			if (item.Tag == null)
			{
				ColorAnimation ca = new ColorAnimation();
				ca.Duration = TimeSpan.FromSeconds(0.75);
				ca.From = Color.FromArgb(255,120,120,120);
				ca.To = Color.FromArgb(255,80,80,80);

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
				PinTimer p = new PinTimer(e.NewValue);
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

			if (e.NavigationMode == NavigationMode.Reset) return;
			if (e.IsNavigationInitiator && e.NavigationMode == NavigationMode.Back) return;
			if (e.NavigationMode == NavigationMode.Back && NavigationContext.QueryString.ContainsKey("id")) return;

			//Debug.WriteLine(e.IsNavigationInitiator + " mode = " + e.NavigationMode);
			PinTimer timer = null;
			if (this.NavigationContext.QueryString.TryGetValue("id", out timerId))
				timer = TimersListBox.Items.First(s => (s as PinTimer).Id == timerId) as PinTimer;

			ShiftTimeForActiveTimers(timer);

			// handle tile tap timer
			if (timer == null) return;

			if (timer.IsActive && !timer.IsPaused)
			{
				TimeSpan lastTime = GetLastActiveTime();
				TimeSpan shiftTime = TimeSpan.FromSeconds(Math.Floor(DateTime.Now.TimeOfDay.TotalSeconds - lastTime.TotalSeconds));
				if (shiftTime < timer.ElapsedTime)
					timer.ElapsedTime -= shiftTime;
				else
				{
					timer.ResetTime();
					return;
				}
			}

			if (timer.IsPaused)
				timer.ResumeTimer();
			else
				timer.StartTimer();
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
			PinTimer timerForDelete = (sender as MenuItem).DataContext as PinTimer;
			if (timerForDelete.HasTile) timerForDelete.DeleteTile();
			TimersListBox.Items.Remove(timerForDelete);
			SaveTimers();
		}

		private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			PinTimer innerTimer = (sender as Grid).Tag as PinTimer;
			Storyboard board = GetAnimationForTimer(innerTimer);
			if (board != null && board.GetCurrentState() == ClockState.Active)
			{				
				board.Stop();
				innerTimer.ResetTime();
			}			
			else
				HandleTimerTap(innerTimer);
			
			media.Stop();
			Debug.WriteLine("play paused "+media.CurrentState);
		}

		Storyboard GetAnimationForTimer(PinTimer timer)
		{
			var item = TimersListBox.ItemContainerGenerator.ContainerFromItem(timer) as ListBoxItem;			
			return item.Tag != null ? item.Tag as Storyboard : null;
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