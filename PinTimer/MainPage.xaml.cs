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
using System.Windows.Controls.Primitives;
using System.Threading.Tasks;
using Microsoft.Phone;
using Microsoft.Phone.Reactive;

namespace PinTimer
{
	public partial class MainPage : PhoneApplicationPage
	{
		//PinTimer p;
		//TimeSpanPicker _durationPicker;
		List<PinTimer> _listNeedAmimationFor = new List<PinTimer>();
		TimerSettingsSetup _timerSetupControl;
		//PinTimer _tileTapTimer = null;
		// Constructor
		public MainPage()
		{
			InitializeComponent();
			
			/*_durationPicker = new TimeSpanPicker();
			_durationPicker.Visibility = Visibility.Collapsed;
			_durationPicker.DialogTitle = "Select countdown time";
			_durationPicker.ValueChanged += tp_ValueChanged;
			LayoutRoot.Children.Add(_durationPicker);*/
			if (IsLightTheme())
				LayoutRoot.Background = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120));

			LoadTimers();
			BuildLocalizedApplicationBar();
			ConfigIdleDetectionMode();
			PhoneApplicationService.Current.Deactivated += Current_Deactivated;
			PhoneApplicationService.Current.Closing += Current_Closing;
		}

		private Popup _popup;
		private TextBlock _popText;
		private Popup popup {
			get
			{
				if (_popup == null)
				{
					_popText = new TextBlock();
					_popText.Foreground = (Brush)this.Resources["PhoneForegroundBrush"];
					_popText.FontSize = (double)this.Resources["PhoneFontSizeMedium"];
					_popText.Margin = new Thickness(24, 32, 24, 12);

					// grid wrapper
					Grid grid = new Grid();
					grid.Background = (Brush)this.Resources["PhoneAccentBrush"];
					grid.Children.Add(_popText);
					grid.Width = this.ActualWidth;

					// popup
					_popup = new Popup();
					_popup.Child = grid;
				}
				
				return _popup;
			}
		}

		// hides popup
		private void HidePopup()
		{
			SystemTray.BackgroundColor = ApplicationBar.BackgroundColor;// (System.Windows.Media.Color)Application.Current.Resources["AppBarBackColor"];//(Color)this.Resources["PhoneBackgroundColor"];
			popup.IsOpen = false;
		}

		// shows popup
		private async void ShowPopup(string message, uint hideAfter = 1200)
		{
			await Task.Delay(50);
			SystemTray.BackgroundColor = (Color)this.Resources["PhoneAccentColor"];
			popup.IsOpen = true;
			_popText.Text = message;
			await Task.Delay((int)hideAfter);
			this.HidePopup();
		}

		void ConfigIdleDetectionMode()
		{
			PhoneApplicationService.Current.UserIdleDetectionMode = TimersListBox.Items.Cast<PinTimer>().Any(w => w.IsActive && !w.IsPaused) ? IdleDetectionMode.Disabled : IdleDetectionMode.Enabled;
			//ShowPopup("Timers active = " + PhoneApplicationService.Current.UserIdleDetectionMode);
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
			ScheduleBackgroundTimers();
		}

		void Current_Deactivated(object sender, DeactivatedEventArgs e)
		{
			SaveLastActiveTime();
			SaveTimers();
			ScheduleBackgroundTimers();
		}

		void ScheduleBackgroundTimers()
		{
			foreach(PinTimer timer in TimersListBox.Items.Cast<PinTimer>().Where(w => w.IsActive && !w.IsPaused).ToList())
			{
				Alarm alarm = new Alarm(timer.Id);
				alarm.Content = timer.ContentMessage ?? timer.CountDownTime.ToString() + " is over";
				alarm.Sound = timer.AudioSource.Path;
				alarm.BeginTime = DateTime.Now + timer.ElapsedTime;
				Debug.WriteLine("previous time " + alarm.BeginTime);
				if (alarm.BeginTime.Minute == DateTime.Now.Minute)
				{
					alarm.BeginTime = alarm.BeginTime.AddSeconds(60 - alarm.BeginTime.Second);
					Debug.WriteLine("changed time " + alarm.BeginTime);
				}
				ScheduledActionService.Add(alarm);
			}
		}

		void UnScheduleBackgroundTimers()
		{
			var notifications = ScheduledActionService.GetActions<ScheduledNotification>();
			foreach (ScheduledNotification not in notifications)
				ScheduledActionService.Remove(not.Name);
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
				PinTimer p = PinTimer.ParseFromString(timer);
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
			{
				StartCompleteAnimation(item);
				ShowPopup(obj.ContentMessage, 5000);
			}
			if (!inBackground)
			{
				media.MediaOpened += media_MediaOpened;
				media.Source = obj.AudioSource.Path;
				//Scheduler.Dispatcher.Schedule(() => { media.Play(); }, TimeSpan.FromMilliseconds(3360));				
			}
		}

		void media_MediaOpened(object sender, RoutedEventArgs e)
		{
			media.Play();
			media.MediaOpened -= media_MediaOpened;
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
			ConfigIdleDetectionMode();
		}

		private void OnSrartTimerClick(object sender, RoutedEventArgs e)
		{
			PinTimer innerTimer = (sender as Button).Tag as PinTimer;
			HandleTimerTap(innerTimer);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			string timerId;

			if (e.NavigationMode == NavigationMode.Reset) return;
			
			if (e.IsNavigationInitiator && e.NavigationMode == NavigationMode.Back) return;
			bool isBackWithTile = (e.NavigationMode == NavigationMode.Back && NavigationContext.QueryString.ContainsKey("id"));

			//Debug.WriteLine(e.IsNavigationInitiator + " mode = " + e.NavigationMode);
			PinTimer timer = null;
			if (this.NavigationContext.QueryString.TryGetValue("id", out timerId))
				timer = TimersListBox.Items.First(s => (s as PinTimer).Id == timerId) as PinTimer;

			UnScheduleBackgroundTimers();
			ShiftTimeForActiveTimers(isBackWithTile ? null : timer);

			// handle tile tap timer
			if (timer == null || isBackWithTile) return;
			//ShowPopup("tile timer has value");
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
			ConfigIdleDetectionMode();
		 }

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			if (e.Content is CustomDialogPage)
			{
				(e.Content as CustomDialogPage).CustomView = _timerSetupControl;
				(e.Content as CustomDialogPage).dismissedWithOk += OnAddTimerDismissedWithOk;
			}
		}

		void OnAddTimerDismissedWithOk(UIElement obj)
		{
			if (obj == null) return;
			if (_timerSetupControl.beginDatePicker.Value == TimeSpan.Zero) return;

			if (TimersListBox.Items.Contains(_timerSetupControl.Timer) == false) // if timer just created
				TimersListBox.Items.Add(_timerSetupControl.Timer);
			_timerSetupControl.Timer.TimerCompleted += OnTimerCompleted;
			SaveTimers();
			_timerSetupControl = null;
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
			DisplaySetupTimerDialog(timer);
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
			DisplaySetupTimerDialog();
		}

		void DisplaySetupTimerDialog(PinTimer timer = null)
		{
			_timerSetupControl = new TimerSettingsSetup(timer);
			NavigationService.Navigate(new Uri("/CustomDialogPage.xaml", UriKind.Relative));
			/*
			DisplaySetupTimerDialog(_timerSetupControl);
			_timerSetupControl.beginDatePicker.ValueChanged += beginDatePicker_ValueChanged;
			_timerSetupControl.soundPicker. += soundPicker_SelectionChanged; // перенести добавление нового таймера на отдельную страницу ибо с messageBox геморой */
		}

		/*void DisplaySetupTimerDialog(TimerSettingsSetup settings)
		{
			CustomMessageBox msb = new CustomMessageBox();			
			msb.Content = settings;
			msb.LeftButtonContent = "Ok";
			msb.RightButtonContent = "Cancel";
			msb.Show();
			msb.Dismissed += msb_Dismissed;
		}*/

		

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
		private void BuildLocalizedApplicationBar()
		{
		    // Set the page's ApplicationBar to a new instance of ApplicationBar.
		    ApplicationBar = new ApplicationBar();
				//    // Create a new button and set the text value to the localized string from AppResources.
			ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("Assets/add.png", UriKind.Relative));
		    appBarButton.Text = AppResources.AppBarAddTimer;
			appBarButton.Click += OnAddNewTimerClick;
		    ApplicationBar.Buttons.Add(appBarButton);

			appBarButton = new ApplicationBarIconButton(new Uri("Assets/feature.settings.png", UriKind.Relative));
			appBarButton.Text = AppResources.AppBarSettings;
			appBarButton.Click += (sender, e) => { NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative)); };
			ApplicationBar.Buttons.Add(appBarButton);

			//feature.settings.png
		    // Create a new menu item with the localized string from AppResources.
		    //ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
		    //ApplicationBar.MenuItems.Add(appBarMenuItem);
		}

		
	}
}