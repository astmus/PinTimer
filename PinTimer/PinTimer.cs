using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PinTimer
{
	class PinTimer : DependencyObject
	{
		private static TimeSpan _negativeSecond = TimeSpan.FromSeconds(-1);
		private Timer _timer;
		private TimeSpan _countDownTime;

		private TimeSpan _elapsedTime;
		private Uri _audioSource;
		private string _id;

		public event Action<PinTimer, bool> TimerCompleted;
		

		public static PinTimer ParseFromString(string timerData)
		{
			PinTimer result = null;
			var parts = timerData.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			try
			{
				TimeSpan time = TimeSpan.FromSeconds(int.Parse(parts[0]));
				string id = parts[1];
				result = new PinTimer(time, id);

				bool isActive = bool.Parse(parts[2]);
				if (isActive)
					result.StartTimer(TimeSpan.FromMilliseconds(double.Parse(parts[3])));
								
				bool isPaused = bool.Parse(parts[4]);
				if (isPaused)
					result.PauseTimer();				
			}
			catch { }

			return result;
		}

		public PinTimer(TimeSpan countDownTime)
			: this(countDownTime, Guid.NewGuid().ToString())
		{
			
		}

		public PinTimer(TimeSpan countDownTime, string id)
		{
			_id = id;
			_countDownTime = countDownTime;
			ElapsedTime = _countDownTime;
			_audioSource = new System.Uri("/Audio/2.mp3", UriKind.Relative);
			HasTile = ShellTile.ActiveTiles.FirstOrDefault(tile => tile.NavigationUri.ToString().Contains(_id)) != null;
			PhoneApplicationService.Current.Deactivated +=Current_Deactivated;
			PhoneApplicationService.Current.Closing += Current_Closing;
		}

		void Current_Closing(object sender, ClosingEventArgs e)
		{
			UpdateTileAccordinglyToCurrentState();
		}

		void Current_Deactivated(object sender, DeactivatedEventArgs e)
		{
			UpdateTileAccordinglyToCurrentState();
		}

		#region Properties
		public TimeSpan CountDownTime
		{
			get { return _countDownTime; }
			set
			{
				_countDownTime = value;
				ElapsedTime = _countDownTime;
				UpdateTileAccordinglyToCurrentState();
			}
		}

		public TimeSpan ElapsedTime
		{
			get { return _elapsedTime; }
			set
			{
				if (value.TotalSeconds > 0)
					_elapsedTime = value;
				else
				{
					StopTimer();
					_elapsedTime = new TimeSpan();
					if (TimerCompleted != null)
						TimerCompleted(this, value.TotalSeconds < 0);
				}				
				ElaspedFormatedTime = _elapsedTime.ToString();
			}
		}

		public string Id
		{
			get { return _id; }
		}

		public Uri AudioSource
		{
			get	{ return _audioSource; }
		}
		#endregion

		public override string ToString()
		{
			return String.Format("{0}|{1}|{2}|{3}|{4}", (ulong)_countDownTime.TotalSeconds, _id, IsActive, ElapsedTime.TotalMilliseconds, IsPaused);
		}

		#region Tile
		public void PinTile(string destinationPage)
		{
			ShellTile shellTile = ShellTile.ActiveTiles.FirstOrDefault(tile => tile.NavigationUri.ToString().Contains(_id));
			if (shellTile != null) return;

			string tileuri = string.Format("/{0}?id={1}", destinationPage, _id);
			ShellTile.Create(new Uri(tileuri, UriKind.Relative), GetTileDataForCurrentState(), true);
			HasTile = true;
		}

		public void DeleteTile()
		{
			if (!HasTile) return;
			ShellTile shellTile = ShellTile.ActiveTiles.FirstOrDefault(tile => tile.NavigationUri.ToString().Contains(_id));
			shellTile.Delete();
			HasTile = false;
		}

		private void UpdateTileAccordinglyToCurrentState()
		{
			if (HasTile) // think about replace this string in to HasTile getter
				ShellTile.ActiveTiles.FirstOrDefault(tile => tile.NavigationUri.ToString().Contains(_id)).Update(GetTileDataForCurrentState());
		}

		private IconicTileData GetTileDataForCurrentState()
		{
			string uri = "/Assets/timer.png";
			
			if (IsActive)
				uri = IsPaused ? "/Assets/timer.pause.png" : "/Assets/timer.play.png";

			IconicTileData data = new IconicTileData()
			{
				Title = ElapsedTime.ToString(),
				SmallIconImage = new Uri(uri, UriKind.Relative),
				IconImage = new Uri(uri, UriKind.Relative),
			};

			return data;
		}
		#endregion

		#region Basic actions

		public void StartTimer()
		{
			StartTimer(_countDownTime);
			UpdateTileAccordinglyToCurrentState();
		}
		public void StartTimer(TimeSpan countDownTime)
		{
			if (IsActive) return;
			ElapsedTime = countDownTime;
			_timer = new Timer(new TimerCallback(Callback));
			_timer.Change(1000, 1000);
			IsActive = true;
		}

		public void PauseTimer()
		{
			if (_timer == null) return;
			_timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
			IsPaused = true;
			UpdateTileAccordinglyToCurrentState();
		}

		public void ResumeTimer()
		{
			if (_timer == null) return;
			_timer.Change(1000, 1000);
			IsPaused = false;
			UpdateTileAccordinglyToCurrentState();
		}

		public void StopTimer()
		{
			_timer.Dispose();
			IsPaused = IsActive = false;
			UpdateTileAccordinglyToCurrentState();
		}

		public void ResetTime()
		{
			ElapsedTime = _countDownTime;
			UpdateTileAccordinglyToCurrentState();
		}
		#endregion		

		private void Callback(object state)
		{
			ElapsedTime = ElapsedTime.Add(_negativeSecond);			
		}

		#region Dp Properties
		public bool IsPaused
		{
			get { return (bool)GetValue(IsPausedProperty); }
			private set 
			{
				if (this.Dispatcher.CheckAccess())
					SetValue(IsPausedProperty, value);
				else
				Dispatcher.BeginInvoke(() =>
				{
					SetValue(IsPausedProperty, value);
				});
			}
		}

		// Using a DependencyProperty as the backing store for IsPaused.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsPausedProperty =
			DependencyProperty.Register("IsPaused", typeof(bool), typeof(PinTimer), new PropertyMetadata(false));

		public bool IsActive
		{
			get { return (bool)GetValue(IsActiveProperty); }
			private set
			{
				if (Dispatcher.CheckAccess())
					SetValue(IsActiveProperty, value);
				else
				Dispatcher.BeginInvoke(() =>
				{
					SetValue(IsActiveProperty, value);
				});
			}
		}

		public static readonly DependencyProperty IsActiveProperty =
			DependencyProperty.Register("IsActive", typeof(bool), typeof(PinTimer), new PropertyMetadata(false));
		public bool HasTile
		{
			get { return (bool)GetValue(HasTileProperty); }
			private set { SetValue(HasTileProperty, value); }
		}

		// Using a DependencyProperty as the backing store for HasTile.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty HasTileProperty =
			DependencyProperty.Register("HasTile", typeof(bool), typeof(PinTimer), new PropertyMetadata(false));

		public string ElaspedFormatedTime
		{
			get
			{
				return (string)GetValue(ElaspedFormatedTimeProperty);
			}
			private set
			{
				if (this.Dispatcher.CheckAccess())
					SetValue(ElaspedFormatedTimeProperty, value);
				else
				Dispatcher.BeginInvoke(() =>
				{
					SetValue(ElaspedFormatedTimeProperty, value);
				});
			}
		}
		// Using a DependencyProperty as the backing store for ElaspedFormatedTime.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ElaspedFormatedTimeProperty =
			DependencyProperty.Register("ElaspedFormatedTime", typeof(string), typeof(PinTimer), new PropertyMetadata(null));

		#endregion
	}
}
