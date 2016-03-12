using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinTimer
{
	public class AudioItemData
	{
		public string Title {get;set;}
		public Uri Path { get; set; }
		public TimeSpan Length { get; set; }

		public string FormattedLength
		{
			get { return Length.TotalMilliseconds != 0 ? Length.ToString("ss\\.fff") + " secs" : "?"; }
		}
		public override string ToString()
		{
			return String.Format("{0}|{1}|{2}", Title, Path, Length.TotalMilliseconds);
		}

		private static Lazy<List<AudioItemData>> defaultItems = new Lazy<List<AudioItemData>>(() =>
		{
			List<AudioItemData> result = new List<AudioItemData>(17);
			result.Add(new AudioItemData() { Title = "Alarm 1", Path = new Uri("/Audio/1112.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(11120) });
			result.Add(new AudioItemData() { Title = "Alarm 2", Path = new Uri("/Audio/1614.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(16140) });
			result.Add(new AudioItemData() { Title = "Alarm 3", Path = new Uri("/Audio/2300.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(23000) });
			result.Add(new AudioItemData() { Title = "Alarm 4", Path = new Uri("/Audio/300.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(3000) });
			result.Add(new AudioItemData() { Title = "Alarm 5", Path = new Uri("/Audio/337.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(3370) });
			result.Add(new AudioItemData() { Title = "Alarm 6", Path = new Uri("/Audio/408.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(4080) });
			result.Add(new AudioItemData() { Title = "Alarm 7", Path = new Uri("/Audio/421.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(4210) });
			result.Add(new AudioItemData() { Title = "Alarm 8", Path = new Uri("/Audio/460.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(4600) });
			result.Add(new AudioItemData() { Title = "Alarm 9", Path = new Uri("/Audio/522.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(5220) });
			result.Add(new AudioItemData() { Title = "Alarm 10", Path = new Uri("/Audio/556.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(5560) });
			result.Add(new AudioItemData() { Title = "Alarm 11", Path = new Uri("/Audio/562.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(5620) });
			result.Add(new AudioItemData() { Title = "Alarm 12", Path = new Uri("/Audio/596.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(5960) });
			result.Add(new AudioItemData() { Title = "Alarm 13", Path = new Uri("/Audio/665.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(6650) });
			result.Add(new AudioItemData() { Title = "Alarm 14", Path = new Uri("/Audio/708.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(7080) });
			result.Add(new AudioItemData() { Title = "Alarm 15", Path = new Uri("/Audio/807.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(8070) });
			result.Add(new AudioItemData() { Title = "Alarm 16", Path = new Uri("/Audio/911.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(9110) });
			result.Add(new AudioItemData() { Title = "Alarm 17", Path = new Uri("/Audio/94.mp3", UriKind.Relative), Length = TimeSpan.FromMilliseconds(940) });
			return result;
		});

		private static Lazy<List<AudioItemData>> customItems = new Lazy<List<AudioItemData>>(() =>
		{
			List<AudioItemData> result = new List<AudioItemData>();
			
			string audioDataString = "";
			IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
			if (settings.Contains("audios"))
				audioDataString = settings["audios"] as string;

			foreach (string data in audioDataString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				var parts = data.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
				AudioItemData newItem = new AudioItemData();
				newItem.Title = parts[0];
				newItem.Path = new Uri(parts[1], UriKind.RelativeOrAbsolute);
				newItem.Length = TimeSpan.FromMilliseconds(double.Parse(parts[2]));
				result.Add(newItem);
			}

			return result;
		});

		private static ObservableCollection<AudioItemData> _listOfPossible;
		public static ObservableCollection<AudioItemData> ListOfPossible 
		{
			get { return _listOfPossible ?? (_listOfPossible = new ObservableCollection<AudioItemData>(defaultItems.Value.Concat(customItems.Value))); }
		} 		

		public static void AddNewAudio(AudioItemData data)
		{
			_listOfPossible.Add(data);
			customItems.Value.Add(data);
			string timersString = String.Join(";", customItems.Value.Select(s => s.ToString()));
			IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

			if (!settings.Contains("audios"))
				settings.Add("audios", timersString);
			else
				settings["audios"] = timersString;

			settings.Save();
		}
	}
}
