using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Scheduler;

namespace PinTimer
{
	public partial class AddNotification : PhoneApplicationPage
	{
		public AddNotification()
		{
			InitializeComponent();
		}

		private void ApplicationBarIconButton_Click(object sender, EventArgs e)
		{
			/*String name = System.Guid.NewGuid().ToString();
			DateTime date = (DateTime)beginDatePicker.Value;
			DateTime time = (DateTime)beginTimePicker.Value;
			DateTime beginTime = date + time.TimeOfDay;

			// Make sure that the begin time has not already passed.
			if (beginTime < DateTime.Now)
			{
				MessageBox.Show("the begin date must be in the future.");
				return;
			}

			// Get the expiration time for the notification.
			date = (DateTime)expirationDatePicker.Value;
			time = date.Date + expirationTimePicker.Value.Value;
			DateTime expirationTime = date + time.TimeOfDay;

			// Make sure that the expiration time is after the begin time.
			if (expirationTime < beginTime)
			{
				MessageBox.Show("expiration time must be after the begin time.");
				return;
			}

			/**/
/*
			RecurrenceInterval recurrence = RecurrenceInterval.None;
			if (dailyRadioButton.IsChecked == true)
			{
				recurrence = RecurrenceInterval.Daily;
			}
			else if (weeklyRadioButton.IsChecked == true)
			{
				recurrence = RecurrenceInterval.Weekly;
			}
			else if (monthlyRadioButton.IsChecked == true)
			{
				recurrence = RecurrenceInterval.Monthly;
			}
			else if (endOfMonthRadioButton.IsChecked == true)
			{
				recurrence = RecurrenceInterval.EndOfMonth;
			}
			else if (yearlyRadioButton.IsChecked == true)
			{
				recurrence = RecurrenceInterval.Yearly;
			}

			/************************************************************************/
			/*                                                                      */
			/************************************************************************/
/*
			string param1Value = param1TextBox.Text;
			string param2Value = param2TextBox.Text;
			string queryString = "";
			if (param1Value != "" && param2Value != "")
			{
				queryString = "?param1=" + param1Value + "&param2=" + param2Value;
			}
			else if (param1Value != "" || param2Value != "")
			{
				queryString = (param1Value != null) ? "?param1=" + param1Value : "?param2=" + param2Value;
			}
			Uri navigationUri = new Uri("/ShowParams.xaml" + queryString, UriKind.Relative);

			/************************************************************************/
			/*                                                                      */
			/************************************************************************/
/*
			if ((bool)reminderRadioButton.IsChecked)
			{
				Reminder reminder = new Reminder(name);
				reminder.Title = titleTextBox.Text;
				reminder.Content = contentTextBox.Text;
				reminder.BeginTime = beginTime;
				reminder.ExpirationTime = expirationTime;
				reminder.RecurrenceType = recurrence;
				reminder.NavigationUri = navigationUri;

				// Register the reminder with the system.
				ScheduledActionService.Add(reminder);
			}
			else
			{
				Alarm alarm = new Alarm(name);
				alarm.Content = contentTextBox.Text;
				//alarm.Sound = new Uri("/Ringtones/Ring01.wma", UriKind.Relative);
				alarm.BeginTime = beginTime;
				alarm.ExpirationTime = expirationTime;
				alarm.RecurrenceType = recurrence;

				ScheduledActionService.Add(alarm);
			}
			NavigationService.GoBack();
			*/
		}

		private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
		{
			picker.OpenPicker();
		}
	}
}